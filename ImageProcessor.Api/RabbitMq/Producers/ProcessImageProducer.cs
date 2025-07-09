using ImageProcessor.Api.RabbitMq.Messages;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace ImageProcessor.Api.RabbitMq.Producers
{
    public interface IProcessImageProducer
    {
        public Task SendImageForResize(ResizeImageMessage message);
        public Task SendImageForCrop(CropImageMessage message);
        public Task SendImageForRotate(RotateImageMessage message);
        public Task SendImageForApplyWatermark(ApplyWatermarkMessage message);
    }

    public class ProcessImageProducer : IProcessImageProducer, IDisposable
    {
        private IChannel _channel;
        private IConfiguration _configuration;
        private IConnection _connection;
        private const string ExchangeName = "image_process_exchange";

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

            await _channel.ExchangeDeclareAsync(ExchangeName, "direct", durable: true);

            var serialize = JsonSerializer.Serialize(message);
            var messageBytes = Encoding.UTF8.GetBytes(serialize);
            await _channel.BasicPublishAsync(ExchangeName, "resize.image", messageBytes);
        }

        public async Task SendImageForCrop(CropImageMessage message)
        {
            _connection = await new ConnectionFactory()
            {
                Port = _configuration.GetValue<int>("services:rabbitMq:port"),
                HostName = _configuration.GetValue<string>("services:rabbitMq:hostName")!,
                UserName = _configuration.GetValue<string>("services:rabbitMq:username")!,
                Password = _configuration.GetValue<string>("services:rabbitMq:password")!,
            }.CreateConnectionAsync();

            _channel = await _connection.CreateChannelAsync();

            await _channel.ExchangeDeclareAsync(ExchangeName, "direct", true);

            var serialize = JsonSerializer.Serialize(message);
            var messageBytes = Encoding.UTF8.GetBytes(serialize);
            await _channel.BasicPublishAsync(ExchangeName, "crop.image", messageBytes);
        }

        public async Task SendImageForRotate(RotateImageMessage message)
        {
            _connection = await new ConnectionFactory()
            {
                Port = _configuration.GetValue<int>("services:rabbitMq:port"),
                HostName = _configuration.GetValue<string>("services:rabbitMq:hostName")!,
                UserName = _configuration.GetValue<string>("services:rabbitMq:username")!,
                Password = _configuration.GetValue<string>("services:rabbitMq:password")!,
            }.CreateConnectionAsync();

            _channel = await _connection.CreateChannelAsync();

            await _channel.ExchangeDeclareAsync(ExchangeName, "direct", true);

            var serialize = JsonSerializer.Serialize(message);
            var messageBytes = Encoding.UTF8.GetBytes(serialize);
            await _channel.BasicPublishAsync(ExchangeName, "rotate.image", messageBytes);
        }

        public async Task SendImageForApplyWatermark(ApplyWatermarkMessage message)
        {
            _connection = await new ConnectionFactory()
            {
                Port = _configuration.GetValue<int>("services:rabbitMq:port"),
                HostName = _configuration.GetValue<string>("services:rabbitMq:hostName")!,
                UserName = _configuration.GetValue<string>("services:rabbitMq:username")!,
                Password = _configuration.GetValue<string>("services:rabbitMq:password")!,
            }.CreateConnectionAsync();

            _channel = await _connection.CreateChannelAsync();

            await _channel.ExchangeDeclareAsync(ExchangeName, "direct", true);

            var serialize = JsonSerializer.Serialize(message);
            var encodeMessage = Encoding.UTF8.GetBytes(serialize);
            await _channel.BasicPublishAsync(ExchangeName, "watermark.image", encodeMessage);
        }

        public void Dispose()
        {
            _channel.CloseAsync();
            GC.SuppressFinalize(this);
        }
    }
}