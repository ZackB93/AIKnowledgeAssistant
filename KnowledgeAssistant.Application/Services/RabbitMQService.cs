using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace KnowledgeAssistant.Application.Services
{
    public interface IRabbitMQService
    {
        Task PublishAsync<T>(T message, string queue, CancellationToken cancellationToken = default);
    }

    public class RabbitMQService : IRabbitMQService
    {
        private readonly IConfiguration _configuration;
        private readonly ConnectionFactory _connectionFactory;
        private IConnection? _connection;
        private readonly SemaphoreSlim _connectionLock = new(1, 1);

        public RabbitMQService(IConfiguration configuration)
        {
            _configuration = configuration;

            _connectionFactory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMQ:HostName"]!,
                UserName = _configuration["RabbitMQ:UserName"]!,
                Password = _configuration["RabbitMQ:Password"]!
            };
        }

        public async Task PublishAsync<T>(T message, string queue, CancellationToken cancellationToken = default)
        {
            var connection = await GetConnectionAsync(cancellationToken);

            await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

            await channel.QueueDeclareAsync(
                queue: queue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: cancellationToken);

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: queue,
                body: body,
                cancellationToken: cancellationToken);
        }

        private async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken)
        {
            if (_connection is { IsOpen: true })
            {
                return _connection;
            }           

            await _connectionLock.WaitAsync(cancellationToken);

            try
            {
                if (_connection is { IsOpen: true })
                {
                    return _connection;
                }

                _connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

                return _connection;
            }
            finally
            {
                _connectionLock.Release();
            }
        }
    }
}
