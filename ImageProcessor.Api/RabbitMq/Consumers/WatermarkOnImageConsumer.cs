
using ImageProcessor.Api.RabbitMq.Messages;
using ImageProcessor.Api.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;

namespace ImageProcessor.Api.RabbitMq.Consumers
{
    public class WatermarkOnImageConsumer : BackgroundService
    {
        private IConnection _connection;
        private IChannel _channel;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<WatermarkOnImageConsumer> _logger;
        private readonly IHttpClientFactory _httpClient;
        private readonly IConfiguration _configuration;

        public WatermarkOnImageConsumer(IServiceProvider serviceProvider, 
            ILogger<WatermarkOnImageConsumer> logger, IHttpClientFactory httpClient, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _httpClient = httpClient;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _connection = await new ConnectionFactory()
            {
                Port = _configuration.GetValue<int>("services:rabbitMq:port"),
                HostName = _configuration.GetValue<string>("services:rabbitMq:hostName")!,
                UserName = _configuration.GetValue<string>("services:rabbitMq:username")!,
                Password = _configuration.GetValue<string>("services:rabbitMq:password")!,
            }.CreateConnectionAsync();

            _channel = await _connection.CreateChannelAsync();

            await _channel.ExchangeDeclareAsync("image_process_exchange", "direct", true);

            await _channel.QueueDeclareAsync("watermark_image", true, true, false);
            await _channel.QueueBindAsync("watermark_image", "image_process_exchange", "watermark.image");

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (ModuleHandle, ea) =>
            {
                var body = ea.Body;
                var decode = Encoding.UTF8.GetString(body.ToArray());

                try
                {
                    var deserialize = JsonSerializer.Deserialize<ApplyWatermarkMessage>(decode);

                    await Consume(deserialize);
                }catch(SerializationException ex)
                {
                    _logger.LogError(ex, $"An error occured while trying to deserialize message: {decode}");

                    await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
                    throw;
                }catch(FileNotFoundException ex)
                {
                    _logger.LogError($"File wasn't found in storage");
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, true);
                    throw;
                }catch(Exception ex)
                {
                    _logger.LogError(ex, $"An unexpectadly error occured: {ex.Message}");

                    await _channel.BasicNackAsync(ea.DeliveryTag, false, true);
                    throw;
                }
            };

            await _channel.BasicConsumeAsync("watermark_image", false, consumer);
        }

        private async Task Consume(ApplyWatermarkMessage message)
        {
            using var scope = _serviceProvider.CreateScope();

            var imageService = scope.ServiceProvider.GetRequiredService<ImageService>();
            var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();

            using var image = await storageService.GetImageStreamByName(message.UserId, message.ImageName);

            using var applyOnImage = await imageService.ApplyWatermarkOnImage(image, message.ImageType, message.Text, message.WatermarkSize);

            if (message.SaveChanges)
                await storageService.UploadImage(message.UserId, message.ImageName, applyOnImage);

            await storageService.UploadImageOnProcess(applyOnImage, message.ImageName);

            var imageUrl = storageService.GetImageUrlOnProcessByName(message.ImageName);

            using (var httpClient = _httpClient.CreateClient())
            {
                var response = await httpClient.PostAsJsonAsync(message.CallbackUrl, new
                {
                    event_type = "watermark_image_processed",
                    image_url = imageUrl,
                    processed = true,
                    processed_at = DateTime.UtcNow,
                    expires_at = DateTime.UtcNow.AddMinutes(30)
                });

                if (!response.IsSuccessStatusCode)
                    throw new Exception($"Error while trying to make request to {message.CallbackUrl}");
            }
        }

        public override void Dispose()
        {
            //_channel.CloseAsync();
            GC.SuppressFinalize(this);
        }
    }
}
