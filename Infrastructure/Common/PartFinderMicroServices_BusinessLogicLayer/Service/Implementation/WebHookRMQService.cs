using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PartFinderMicroServices_BusinessLogicLayer.Repository.Interface;
using PartFinderMicroServices_BusinessLogicLayer.Service.Interface;
using PartFinderMicroServices_DataAccessLayer.Entities.DTOs;
using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.TransactionHistoryDTO;
using PartFinderMicroServices_DataAccessLayer.Entities.RabbitMQ;
using PartFinderMicroServices_DataAccessLayer.Enum;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace PartFinderMicroServices_BusinessLogicLayer.Service.Implementation
{
    /// <summary>
    /// Service for processing INBOUND webhooks FROM Shopify via RabbitMQ
    /// Handles Product, Collection, and InventoryLevel webhook messages
    /// </summary>
    public class WebHookRMQService : IWebHookRMQService, IDisposable
    {
        private readonly RabbitMQSetting _rabbitMQSetting;
        private readonly IServiceProvider _serviceProvider;
        private readonly ITransactionHistoryService _transactionHistoryService;
        private readonly IShopifyRepository _shopifyRepo;
        private readonly ILogger<WebHookRMQService> _logger;
        
        // Connection management - single long-lived connection
        private IConnection _connection;
        private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(1, 1);
        private bool _disposed = false;

        public WebHookRMQService(
            IOptions<RabbitMQSetting> rabbitMQSetting,
            IServiceProvider serviceProvider,
            ILogger<WebHookRMQService> logger,
            ITransactionHistoryService transactionHistoryService,
            IShopifyRepository shopifyRepo )
        {
            _rabbitMQSetting = rabbitMQSetting.Value;
            _serviceProvider = serviceProvider;
            _logger = logger;
            _transactionHistoryService = transactionHistoryService;
            _shopifyRepo = shopifyRepo;
            _logger.LogInformation("[WebHookRMQService] Service initialized successfully with host: {HostName}, port: {Port}",
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

                _logger.LogInformation("[WebHookRMQService] Creating new shared RabbitMQ connection to {HostName}:{Port}", 
                    _rabbitMQSetting.HostName, _rabbitMQSetting.Port);

                if (string.IsNullOrWhiteSpace(_rabbitMQSetting.HostName))
                {
                    _logger.LogError("[WebHookRMQService] HostName is null or whitespace, cannot create connection");
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
                _logger.LogInformation("[WebHookRMQService] ✅ Shared RabbitMQ connection created successfully.");
                
                return _connection;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[WebHookRMQService] ❌ Failed to create RabbitMQ connection: {Message}", ex.Message);
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
            _logger.LogDebug("[WebHookRMQService] Created new channel from shared connection");
            return channel;
        }

        /// <summary>
        /// [DEPRECATED] Legacy method for backward compatibility. Use CreateChannelAsync() instead.
        /// Creates a channel from the shared connection.
        /// </summary>
        public async Task<IChannel> InitializeMQ(RabbitMQSetting setting)
        {
            _logger.LogWarning("[WebHookRMQService] InitializeMQ is deprecated. Using shared connection instead of creating new connection.");
            return await CreateChannelAsync();
        }

        public async Task ReceviedData(List<string> queues)
        {
            foreach (var queue in queues)
            {
                _logger.LogDebug("[WebHookRMQService] Processing queue: {QueueName}", queue);
                IChannel dedicatedChannel = null;
                try
                {
                    // Create a dedicated channel for THIS queue from the shared connection
                    dedicatedChannel = await CreateChannelAsync();
                    _logger.LogDebug("[WebHookRMQService] ✅ Channel created for queue: {QueueName}", queue);
                    
                    // Try to declare queue with DLX, fallback to without DLX if queue already exists
                    // Separate try-catch for queue declaration only
                    try
                    {
                        // Declare dead-letter exchange and queue
                        var dlxName = "dlx.shopify.webhooks";
                        var dlqName = $"{queue}.dlq";
                        
                        _logger.LogDebug("[WebHookRMQService] Declaring dead-letter exchange: {DLXName}", dlxName);
                        await dedicatedChannel.ExchangeDeclareAsync(
                            exchange: dlxName,
                            type: "direct",
                            durable: true,
                            autoDelete: false,
                            arguments: null
                        );
                        
                        _logger.LogDebug("[WebHookRMQService] Declaring dead-letter queue: {DLQName}", dlqName);
                        
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
                        _logger.LogDebug("[WebHookRMQService] Declaring queue: {QueueName} with DLX", queue);
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
                        
                        _logger.LogInformation("[WebHookRMQService] ✅ Queue {QueueName} declared WITH DLX configuration. DLX={DLX}, DLQ={DLQ}", queue, dlxName, dlqName);
                    }
                    catch (RabbitMQ.Client.Exceptions.OperationInterruptedException ex) when (ex.Message.Contains("PRECONDITION_FAILED"))
                    {
                        _logger.LogWarning("[WebHookRMQService] ⚠️ Queue {QueueName} already exists with different configuration. Using existing queue. To update configuration, delete queue {QueueName} and restart service.", queue, queue);
                        
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

                            var webHookService = scopedProvider.GetRequiredService<IWebHookService>();
                            var shopifyService = scopedProvider.GetRequiredService<IShopifyService>();
                            var transactionHistoryService = scopedProvider.GetRequiredService<ITransactionHistoryService>();

                            // Process INBOUND webhook messages
                            if (!string.IsNullOrEmpty(queueName))
                            {
                                if (queueName.Equals(QueueName.ProductWebhook.ToString(), StringComparison.OrdinalIgnoreCase))
                                {
                                    _logger.LogInformation("[WebHookRMQService] Processing Product webhook message");
                                    using JsonDocument doc = JsonDocument.Parse(message);
                                    JsonElement root = doc.RootElement;

                                    if (root.TryGetProperty("id", out JsonElement idElement))
                                    {
                                        long id = idElement.GetInt64();
                                        transaction.ProductId = id.ToString();
                                        await transactionHistoryService.LogTransactionAsync(transaction);

                                    // Process product with OEM change detection and fitment sync logic
                                    await ProcessProductWebhookWithOEMDetectionAsync(id, scopedProvider);
                                }
                                    else
                                    {
                                        throw new InvalidOperationException("Product webhook payload is missing required 'id' property.");
                                    }
                                    }
                            else if (queueName.Equals(QueueName.CollectionWebhook.ToString(), StringComparison.OrdinalIgnoreCase))
                            {
                                var jsonBody = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(body);
                                _logger.LogInformation("[WebHookRMQService] Processing Collection webhook message");
                                await transactionHistoryService.LogTransactionAsync(transaction);
                                await webHookService.ProcessCollectionCreatedOrUpdatedAsync(jsonBody);
                            }
                            else if (queueName.Equals(QueueName.InventoryLevelWebhook.ToString(), StringComparison.OrdinalIgnoreCase))
                            {
                                var jsonBody = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(body);
                                _logger.LogInformation("[WebHookRMQService] Processing InventoryLevel webhook message");
                                await transactionHistoryService.LogTransactionAsync(transaction);
                                await webHookService.UpdateInventoryLevel(jsonBody);
                            }
                                else if (queueName.Equals(QueueName.OrderWebhook.ToString(), StringComparison.OrdinalIgnoreCase))
                                {
                                    var jsonBody = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(body);
                                    _logger.LogInformation("[WebHookRMQService] Processing Order webhook message");
                                    
                                    // Extract order ID for transaction logging
                                    if (jsonBody.TryGetProperty("id", out JsonElement orderIdElement))
                                    {
                                        transaction.ProductId = orderIdElement.GetInt64().ToString(); // Reusing ProductId field for order ID
                        }

                                    await transactionHistoryService.LogTransactionAsync(transaction);
                                    await webHookService.ProcessOrderCreatedOrUpdatedAsync(jsonBody);
                                }
                            }

                            // Acknowledge message on success
                            _logger.LogDebug("[WebHookRMQService] Acknowledging message with delivery tag: {DeliveryTag}", ea.DeliveryTag);
                            await dedicatedChannel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                            _logger.LogDebug("[WebHookRMQService] Message acknowledged successfully");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "[WebHookRMQService] ❌ ERROR processing message from queue {QueueName}. Retry Count: {RetryCount}, Exception Type: {ExceptionType}, Message: {Message}", 
                                queueName, retryCount, ex.GetType().FullName, ex.Message);

                            // Determine if error is transient or permanent
                            bool isTransientError = IsTransientError(ex);

                            _logger.LogWarning("[WebHookRMQService] 🔍 Error Classification: {ErrorType} | Exception: {ExceptionType} | Retry: {RetryCount}/5 | HasStatusCode: {HasStatusCode}", 
                                isTransientError ? "TRANSIENT" : "PERMANENT", 
                                ex.GetType().Name,
                                retryCount,
                                (ex as HttpRequestException)?.StatusCode.HasValue ?? false);

                            if (isTransientError && retryCount < 5)
                            {
                                // Transient errors with retries remaining: republish with incremented retry count
                                _logger.LogWarning("[WebHookRMQService] ♻️ TRANSIENT error detected (Attempt {Attempt}/5), RETRYING message with delivery tag: {DeliveryTag}", 
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
                                    _logger.LogInformation("[WebHookRMQService] ✅ Message republished with retry count {RetryCount}", retryCount + 1);
                                }
                                catch (Exception repubEx)
                                {
                                    _logger.LogError(repubEx, "[WebHookRMQService] ❌ FAILED to republish message, sending to DLQ: {RepubError}", repubEx.Message);
                                    await dedicatedChannel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                                }
                            }
                            else
                            {
                                // Permanent errors OR max retries exceeded: send to dead-letter
                                var reason = isTransientError 
                                    ? $"Max retries (5) exceeded for transient error" 
                                    : "Permanent error detected";
                                _logger.LogWarning("[WebHookRMQService] ☠️ {Reason}, sending to DLQ with delivery tag: {DeliveryTag}", 
                                    reason, ea.DeliveryTag);
                                
                                try
                                {
                                    await dedicatedChannel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                                    _logger.LogInformation("[WebHookRMQService] ✅ Message rejected to DLQ successfully");
                                }
                                catch (Exception nackEx)
                                {
                                    _logger.LogError(nackEx, "[WebHookRMQService] ❌ FAILED to send message to DLQ: {NackError}", nackEx.Message);
                                }
                            }
                        }
                    };

                    _logger.LogDebug("[WebHookRMQService] Setting up consumer for queue: {QueueName}", queue);
                    await dedicatedChannel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);
                    await dedicatedChannel.BasicConsumeAsync(queue: queue,
                                     autoAck: false,
                                     consumer: consumer);
                    _logger.LogInformation("[WebHookRMQService] Consumer setup successfully for queue: {QueueName}", queue);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[WebHookRMQService] Error setting up consumer for queue {QueueName}: {Message}", queue, ex.Message);
                    
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
                        _logger.LogDebug("[WebHookRMQService] HTTP {StatusCode} detected - treating as transient error", statusCode);
                        return true;
                    }
                    
                    // 429 Rate Limit is transient
                    if (statusCode == 429)
                    {
                        _logger.LogDebug("[WebHookRMQService] HTTP 429 Rate Limit detected - treating as transient error");
                        return true;
                    }
                    
                    // 408 Request Timeout is transient
                    if (statusCode == 408)
                    {
                        _logger.LogDebug("[WebHookRMQService] HTTP 408 Timeout detected - treating as transient error");
                        return true;
                    }
                    
                    // 4xx client errors are permanent (except 408, 429)
                    if (statusCode >= 400 && statusCode <= 499)
                    {
                        _logger.LogDebug("[WebHookRMQService] HTTP {StatusCode} detected - treating as permanent error", statusCode);
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
                _logger.LogDebug("[WebHookRMQService] HttpRequestException without status code - treating as transient by default");
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
            _logger.LogDebug("[WebHookRMQService] Exception type {ExceptionType} - treating as permanent error", ex.GetType().Name);
            return false;
        }

        public async Task SendDataToQueue(string jsonBody, string queueName)
        {
            IChannel channel = null;
            try
            {
                // Create channel from shared connection
                channel = await CreateChannelAsync();
                _logger.LogDebug("[WebHookRMQService] ✅ Channel created for sending to queue: {QueueName}", queueName);

                // Try to declare queue with DLX, fallback to without DLX if queue already exists
                try
                {
                    // Declare dead-letter exchange and queue
                    var dlxName = "dlx.shopify.webhooks";
                    var dlqName = $"{queueName}.dlq";
                    
                    _logger.LogDebug("[WebHookRMQService] Declaring dead-letter exchange: {DLXName}", dlxName);
                    await channel.ExchangeDeclareAsync(
                        exchange: dlxName,
                        type: "direct",
                        durable: true,
                        autoDelete: false,
                        arguments: null
                    );
                    
                    _logger.LogDebug("[WebHookRMQService] Declaring dead-letter queue: {DLQName}", dlqName);
                    
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
                    _logger.LogWarning("[WebHookRMQService] Queue {QueueName} already exists without DLX. Using existing queue. To enable DLX, delete and recreate the queue.", queueName);
                    
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

                _logger.LogDebug("[WebHookRMQService] Publishing message to queue: {QueueName}", queueName);
                await channel.BasicPublishAsync("",
                                    routingKey: queueName,
                                    mandatory: false,
                                    basicProperties: props,
                                    body: body);

                _logger.LogInformation("[WebHookRMQService] Successfully sent message to queue: {QueueName}", queueName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[WebHookRMQService] Error occurred in SendDataToQueue for queue {QueueName}: {Message}", queueName, ex.Message);
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

            _logger.LogInformation("[WebHookRMQService] Disposing service and cleaning up RabbitMQ connection");

            try
            {
                _connection?.Dispose();
                _connectionLock?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[WebHookRMQService] Error during disposal: {Message}", ex.Message);
            }
            finally
            {
                _disposed = true;
                _logger.LogInformation("[WebHookRMQService] ✅ Service disposed successfully");
            }
        }

        /// <summary>
        /// Processes product webhook with OEM change detection and fitment sync enqueuing logic.
        /// Detects new products or OEM changes, and enqueues to FitmentSync queue if conditions are met.
        /// </summary>
        /// <param name="productId">The numeric Shopify product ID</param>
        /// <param name="shopifyService">The Shopify service instance</param>
        private async Task ProcessProductWebhookWithOEMDetectionAsync(long productId, IServiceProvider scopedProvider)
        {
            _logger.LogInformation("[WebHookRMQService] ProcessProductWebhookWithOEMDetectionAsync - Starting for product ID: {ProductId}", productId);
            var shopifyService = scopedProvider.GetRequiredService<IShopifyService>();
            try
            {
                // Format the Shopify ID
                string shopifyProductId = $"gid://shopify/Product/{productId}";

                // Check if product exists in database
                _logger.LogDebug("[WebHookRMQService] ProcessProductWebhookWithOEMDetectionAsync - Checking if product exists in database: {ShopifyProductId}", shopifyProductId);
                var existingProduct = await _shopifyRepo.GetProductByShopifyIdAsync(shopifyProductId);

                if (existingProduct == null)
                {
                    // New product detected
                    _logger.LogInformation("[WebHookRMQService] ProcessProductWebhookWithOEMDetectionAsync - NEW PRODUCT detected: {ShopifyProductId}. Importing product.", shopifyProductId);

                    // Import the new product
                    await shopifyService.ImportProductByIdAsync(productId);
                    _logger.LogInformation("[WebHookRMQService] ProcessProductWebhookWithOEMDetectionAsync - Product imported successfully: {ShopifyProductId}", shopifyProductId);

                    // After import, fetch the product to check if it needs fitment sync
                    existingProduct = await _shopifyRepo.GetProductByShopifyIdAsync(shopifyProductId);

                    if (existingProduct != null && existingProduct.Is_Piece && existingProduct.Exact_Fit)
                    {
                        _logger.LogInformation("[WebHookRMQService] ProcessProductWebhookWithOEMDetectionAsync - New product has Is_Piece=true and Exact_Fit=true. Enqueuing to FitmentSync.");
                        await EnqueueFitmentSyncAsync( "by_product" , productId: productId.ToString());
                    }
                    else
                    {
                        _logger.LogDebug("[WebHookRMQService] ProcessProductWebhookWithOEMDetectionAsync - New product does not meet fitment sync criteria (Is_Piece={IsPiece}, Exact_Fit={ExactFit})",
                            existingProduct?.Is_Piece ?? false, existingProduct?.Exact_Fit ?? false);
                    }

                    return;
                }

                // Existing product - check for OEM changes
                _logger.LogInformation("[WebHookRMQService] ProcessProductWebhookWithOEMDetectionAsync - EXISTING PRODUCT found (DB ID: {ProductId}). Checking for OEM changes.", existingProduct.Id);

                // Get existing variants from database with OEM data
                var existingVariants = await _shopifyRepo.GetVariantsByProductIdAsync(existingProduct.Id);
                _logger.LogDebug("[WebHookRMQService] ProcessProductWebhookWithOEMDetectionAsync - Found {ExistingVariantCount} existing variants in database", existingVariants?.Count ?? 0);

                // Fetch latest product data from Shopify
                _logger.LogDebug("[WebHookRMQService] ProcessProductWebhookWithOEMDetectionAsync - Fetching latest product data from Shopify");
                string jsonData = await shopifyService.ImportProductFromShopifyAsync(productId.ToString());

                if (string.IsNullOrEmpty(jsonData) || jsonData.Contains("error"))
                {
                    _logger.LogWarning("[WebHookRMQService] ProcessProductWebhookWithOEMDetectionAsync - Failed to fetch product data from Shopify.");
                    await shopifyService.ImportProductByIdAsync(productId);
                    return;
                }

                _logger.LogDebug("[WebHookRMQService] ProcessProductWebhookWithOEMDetectionAsync - Successfully fetched product data. Parsing JSON to check for OEM changes.");

                // Parse JSON to extract variants and compare OEM
                using var doc = JsonDocument.Parse(jsonData);
                var root = doc.RootElement;

                if (!root.TryGetProperty("data", out var dataElement) ||
                    !dataElement.TryGetProperty("product", out var productElement))
                {
                    _logger.LogWarning("[WebHookRMQService] ProcessProductWebhookWithOEMDetectionAsync - Invalid JSON structure.");
                    await shopifyService.ImportProductByIdAsync(productId);
                    return;
                }

                // Get variants from JSON
                if (!productElement.TryGetProperty("variants", out var variantsNode) ||
                    !variantsNode.TryGetProperty("edges", out var variantEdgesNode))
                {
                    _logger.LogWarning("[WebHookRMQService] ProcessProductWebhookWithOEMDetectionAsync - No variants found in JSON. Falling back to standard import.");
                    await shopifyService.ImportProductByIdAsync(productId);
                    return;
                }

                // Compare OEM values between existing and new data
                bool oemHasChanged = false;
                var oemChangesLog = new List<string>();

                foreach (var variantEdge in variantEdgesNode.EnumerateArray())
                {
                    if (!variantEdge.TryGetProperty("node", out var variantNode))
                        continue;

                    if (!variantNode.TryGetProperty("id", out var variantIdElement))
                        continue;

                    string variantShopifyId = variantIdElement.GetString();
                    string variantTitle = variantNode.TryGetProperty("title", out var titleElement) ? titleElement.GetString() : "Unknown";

                    // Find existing variant
                    var existingVariant = existingVariants?.FirstOrDefault(v => v.ShopifyId == variantShopifyId);

                    if (existingVariant == null)
                    {
                        _logger.LogDebug("[WebHookRMQService] ProcessProductWebhookWithOEMDetectionAsync - New variant detected: {VariantShopifyId}", variantShopifyId);
                        continue; // New variant, not an OEM change
                    }

                    // Extract official OEM from metafields (namespace: "custom", key: "oem")
                    string existingOemName = existingVariant.OEMMetaField;
                    string newOemValue = null;

                    if (variantNode.TryGetProperty("metafields", out var metafieldsNode) &&
                        metafieldsNode.TryGetProperty("edges", out var metafieldEdgesNode))
                    {
                        foreach (var metafieldEdge in metafieldEdgesNode.EnumerateArray())
                        {
                            if (!metafieldEdge.TryGetProperty("node", out var metafieldNode))
                                continue;

                            if (metafieldNode.TryGetProperty("namespace", out var namespaceElement) &&
                                metafieldNode.TryGetProperty("key", out var keyElement))
                            {
                                string metafieldNamespace = namespaceElement.GetString();
                                string metafieldKey = keyElement.GetString();

                                if (metafieldNamespace == "custom" && metafieldKey == "oem")
                                {
                                    if (metafieldNode.TryGetProperty("value", out var valueElement))
                                    {
                                        newOemValue = valueElement.GetString();
                                        _logger.LogDebug("[WebHookRMQService] ProcessProductWebhookWithOEMDetectionAsync - Found OEM metafield for variant {VariantShopifyId}: {OemValue}",
                                            variantShopifyId, newOemValue ?? "null");
                                    }
                                    break;
                                }
                            }
                        }
                    }

                    // Compare OEM values
                    if (existingOemName != newOemValue && existingProduct.Is_Piece && existingProduct.Exact_Fit)
                    {
                        var variantId = existingVariant.ShopifyId.Split('/').Last();
                        await EnqueueFitmentSyncAsync( "by_variant" , productId.ToString() , variantId);

                        //oemHasChanged = true;
                        string changeLog = $"Variant '{variantTitle}' (Shopify ID: {variantShopifyId}, DB ID: {existingVariant.Id}): OEM changed from '{existingOemName ?? "null"}' to '{newOemValue ?? "null"}'";
                        oemChangesLog.Add(changeLog);
                        _logger.LogInformation("[WebHookRMQService] ProcessProductWebhookWithOEMDetectionAsync - OEM CHANGE DETECTED: {ChangeLog}", changeLog);
                    }
                }

                // Always update the product
                _logger.LogDebug("[WebHookRMQService] ProcessProductWebhookWithOEMDetectionAsync - Calling ImportProductByIdAsync to update product");
                await shopifyService.ImportProductByIdAsync(productId);
                _logger.LogInformation("[WebHookRMQService] ProcessProductWebhookWithOEMDetectionAsync - Product update completed successfully for {ShopifyProductId}", shopifyProductId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[WebHookRMQService] ProcessProductWebhookWithOEMDetectionAsync - Error processing product {ProductId}: {Message}. StackTrace: {StackTrace}",
                    productId, ex.Message, ex.StackTrace);
                throw;
            }
        }

        /// <summary>
        /// Enqueues a message to the FitmentSync queue for products that need fitment data processing.
        /// </summary>
        /// <param name="productId">The numeric Shopify product ID</param>
        /// <param name="variantId">The formatted Shopify product ID (gid://shopify/Product/{id})</param>
        /// <param name="mode">The reason for enqueuing (for logging/context)</param>
        private async Task EnqueueFitmentSyncAsync(string mode , string? productId = null , string variantId = null)
        {
            var shopifyService = _serviceProvider.GetRequiredService<IShopifyService>();
            try
            {
                _logger.LogInformation("[WebHookRMQService] EnqueueFitmentSyncAsync - Enqueuing product {ProductId} to FitmentSync queue. Reason: {Reason}",
                    productId, mode);

                // Create the message payload
                var fitmentSyncMessage = new FitmentUpsertDTO
                {
                    Mode = mode,
                    ProductId = productId,
                    VariantId = variantId
                };

                // Serialize to JSON
                string jsonMessage = System.Text.Json.JsonSerializer.Serialize(fitmentSyncMessage);

                _logger.LogDebug("[WebHookRMQService] EnqueueFitmentSyncAsync - Message payload: {JsonMessage}", jsonMessage);

                // Send to FitmentSync queue
                await shopifyService.SendUpdateToQueueAsync(jsonMessage, QueueName.FitmentSync.ToString());

                _logger.LogInformation("[WebHookRMQService] EnqueueFitmentSyncAsync - Successfully enqueued product {ProductId} to FitmentSync queue", productId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[WebHookRMQService] EnqueueFitmentSyncAsync - Error enqueuing product {ProductId} to FitmentSync queue: {Message}",
                    productId, ex.Message);
                // Don't rethrow - we don't want to fail the entire webhook processing if fitment sync enqueuing fails
            }
        }
    }
}

