
using ImageProcessor.Api.RabbitMq.Messages;
using ImageProcessor.Api.Services;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.IdentityModel.Tokens;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;

namespace ImageProcessor.Api.RabbitMq.Consumers
{
    public class ResizeImageConsumer : BackgroundService
    {
        private IChannel _channel;
        private IConnection _connection;
        private readonly IConfiguration _configuration;
        private readonly ImageService _imageService;
        private readonly ILogger<ResizeImageConsumer> _logger;
        private readonly IStorageService _storageService;
        private readonly IHttpClientFactory _httpClient;
        private readonly IServiceProvider _serviceProvider;

        public ResizeImageConsumer(IConfiguration configuration, 
            ILogger<ResizeImageConsumer> logger, IHttpClientFactory httpClient, IServiceProvider serviceProvider)
        {
            _configuration = configuration;
            _serviceProvider = serviceProvider;
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

            await _channel.ExchangeDeclareAsync("image_process_exchange", "direct", true);

            await _channel.QueueDeclareAsync("resize_image", durable:true, true, false);
            await _channel.QueueBindAsync("resize_image", "image_process_exchange", "resize.image");

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (ModuleHandler, ea) =>
            {
                var body = ea.Body;
                var message = Encoding.UTF8.GetString(body.ToArray());

                _logger.LogInformation($"Message recived by consumer: {ea.RoutingKey} on exchange: {ea.Exchange}, message: {message}");

                try
                {
                    var deserialize = JsonSerializer.Deserialize<ResizeImageMessage>(message);

                    await ConsumeMessage(deserialize!);
                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
                }catch(SerializationException ex)
                {
                    _logger.LogError($"Error occured while trying deserialize message: {ex.Message}, message recived: {message}");

                    await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
                    throw;
                }catch(FileNotFoundException ex)
                {
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, true);
                    throw;
                }catch(Exception ex)
                {
                    _logger.LogError(ex, $"An unexpectedly error occured: {ex.Message}");

                    await _channel.BasicNackAsync(ea.DeliveryTag, false, true);
                    throw;
                }
            };

            await _channel.BasicConsumeAsync("resize_image", false, consumer);
        }

        private async Task ConsumeMessage(ResizeImageMessage message)
        {
            using var image = await _storageService.GetImageStreamByName(message.UserIdentifier, message.ImageName);

            using var resizeImage = await _imageService.ResizeImage(image, message.Width, message.Height, message.ImageType);

            await _storageService.UploadImageOnProcess(resizeImage, message.ImageName);

            var newImage = await _storageService.GetImageUrlOnProcessByName(message.ImageName);

            using var client = _httpClient.CreateClient();

            var response = await client.PostAsJsonAsync(message.CallbackUrl, new 
            {
                event_type = "resize_image_processed", 
                image_url = newImage,
                processed = true,
                processed_at = DateTime.UtcNow,
                expires_at = DateTime.UtcNow.AddMinutes(30)
            });

            if (message.SaveImage)
                await _storageService.UploadImage(message.UserIdentifier, message.ImageName, resizeImage);

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Error while trying to make request to {message.CallbackUrl}");
        }

        public override void Dispose()
        {
            //_channel.CloseAsync();
            GC.SuppressFinalize(this);
        }
    }
}
