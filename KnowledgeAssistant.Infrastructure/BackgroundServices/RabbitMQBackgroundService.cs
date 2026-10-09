using KnowledgeAssistant.Application.Handlers;
using KnowledgeAssistant.Domain.Entities.Emails;
using KnowledgeAssistant.Domain.Entities.Notifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace KnowledgeAssistant.Infrastructure.BackgroundServices
{
    public class RabbitMQBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RabbitMQBackgroundService> _logger;
        private IConnection? _connection;
        private IChannel? _channel;
        private readonly List<IChannel> _channels = new();
        private readonly bool _enabled;

        public RabbitMQBackgroundService(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<RabbitMQBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _logger = logger;
            _enabled = _configuration.GetValue<bool>("RabbitMQ:Enabled");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_enabled)
            {
                return;
            }

            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMQ:HostName"]!,
                UserName = _configuration["RabbitMQ:UserName"]!,
                Password = _configuration["RabbitMQ:Password"]!
            };

            _connection = await factory.CreateConnectionAsync(stoppingToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

            _logger.LogInformation("RabbitMQ background service started.");

            await StartConsumersAsync(stoppingToken);

            try
            {
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {

            }
        }

        private async Task StartConsumersAsync(CancellationToken ct)
        {
            await StartConsumerAsync<SendEmailMessage>("emails", prefetch: 1, ct);
            await StartConsumerAsync<InsertRecipientsChunkMessage>("notification-recipients", prefetch: 2, ct);
        }

        private async Task StartConsumerAsync<T>(string queue, ushort prefetch, CancellationToken ct)
        {
            var channel = await _connection!.CreateChannelAsync(cancellationToken: ct);
            _channels.Add(channel);

            await channel.QueueDeclareAsync(
                queue: queue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: ct);

            await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: prefetch, global: false, cancellationToken: ct);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += (_, eventArgs) => ProcessMessageAsync<T>(channel, queue, eventArgs, ct);

            await channel.BasicConsumeAsync(queue: queue, autoAck: false, consumer: consumer, cancellationToken: ct);

            _logger.LogInformation("RabbitMQ consumer started for queue {Queue}.", queue);
        }

        private async Task ProcessMessageAsync<T>(IChannel channel, string queue, BasicDeliverEventArgs eventArgs, CancellationToken ct)
        {
            try
            {
                var json = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
                var message = JsonSerializer.Deserialize<T>(json)
                    ?? throw new InvalidOperationException($"Could not deserialize message from queue '{queue}'.");

                using var scope = _scopeFactory.CreateScope();
                var handler = GetHandler<T>(scope.ServiceProvider, queue);

                await handler.HandleAsync(message, ct);

                await channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false, cancellationToken: ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing RabbitMQ message from queue {Queue}.", queue);

                // Retry once; if it was already redelivered and failed again, stop looping
                await channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: !eventArgs.Redelivered, cancellationToken: ct);
            }
        }

        private IMessageHandler<T> GetHandler<T>(IServiceProvider serviceProvider, string queue)
        {
            return queue switch
            {
                "emails" or "notification-recipients" => serviceProvider.GetRequiredService<IMessageHandler<T>>(),
                _ => throw new InvalidOperationException($"No message handler configured for queue '{queue}'.")
            };
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("RabbitMQ background service stopping.");

            if (_channel != null)
            {
                await _channel.CloseAsync(cancellationToken);
            }

            if (_connection != null)
            {
                await _connection.CloseAsync(cancellationToken);
            }

            await base.StopAsync(cancellationToken);
        }
    }
}