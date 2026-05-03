using Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Net.Security;
using System.Text;
using System.Text.Json;

namespace Infrastructure.Services
{
    
    public class OrderEventPublisher : IOrderEventPublisher, IDisposable
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<OrderEventPublisher> _logger;
        private IConnection _connection;
        private IChannel _channel;
        private readonly string _exchangeName;

        public OrderEventPublisher(IConfiguration configuration, ILogger<OrderEventPublisher> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _exchangeName = _configuration["OrderQueue:Exchange"] ?? "order.exchange";
            
            // Initialize asynchronously in the background
            Task.Run(InitializeAsync);
        }

        private async Task InitializeAsync()
        {
            try
            {
                var factory = new ConnectionFactory()
                {
                    HostName = _configuration["RabbitMQ:HostName"] ?? "localhost",
                    UserName = _configuration["RabbitMQ:UserName"] ?? "guest",
                    Password = _configuration["RabbitMQ:Password"] ?? "guest",
                    VirtualHost = _configuration["RabbitMQ:VirtualHost"] ?? "/",
                    Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
                    RequestedHeartbeat = TimeSpan.FromSeconds(60),
                    AutomaticRecoveryEnabled = true,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
                };

                // Configure SSL if needed
                if (bool.Parse(_configuration["RabbitMQ:SslEnabled"] ?? "false"))
                {
                    factory.Ssl = new SslOption
                    {
                        Enabled = true,
                        AcceptablePolicyErrors = SslPolicyErrors.RemoteCertificateNameMismatch |
                                               SslPolicyErrors.RemoteCertificateChainErrors
                    };
                }

                _connection = await factory.CreateConnectionAsync();
                _channel = await _connection.CreateChannelAsync();

                // Declare exchange
                await _channel.ExchangeDeclareAsync(
                    exchange: _exchangeName,
                    type: ExchangeType.Direct,
                    durable: true,
                    autoDelete: false);

                _logger.LogInformation("Order Event Publisher connected to RabbitMQ successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect Order Event Publisher to RabbitMQ");
                throw;
            }
        }

        public async Task PublishOrderCreatedAsync(int tenantId, int userId, decimal amount, string description, string customerEmail)
        {
            try
            {
                var orderMessage = new
                {
                    TenantId = tenantId,
                    UserId = userId,
                    Amount = amount,
                    Currency = "USD",
                    Description = description,
                    CustomerEmail = customerEmail,
                    Metadata = new Dictionary<string, string>(),
                    MessageType = "OrderCreated",
                    Timestamp = DateTime.UtcNow,
                    CorrelationId = Guid.NewGuid().ToString()
                };

                await PublishMessageAsync(orderMessage, "order.queue");
                
                _logger.LogInformation("OrderCreated event published: TenantId={TenantId}, UserId={UserId}, Amount={Amount}", 
                    tenantId, userId, amount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish OrderCreated event: TenantId={TenantId}, UserId={UserId}", 
                    tenantId, userId);
                throw;
            }
        }

        public async Task PublishOrderUpdatedAsync(int orderId, int tenantId, string status)
        {
            try
            {
                var statusMessage = new
                {
                    OrderId = orderId,
                    TenantId = tenantId,
                    Status = status,
                    MessageType = "OrderStatusUpdated",
                    Timestamp = DateTime.UtcNow,
                    CorrelationId = Guid.NewGuid().ToString()
                };

                await PublishMessageAsync(statusMessage, "order.status.queue");
                
                _logger.LogInformation("OrderStatusUpdated event published: OrderId={OrderId}, TenantId={TenantId}, Status={Status}", 
                    orderId, tenantId, status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish OrderStatusUpdated event: OrderId={OrderId}, TenantId={TenantId}", 
                    orderId, tenantId);
                throw;
            }
        }

        private async Task PublishMessageAsync<T>(T message, string queueName)
        {
            try
            {
                var messageJson = JsonSerializer.Serialize(message);
                var body = Encoding.UTF8.GetBytes(messageJson);

                // Declare queue
                await _channel.QueueDeclareAsync(
                    queue: queueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                // Bind queue to exchange
                await _channel.QueueBindAsync(
                    queue: queueName,
                    exchange: _exchangeName,
                    routingKey: queueName);

                // Publish message
                var properties = new BasicProperties();
                properties.Persistent = true;
                properties.MessageId = Guid.NewGuid().ToString();
                properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                properties.ContentType = "application/json";

                await _channel.BasicPublishAsync(
                    exchange: _exchangeName,
                    routingKey: queueName,
                    mandatory: false,
                    basicProperties: properties,
                    body: body);

                _logger.LogDebug("Message published to queue {QueueName}: {MessageId}", queueName, properties.MessageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish message to queue {QueueName}", queueName);
                throw;
            }
        }

        public void Dispose()
        {
            try
            {
                _channel?.CloseAsync();
                _connection?.CloseAsync();
                _logger.LogInformation("Order Event Publisher connection closed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing Order Event Publisher connection");
            }
        }
    }
}
