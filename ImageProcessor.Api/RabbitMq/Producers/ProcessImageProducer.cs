using ImageProcessor.Api.RabbitMq.Messages;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace ImageProcessor.Api.RabbitMq.Producers
{
    public interface IProcessImageProducer
    {
        public Task SendImageForResize(ResizeImageMessage message);
    }

    public class ProcessImageProducer : IProcessImageProducer
    {
        private IChannel _channel;
        private IConfiguration _configuration;
        private IConnection _connection;

        public ProcessImageProducer(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendImageForResize(ResizeImageMessage message)
        {
            _connection = await new ConnectionFactory()
            {
                Port = _configuration.GetValue<int>("services:rabbitMq:port"),
                HostName = _configuration.GetValue<string>("services:rabbitMq:hostName")!,
                UserName = _configuration.GetValue<string>("services:rabbitMq:username")!,
                Password = _configuration.GetValue<string>("services:rabbitMq:password")!,
            }.CreateConnectionAsync();

            _channel = await _connection.CreateChannelAsync();

            await _channel.ExchangeDeclareAsync("image_process_exchange", "direct", durable: true);

            var serialize = JsonSerializer.Serialize(message);
            var encode = Encoding.UTF8.GetBytes(serialize);
            await _channel.BasicPublishAsync("image_process_exchange", "resize.image", encode);
        }
    }
}