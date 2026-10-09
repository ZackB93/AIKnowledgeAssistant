using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace KnowledgeAssistant.Application.Services.Infrastructure
{
    public interface IRabbitMQService
    {
        Task PublishAsync<T>(T message, string queue, CancellationToken ct = default);
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

        public async Task PublishAsync<T>(T message, string queue, CancellationToken ct = default)
        {
            var connection = await GetConnectionAsync(ct);

            await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);

            await channel.QueueDeclareAsync(
                queue: queue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: ct);

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: queue,
                body: body,
                cancellationToken: ct);
        }

        private async Task<IConnection> GetConnectionAsync(CancellationToken ct)
        {
            if (_connection is { IsOpen: true })
            {
                return _connection;
            }           

            await _connectionLock.WaitAsync(ct);

            try
            {
                if (_connection is { IsOpen: true })
                {
                    return _connection;
                }

                _connection = await _connectionFactory.CreateConnectionAsync(ct);

                return _connection;
            }
            finally
            {
                _connectionLock.Release();
            }
        }
    }
}
