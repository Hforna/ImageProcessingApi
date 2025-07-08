
using ImageProcessor.Api.RabbitMq.Messages;
using ImageProcessor.Api.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;

namespace ImageProcessor.Api.RabbitMq.Consumers
{
    public class CropImageConsumer : BackgroundService //IAsyncDisposable
    {
        private readonly IConfiguration _configuration;
        private IConnection _connection;
        private IChannel _channel;
        private readonly IStorageService _storageService;
        private readonly ImageService _imageService;
        private readonly IHttpClientFactory _httpClient;
        private readonly ILogger<CropImageConsumer> _logger;
        private readonly IServiceProvider _serviceProvider;

        public CropImageConsumer(IConfiguration configuration,
           ILogger<CropImageConsumer> logger, IHttpClientFactory httpClient, IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _httpClient = httpClient;
            _logger = logger;

            var scope = _serviceProvider.CreateScope();
            _imageService = scope.ServiceProvider.GetRequiredService<ImageService>();
            _storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();
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

            await _channel.ExchangeDeclareAsync("image_process_exchange", "direct");
            await _channel.QueueDeclareAsync("crop_image", durable: true, true, false);
            await _channel.QueueBindAsync("crop_image", "image_process_exchange", "crop.image");

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (ModuleHandle, ea) =>
            {
                var body = ea.Body;
                var decode = Encoding.UTF8.GetString(body.ToArray());

                _logger.LogInformation($"Message on crop image consumer recived: {decode}");

                try
                {
                    var deserialize = JsonSerializer.Deserialize<CropImageMessage>(decode);

                    await Consume(deserialize!);
                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
                }
                catch (SerializationException ex)
                {
                    _logger.LogError($"Error occured while trying deserialize message: {ex.Message}, message recived: {decode}");

                    await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
                }
                catch (FileNotFoundException ex)
                {
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"An error occured while trying to consume crop image message: {ex.Message}");
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, true);
                }
            };

            await _channel.BasicConsumeAsync("crop_image", false, consumer);
        }

        private async Task Consume(CropImageMessage message)
        {
            using var image = await _storageService.GetImageStreamByName(message.UserIdentifier, message.ImageName);

            using var crop = await _imageService.CropImage(image, message.Width, message.Height, message.ImageType);

            await _storageService.UploadImageOnProcess(crop, message.ImageName);

            var newImage = await _storageService.GetImageUrlOnProcessByName(message.ImageName);

            using var client = _httpClient.CreateClient();

            var response = client.PostAsJsonAsync(message.CallbackUrl, new
            {
                event_type = "crop_image_processed",
                image_url = newImage,
                processed = true,
                expires_at = DateTime.UtcNow.AddMinutes(30)
            });

            if (message.SaveImage)
                await _storageService.UploadImage(message.UserIdentifier, message.ImageName, crop);
        }


        //public async ValueTask DisposeAsync()
        //{
        //    await _channel.DisposeAsync();
        //    await _channel.CloseAsync();
        //
        //    await _connection.DisposeAsync();
        //    await _connection.CloseAsync();
        //}
    }
}
