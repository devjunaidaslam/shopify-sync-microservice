using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ShopifySync_BusinessLogicLayer.Service.Interface;
using ShopifySync_DataAccessLayer.Entities.DTOs.PredikoDTO;
using ShopifySync_DataAccessLayer.Entities.RabbitMQ;
using ShopifySync_DataAccessLayer.Enum;
using RabbitMQ.Client;
using System.Text;

namespace ShopifySync_BusinessLogicLayer.Service.Implementation
{
    public class PredikoOrderRMQService : IPredikoOrderRMQService
    {
        private readonly RabbitMQSetting _rabbitMQSetting;
        private readonly ILogger<PredikoOrderRMQService> _logger;
        private IConnection? _connection;
        private IChannel? _channel;
        private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(1, 1);

        public PredikoOrderRMQService(
            IOptions<RabbitMQSetting> rabbitMQSetting,
            ILogger<PredikoOrderRMQService> logger)
        {
            _rabbitMQSetting = rabbitMQSetting.Value;
            _logger = logger;
            
            _logger.LogInformation("[PredikoOrderRMQService] Service initialized with host: {HostName}, port: {Port}",
                _rabbitMQSetting.HostName, _rabbitMQSetting.Port);
        }

        private async Task<IChannel?> GetOrCreateChannelAsync()
        {
            await _connectionLock.WaitAsync();
            try
            {
                if (_channel != null && _channel.IsOpen)
                {
                    _logger.LogDebug("[PredikoOrderRMQService] Reusing existing channel");
                    return _channel;
                }

                _logger.LogInformation("[PredikoOrderRMQService] Creating new RabbitMQ connection and channel");

                if (string.IsNullOrWhiteSpace(_rabbitMQSetting.HostName))
                {
                    _logger.LogError("[PredikoOrderRMQService] HostName is null or whitespace, cannot initialize connection");
                    return null;
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
                _logger.LogDebug("[PredikoOrderRMQService] RabbitMQ connection created successfully");

                _channel = await _connection.CreateChannelAsync();
                _logger.LogDebug("[PredikoOrderRMQService] RabbitMQ channel created successfully");

                await DeclareQueueWithDLXAsync(_channel);

                return _channel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PredikoOrderRMQService] Error creating RabbitMQ connection/channel: {Message}", ex.Message);
                return null;
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        private async Task DeclareQueueWithDLXAsync(IChannel channel)
        {
            var queueName = QueueName.PredikoOrderQueue.ToString();
            var dlxName = "dlx.prediko";
            var dlqName = $"{queueName}.dlq";

            _logger.LogDebug("[PredikoOrderRMQService] Declaring dead-letter exchange: {DLXName}", dlxName);
            
            await channel.ExchangeDeclareAsync(
                exchange: dlxName,
                type: "direct",
                durable: true,
                autoDelete: false,
                arguments: null
            );

            _logger.LogDebug("[PredikoOrderRMQService] Declaring dead-letter queue: {DLQName}", dlqName);
            
            // Declare dead-letter queue with 14-day TTL
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

            _logger.LogDebug("[PredikoOrderRMQService] Declaring main queue: {QueueName}", queueName);
            
            var queueArgs = new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", dlxName },
                { "x-dead-letter-routing-key", queueName }
            };

            await channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: queueArgs
            );

            _logger.LogInformation("[PredikoOrderRMQService] Queue {QueueName} declared successfully with DLQ: {DLQName}", 
                queueName, dlqName);
        }

        public async Task<bool> PublishOrderAsync(PredikoOrderMessageDTO orderMessage)
        {
            var queueName = QueueName.PredikoOrderQueue.ToString();
            
            try
            {
                _logger.LogInformation("[PredikoOrderRMQService] Publishing order {OrderId} (Shopify: {ShopifyOrderId}) to Prediko queue",
                    orderMessage.Order.Id, orderMessage.Order.ShopifyOrderId);

                var channel = await GetOrCreateChannelAsync();
                if (channel == null)
                {
                    _logger.LogError("[PredikoOrderRMQService] Failed to get channel, cannot publish message");
                    return false;
                }

                var messageJson = JsonConvert.SerializeObject(orderMessage, new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    NullValueHandling = NullValueHandling.Include,
                    Formatting = Formatting.None
                });

                var body = Encoding.UTF8.GetBytes(messageJson);

                _logger.LogDebug("[PredikoOrderRMQService] Message size: {Size} bytes", body.Length);

                var properties = new BasicProperties
                {
                    Persistent = true,
                    ContentType = "application/json",
                    DeliveryMode = DeliveryModes.Persistent,
                    Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                };

                await channel.BasicPublishAsync(
                    exchange: string.Empty,
                    routingKey: queueName,
                    mandatory: false,
                    basicProperties: properties,
                    body: body
                );

                _logger.LogInformation("[PredikoOrderRMQService] ✅ Successfully published order {OrderId} to queue {QueueName}",
                    orderMessage.Order.Id, queueName);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PredikoOrderRMQService] ❌ Error publishing order {OrderId} to Prediko queue: {Message}",
                    orderMessage.Order?.Id, ex.Message);
                return false;
            }
        }
    }
}
