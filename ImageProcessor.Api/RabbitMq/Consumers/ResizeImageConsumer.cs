
using ImageProcessor.Api.RabbitMq.Messages;
using ImageProcessor.Api.Services;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;

namespace ImageProcessor.Api.RabbitMq.Consumers
{
    public class ResizeImageConsumer : BackgroundService, IDisposable
    {
        private IChannel _channel;
        private IConnection _connection;
        private readonly IConfiguration _configuration;
        private readonly ImageService _imageService;
        private readonly ILogger<ResizeImageConsumer> _logger;
        private readonly ImageService imageService;
        private readonly IStorageService _storageService;

        public ResizeImageConsumer(IConfiguration configuration, ImageService imageService, 
            IStorageService storageService, ILogger<ResizeImageConsumer> logger)
        {
            _configuration = configuration;
            _imageService = imageService;
            _logger = logger;
            _storageService = storageService;
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

                    using var image = await _storageService.GetImageStreamByName(deserialize.UserIdentifier, deserialize.ImageName);

                    using var resizeImage = await _imageService.ResizeImage(image, deserialize.Width, deserialize.Height, deserialize.ImageType);

                    await _storageService.UploadImageOnProcess(resizeImage, deserialize.ImageName);

                    if (deserialize.SaveImage)
                        await _storageService.UploadImage(deserialize.UserIdentifier, deserialize.ImageName, resizeImage);
                }catch(SerializationException ex)
                {
                    _logger.LogError($"Error occured while trying deserialize message: {ex.Message}, message recived: {message}");

                    await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
                }catch(FileNotFoundException ex)
                {
                    await _channel.BasicNackAsync(ea.DeliveryTag, true, false);
                }catch(Exception ex)
                {

                }
            };
        }
    }
}
