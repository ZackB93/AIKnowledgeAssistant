using KnowledgeAssistant.Domain.Entities.Emails;
using KnowledgeAssistant.Application.Messaging.Handlers;
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

            await ConfigureQueuesAsync(stoppingToken);

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

        private async Task ConfigureQueuesAsync(CancellationToken cancellationToken)
        {
            await _channel!.QueueDeclareAsync(
                queue: "emails",
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: cancellationToken);

            await _channel.BasicQosAsync(
                prefetchSize: 0,
                prefetchCount: 1,
                global: false,
                cancellationToken: cancellationToken);
        }

        private async Task StartConsumersAsync(CancellationToken cancellationToken)
        {
            await StartConsumerAsync<SendEmailMessage>("emails", cancellationToken);
        }

        private async Task StartConsumerAsync<T>(string queue, CancellationToken cancellationToken)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel!);

            consumer.ReceivedAsync += async (_, eventArgs) =>
            {
                await ProcessMessageAsync<T>(
                    queue,
                    eventArgs,
                    cancellationToken);
            };

            await _channel!.BasicConsumeAsync(
                queue: queue,
                autoAck: false,
                consumer: consumer,
                cancellationToken: cancellationToken);

            _logger.LogInformation($"RabbitMQ consumer started for queue {queue}.", queue);
        }

        private async Task ProcessMessageAsync<T>(string queue, BasicDeliverEventArgs eventArgs, CancellationToken cancellationToken)
        {
            try
            {
                var json = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
                var message = JsonSerializer.Deserialize<T>(json);

                if (message == null)
                {
                    throw new InvalidOperationException($"Could not deserialize RabbitMQ message from queue '{queue}'.");
                }

                using var scope = _scopeFactory.CreateScope();

                var handler = GetHandler<T>(
                    scope.ServiceProvider,
                    queue);

                await handler.HandleAsync(
                    message,
                    cancellationToken);

                await _channel!.BasicAckAsync(
                    deliveryTag: eventArgs.DeliveryTag,
                    multiple: false,
                    cancellationToken: cancellationToken);

                _logger.LogInformation($"RabbitMQ message processed successfully from queue {queue}.", queue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing RabbitMQ message from queue {queue}.", queue);

                await _channel!.BasicNackAsync(
                    deliveryTag: eventArgs.DeliveryTag,
                    multiple: false,
                    requeue: true,
                    cancellationToken: cancellationToken);
            }
        }

        private IMessageHandler<T> GetHandler<T>(IServiceProvider serviceProvider, string queue)
        {
            return queue switch
            {
                "emails" => (IMessageHandler<T>)serviceProvider.GetRequiredService<IMessageHandler<T>>(),
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