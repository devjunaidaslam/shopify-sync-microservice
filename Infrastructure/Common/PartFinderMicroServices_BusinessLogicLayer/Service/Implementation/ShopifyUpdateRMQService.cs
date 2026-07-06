using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using PartFinderMicroServices_BusinessLogicLayer.Service.Interface;
using PartFinderMicroServices_DataAccessLayer.Entities.DTOs;
using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.TransactionHistoryDTO;
using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.UpdateInventoryRequest;
using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.UpdatePriceRequest;
using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.UpdateVariantLocationPriceRequest;
using PartFinderMicroServices_DataAccessLayer.Entities.RabbitMQ;
using PartFinderMicroServices_DataAccessLayer.Enum;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace PartFinderMicroServices_BusinessLogicLayer.Service.Implementation
{
    /// <summary>
    /// Service for processing OUTBOUND updates TO Shopify via RabbitMQ
    /// Handles VariantPrice, VariantLocation, InventoryLevel, and FitmentSync update messages
    /// </summary>
    public class ShopifyUpdateRMQService : IShopifyUpdateRMQService, IDisposable
    {
        private readonly RabbitMQSetting _rabbitMQSetting;
        private readonly IServiceProvider _serviceProvider;
        private readonly IShopifyUpdateService _shopifyUpdateService;
        private readonly ITransactionHistoryService _transactionHistoryService;
        private readonly IFitmentService _fitmentService;
        private readonly ILogger<ShopifyUpdateRMQService> _logger;
        
        // Connection management - single long-lived connection
        private IConnection _connection;
        private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(1, 1);
        private bool _disposed = false;

        public ShopifyUpdateRMQService(
            IOptions<RabbitMQSetting> rabbitMQSetting,
            IShopifyUpdateService shopifyUpdateService,
            IServiceProvider serviceProvider,
            ILogger<ShopifyUpdateRMQService> logger,
            ITransactionHistoryService transactionHistoryService,
            IFitmentService fitmentService)
        {
            _rabbitMQSetting = rabbitMQSetting.Value;
            _shopifyUpdateService = shopifyUpdateService;
            _serviceProvider = serviceProvider;
            _logger = logger;
            _transactionHistoryService = transactionHistoryService;
            _fitmentService = fitmentService;
            _logger.LogInformation("[ShopifyUpdateRMQService] Service initialized successfully with host: {HostName}, port: {Port}",
                _rabbitMQSetting.HostName, _rabbitMQSetting.Port);
        }

        /// <summary>
        /// Gets or creates the shared RabbitMQ connection (thread-safe)
        /// </summary>
        private async Task<IConnection> GetConnectionAsync()
        {
            if (_connection != null && _connection.IsOpen)
            {
                return _connection;
            }

            await _connectionLock.WaitAsync();
            try
            {
                // Double-check pattern - connection might have been created while waiting
                if (_connection != null && _connection.IsOpen)
        {
                    return _connection;
                }

                // Dispose old connection if it exists
                _connection?.Dispose();

                _logger.LogInformation("[ShopifyUpdateRMQService] Creating new shared RabbitMQ connection to {HostName}:{Port}", 
                    _rabbitMQSetting.HostName, _rabbitMQSetting.Port);

                if (string.IsNullOrWhiteSpace(_rabbitMQSetting.HostName))
                {
                    _logger.LogError("[ShopifyUpdateRMQService] HostName is null or whitespace, cannot create connection");
                    throw new InvalidOperationException("RabbitMQ HostName is not configured");
                }

                var factory = new ConnectionFactory()
                {
                    HostName = _rabbitMQSetting.HostName,
                    Port = _rabbitMQSetting.Port,
                    UserName = _rabbitMQSetting.UserName,
                    Password = _rabbitMQSetting.Password,
                    VirtualHost = _rabbitMQSetting.VirtualHost,
                };

                if (_rabbitMQSetting.UseSsl)
                {
                    factory.Ssl = new SslOption()
                    {
                        Enabled = true,
                        ServerName = _rabbitMQSetting.HostName,
                    };
                }

                _connection = await factory.CreateConnectionAsync();
                _logger.LogInformation("[ShopifyUpdateRMQService] ✅ Shared RabbitMQ connection created successfully. Connection count: 1");
                
                return _connection;
                }
                catch (Exception ex)
                {
                _logger.LogError(ex, "[ShopifyUpdateRMQService] ❌ Failed to create RabbitMQ connection: {Message}", ex.Message);
                    throw;
                }
            finally
            {
                _connectionLock.Release();
            }
        }

        /// <summary>
        /// Creates a new channel from the shared connection
        /// </summary>
        private async Task<IChannel> CreateChannelAsync()
        {
            var connection = await GetConnectionAsync();
            var channel = await connection.CreateChannelAsync();
            _logger.LogDebug("[ShopifyUpdateRMQService] Created new channel from shared connection");
            return channel;
        }

        /// <summary>
        /// [DEPRECATED] Legacy method for backward compatibility. Use CreateChannelAsync() instead.
        /// Creates a channel from the shared connection.
        /// </summary>
        public async Task<IChannel> InitializeMQ(RabbitMQSetting setting)
            {
            _logger.LogWarning("[ShopifyUpdateRMQService] Using shared connection instead of creating new connection.");
            return await CreateChannelAsync();
        }

        public async Task ReceviedData(List<string> queues)
        {
            foreach (var queue in queues)
            {
                _logger.LogDebug("[ShopifyUpdateRMQService] Processing queue: {QueueName}", queue);
                IChannel dedicatedChannel = null;
                try
                {
                    // Create a dedicated channel for THIS queue from the shared connection
                    dedicatedChannel = await CreateChannelAsync();
                    _logger.LogDebug("[ShopifyUpdateRMQService] ✅ Channel created for queue: {QueueName}", queue);
                    
                    // Try to declare queue with DLX, fallback to without DLX if queue already exists
                    // Separate try-catch for queue declaration only
                    try
                    {
                        // Declare dead-letter exchange and queue
                        var dlxName = "dlx.shopify.updates";
                        var dlqName = $"{queue}.dlq";
                        
                        _logger.LogDebug("[ShopifyUpdateRMQService] Declaring dead-letter exchange: {DLXName}", dlxName);
                        await dedicatedChannel.ExchangeDeclareAsync(
                            exchange: dlxName,
                            type: "direct",
                            durable: true,
                            autoDelete: false,
                            arguments: null
                        );
                        
                        _logger.LogDebug("[ShopifyUpdateRMQService] Declaring dead-letter queue: {DLQName}", dlqName);
                        
                        // Set DLQ with 14-day TTL (1,209,600,000 ms) to prevent message loss
                        var dlqArgs = new Dictionary<string, object>
                        {
                            { "x-message-ttl", 1209600000 } // 14 days in milliseconds
                        };
                        
                        await dedicatedChannel.QueueDeclareAsync(
                            queue: dlqName,
                            durable: true,
                            exclusive: false,
                            autoDelete: false,
                            arguments: dlqArgs
                        );
                        
                        await dedicatedChannel.QueueBindAsync(
                            queue: dlqName,
                            exchange: dlxName,
                            routingKey: queue
                        );
                        
                        // Declare main queue with DLX configuration
                        _logger.LogDebug("[ShopifyUpdateRMQService] Declaring queue: {QueueName} with DLX", queue);
                        var queueArgs = new Dictionary<string, object>
                        {
                            { "x-dead-letter-exchange", dlxName },
                            { "x-dead-letter-routing-key", queue }
                        };
                        
                        await dedicatedChannel.QueueDeclareAsync(
                                   queue: queue,
                                   durable: true,
                                   exclusive: false,
                                   autoDelete: false,
                                   arguments: queueArgs
                               );
                        
                        _logger.LogInformation("[ShopifyUpdateRMQService] ✅ Queue {QueueName} declared WITH DLX configuration. DLX={DLX}, DLQ={DLQ}", queue, dlxName, dlqName);
                    }
                    catch (RabbitMQ.Client.Exceptions.OperationInterruptedException ex) when (ex.Message.Contains("PRECONDITION_FAILED"))
                    {
                        _logger.LogWarning("[ShopifyUpdateRMQService] ⚠️ Queue {QueueName} already exists with different configuration. Using existing queue. To update configuration, delete queue {QueueName} and restart service.", queue, queue);
                        
                        // Dispose the old channel and create a new one from the shared connection
                        dedicatedChannel?.Dispose();
                        dedicatedChannel = await CreateChannelAsync();
                        
                        // Declare queue with passive=true to just verify it exists
                        // This won't fail if queue already exists with different args
                        await dedicatedChannel.QueueDeclarePassiveAsync(queue);
                    }

                    // Consumer setup - separate from queue declaration
                    // Each consumer gets its own dedicated channel
                    var consumer = new AsyncEventingBasicConsumer(dedicatedChannel);

                    consumer.ReceivedAsync += async (model, ea) =>
                    {
                        var body = ea.Body.ToArray();
                        var queueName = ea.RoutingKey;
                        var message = Encoding.UTF8.GetString(body);

                        // Get retry count from message headers
                        int retryCount = 0;
                        if (ea.BasicProperties?.Headers != null && ea.BasicProperties.Headers.ContainsKey("x-retry-count"))
                        {
                            retryCount = Convert.ToInt32(ea.BasicProperties.Headers["x-retry-count"]);
                        }

                        try
                        {
                            using var scope = _serviceProvider.CreateScope();
                            var scopedProvider = scope.ServiceProvider;
                            var shopifyUpdateService = scopedProvider.GetRequiredService<IShopifyUpdateService>();
                            var transactionHistoryService = scopedProvider.GetRequiredService<ITransactionHistoryService>();

                            var transaction = new CreateTransactionDto
                            {
                                TransactionType = "received",
                                EventType = $"Get From Queue : {queueName}",
                                Status = "success",
                                Microservice = "ShopifyService",
                                Payload = message,
                                ReceivedBy = DateTime.UtcNow,
                                WebhookId = null
                            };

                            // Process OUTBOUND update messages
                            if (!string.IsNullOrEmpty(queueName))
                            {
                                if (queueName.Equals(QueueName.VariantPriceUpdate.ToString(), StringComparison.OrdinalIgnoreCase))
                                {
                                    var updateVariantPricesRequest = JsonConvert.DeserializeObject<UpdateVariantPricesRequest>(message);
                                    _logger.LogInformation("[ShopifyUpdateRMQService] Updating variant price for variant ID: {VariantId}", updateVariantPricesRequest.VariantId);
                                    await transactionHistoryService.LogTransactionAsync(transaction);
                                    await shopifyUpdateService.UpdateVariantPricesAsync(updateVariantPricesRequest);
                                }
                                else if (queueName.Equals(QueueName.VariantLocationUpdate.ToString(), StringComparison.OrdinalIgnoreCase))
                                {
                                    var updateVariantLocationRequest = JsonConvert.DeserializeObject<UpdateVariantLocationPriceRequest>(message);
                                    _logger.LogInformation("[ShopifyUpdateRMQService] Updating variant location price for variant ID: {VariantId}", updateVariantLocationRequest.VariantId);
                                    await transactionHistoryService.LogTransactionAsync(transaction);
                                    await shopifyUpdateService.UpdateVariantLocationPriceAsync(updateVariantLocationRequest);
                                }
                                else if (queueName.Equals(QueueName.InventoryLevelUpdate.ToString(), StringComparison.OrdinalIgnoreCase))
                                {
                                    var jsonBody = System.Text.Json.JsonSerializer.Deserialize<UpdateInventoryRequest>(body);
                                    _logger.LogInformation("[ShopifyUpdateRMQService] Processing inventory level update");
                                    await transactionHistoryService.LogTransactionAsync(transaction);
                                    await shopifyUpdateService.UpdateInventoryLevelsAsync(jsonBody);
                                }
                                else if (queueName.Equals(QueueName.FitmentSync.ToString(), StringComparison.OrdinalIgnoreCase))
                                {
                                    var fitmentRequest = JsonConvert.DeserializeObject<FitmentUpsertDTO>(message);
                                    var fitmentService = scopedProvider.GetRequiredService<IFitmentService>();
                                    _logger.LogInformation("[ShopifyUpdateRMQService] Processing fitment sync with mode: {Mode}", fitmentRequest?.Mode);
                                    await transactionHistoryService.LogTransactionAsync(transaction);
                                    await fitmentService.UpsertFitmentDataAsync(fitmentRequest);
                                }
                            }

                            // Acknowledge message on success
                            _logger.LogDebug("[ShopifyUpdateRMQService] Acknowledging message with delivery tag: {DeliveryTag}", ea.DeliveryTag);
                            await dedicatedChannel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                            _logger.LogDebug("[ShopifyUpdateRMQService] Message acknowledged successfully");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "[ShopifyUpdateRMQService] ❌ ERROR processing message from queue {QueueName}. Retry Count: {RetryCount}, Exception Type: {ExceptionType}, Message: {Message}, StackTrace: {StackTrace}", 
                                queueName, retryCount, ex.GetType().FullName, ex.Message, ex.StackTrace);

                            // Determine if error is transient or permanent
                            bool isTransientError = IsTransientError(ex);

                            _logger.LogWarning("[ShopifyUpdateRMQService] 🔍 Error Classification: {ErrorType} | Exception: {ExceptionType} | Retry: {RetryCount}/5 | HasStatusCode: {HasStatusCode}", 
                                isTransientError ? "TRANSIENT" : "PERMANENT", 
                                ex.GetType().Name,
                                retryCount,
                                (ex as HttpRequestException)?.StatusCode.HasValue ?? false);

                            if (isTransientError && retryCount < 5)
                            {
                                // Transient errors with retries remaining: republish with incremented retry count
                                _logger.LogWarning("[ShopifyUpdateRMQService] ♻️ TRANSIENT error detected (Attempt {Attempt}/5), RETRYING message with delivery tag: {DeliveryTag}", 
                                    retryCount + 1, ea.DeliveryTag);
                                
                                try
                                {
                                    // Publish message back to queue with incremented retry count
                                    var props = new BasicProperties
                                    {
                                        Headers = new Dictionary<string, object>
                                        {
                                            { "x-retry-count", retryCount + 1 }
                                        },
                                        Persistent = true
                                    };

                                    await dedicatedChannel.BasicPublishAsync(
                                        exchange: "",
                                        routingKey: queueName,
                                        mandatory: false,
                                        basicProperties: props,
                                        body: body);

                                    // Acknowledge the original message to remove it from queue
                                    await dedicatedChannel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                                    _logger.LogInformation("[ShopifyUpdateRMQService] ✅ Message republished with retry count {RetryCount}", retryCount + 1);
                                }
                                catch (Exception repubEx)
                                {
                                    _logger.LogError(repubEx, "[ShopifyUpdateRMQService] ❌ FAILED to republish message, sending to DLQ: {RepubError}", repubEx.Message);
                                    await dedicatedChannel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                                }
                            }
                            else
                            {
                                // Permanent errors OR max retries exceeded: send to dead-letter
                                var reason = isTransientError 
                                    ? $"Max retries (5) exceeded for transient error" 
                                    : "Permanent error detected";
                                _logger.LogWarning("[ShopifyUpdateRMQService] ☠️ {Reason}, sending to DLQ with delivery tag: {DeliveryTag}", 
                                    reason, ea.DeliveryTag);
                                
                                try
                                {
                                    await dedicatedChannel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                                    _logger.LogInformation("[ShopifyUpdateRMQService] ✅ Message rejected to DLQ successfully");
                                }
                                catch (Exception nackEx)
                                {
                                    _logger.LogError(nackEx, "[ShopifyUpdateRMQService] ❌ FAILED to send message to DLQ: {NackError}", nackEx.Message);
                                }
                            }
                        }
                    };

                    _logger.LogDebug("[ShopifyUpdateRMQService] Setting up consumer for queue: {QueueName}", queue);
                    await dedicatedChannel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);
                    await dedicatedChannel.BasicConsumeAsync(queue: queue,
                                     autoAck: false,
                                     consumer: consumer);
                    _logger.LogInformation("[ShopifyUpdateRMQService] Consumer setup successfully for queue: {QueueName}", queue);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[ShopifyUpdateRMQService] Error setting up consumer for queue {QueueName}: {Message}", queue, ex.Message);
                    
                    // Clean up the channel if setup failed
                    dedicatedChannel?.Dispose();
                    throw;
                }
                // Note: Channel is kept alive for the consumer - it will be disposed when the service is disposed
                // The consumer needs the channel to remain open to receive messages
            }
        }

        private bool IsTransientError(Exception ex)
        {
            // Check for transient errors (timeouts, network issues)
            if (ex is TimeoutException || ex is TaskCanceledException)
            {
                return true;
            }

            // Check HttpRequestException with status code
            if (ex is HttpRequestException httpEx)
            {
                // Check if we have a status code
                if (httpEx.StatusCode.HasValue)
                {
                    var statusCode = (int)httpEx.StatusCode.Value;
                    
                    // 5xx server errors are transient
                    if (statusCode >= 500 && statusCode <= 599)
                    {
                        _logger.LogDebug("[ShopifyUpdateRMQService] HTTP {StatusCode} detected - treating as transient error", statusCode);
                        return true;
                    }
                    
                    // 429 Rate Limit is transient
                    if (statusCode == 429)
                    {
                        _logger.LogDebug("[ShopifyUpdateRMQService] HTTP 429 Rate Limit detected - treating as transient error");
                        return true;
                    }
                    
                    // 408 Request Timeout is transient
                    if (statusCode == 408)
                    {
                        _logger.LogDebug("[ShopifyUpdateRMQService] HTTP 408 Timeout detected - treating as transient error");
                        return true;
                    }
                    
                    // 4xx client errors are permanent (except 408, 429)
                    if (statusCode >= 400 && statusCode <= 499)
                    {
                        _logger.LogDebug("[ShopifyUpdateRMQService] HTTP {StatusCode} detected - treating as permanent error", statusCode);
                        return false;
                    }
                }
                
                // If no status code, check message for indicators
                var message = httpEx.Message.ToLower();
                if (message.Contains("timeout") || 
                    message.Contains("connection") ||
                    message.Contains("network") ||
                    message.Contains("timed out"))
                {
                    return true;
                }
                
                // Default HttpRequestException without clear indicators: treat as transient
                _logger.LogDebug("[ShopifyUpdateRMQService] HttpRequestException without status code - treating as transient by default");
                return true;
            }

            // Check for inner HttpRequestException
            if (ex.InnerException is HttpRequestException innerHttpEx)
            {
                return IsTransientError(innerHttpEx); // Recursive check
            }

            // Check exception message for transient indicators
            var exMessage = ex.Message.ToLower();
            if (exMessage.Contains("timeout") || 
                exMessage.Contains("connection") ||
                exMessage.Contains("network") ||
                exMessage.Contains("timed out") ||
                exMessage.Contains("no such host") ||
                exMessage.Contains("connection refused"))
            {
                return true;
            }

            // All other errors (validation, deserialization, null reference, etc.) are permanent
            _logger.LogDebug("[ShopifyUpdateRMQService] Exception type {ExceptionType} - treating as permanent error", ex.GetType().Name);
            return false;
        }

        public async Task SendDataToQueue(string jsonBody, string queueName)
        {
            IChannel channel = null;
            try
            {
                // Create channel from shared connection
                channel = await CreateChannelAsync();
                _logger.LogDebug("[ShopifyUpdateRMQService] ✅ Channel created for sending to queue: {QueueName}", queueName);

                // Try to declare queue with DLX, fallback to without DLX if queue already exists
                try
                {
                    // Declare dead-letter exchange and queue
                    var dlxName = "dlx.shopify.updates";
                    var dlqName = $"{queueName}.dlq";
                    
                    _logger.LogDebug("[ShopifyUpdateRMQService] Declaring dead-letter exchange: {DLXName}", dlxName);
                    await channel.ExchangeDeclareAsync(
                        exchange: dlxName,
                        type: "direct",
                        durable: true,
                        autoDelete: false,
                        arguments: null
                    );
                    
                    _logger.LogDebug("[ShopifyUpdateRMQService] Declaring dead-letter queue: {DLQName}", dlqName);
                    
                    // Set DLQ with 14-day TTL (1,209,600,000 ms) to prevent message loss
                    var dlqArgs = new Dictionary<string, object>
                    {
                        { "x-message-ttl", 1209600000 } // 14 days in milliseconds
                    };
                    
                    await channel.QueueDeclareAsync(
                        queue: dlqName,
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: dlqArgs
                    );
                    
                    await channel.QueueBindAsync(
                        queue: dlqName,
                        exchange: dlxName,
                        routingKey: queueName
                    );
                    
                    // Declare main queue with DLX configuration
                    var queueArgs = new Dictionary<string, object>
                    {
                        { "x-dead-letter-exchange", dlxName },
                        { "x-dead-letter-routing-key", queueName }
                    };
                    
                    await channel.QueueDeclareAsync(queue: queueName,
                                            durable: true,
                                            exclusive: false,
                                            autoDelete: false,
                                            arguments: queueArgs);
                }
                catch (RabbitMQ.Client.Exceptions.OperationInterruptedException ex) when (ex.Message.Contains("PRECONDITION_FAILED"))
                {
                    _logger.LogWarning("[ShopifyUpdateRMQService] Queue {QueueName} already exists without DLX. Using existing queue. To enable DLX, delete and recreate the queue.", queueName);
                    
                    // Dispose the old channel and create a new one from the shared connection
                    channel?.Dispose();
                    channel = await CreateChannelAsync();
                    
                    // Declare queue without DLX (will use existing queue)
                    await channel.QueueDeclareAsync(queue: queueName,
                                            durable: true,
                                            exclusive: false,
                                            autoDelete: false,
                                            arguments: null);
                }

                var body = Encoding.UTF8.GetBytes(jsonBody.ToString());
                var props = new BasicProperties();

                _logger.LogDebug("[ShopifyUpdateRMQService] Publishing message to queue: {QueueName}", queueName);
                await channel.BasicPublishAsync("",
                                    routingKey: queueName,
                                    mandatory: false,
                                    basicProperties: props,
                                    body: body);

                _logger.LogInformation("[ShopifyUpdateRMQService] Successfully sent message to queue: {QueueName}", queueName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ShopifyUpdateRMQService] Error occurred in SendDataToQueue for queue {QueueName}: {Message}", queueName, ex.Message);
                throw;
            }
            finally
            {
                // Dispose the channel after the message is sent (short-lived channel for publishing)
                channel?.Dispose();
            }
        }

        /// <summary>
        /// Disposes the shared RabbitMQ connection and resources
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _logger.LogInformation("[ShopifyUpdateRMQService] Disposing service and cleaning up RabbitMQ connection");

            try
            {
                _connection?.Dispose();
                _connectionLock?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ShopifyUpdateRMQService] Error during disposal: {Message}", ex.Message);
            }
            finally
            {
                _disposed = true;
                _logger.LogInformation("[ShopifyUpdateRMQService] ✅ Service disposed successfully");
            }
        }
    }
}

