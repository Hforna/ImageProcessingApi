﻿using ImageProcessor.Api.RabbitMq.Messages;
using ImageProcessor.Api.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;

namespace ImageProcessor.Api.RabbitMq.Consumers
{
    public class RotateImageConsumer : BackgroundService
    {
        private IChannel _channel;
        private IConnection _connection;
        private readonly IConfiguration _configuration;
        private readonly ImageService _imageService;
        private readonly ILogger<RotateImageConsumer> _logger;
        private readonly IStorageService _storageService;
        private readonly IHttpClientFactory _httpClient;
        private readonly IServiceProvider _serviceProvider;

        public RotateImageConsumer(IConfiguration configuration,
            ILogger<RotateImageConsumer> logger, IHttpClientFactory httpClient, IServiceProvider serviceProvider)
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

            await _channel.ExchangeDeclareAsync(RabbitmqConnectionConsts.ProcessImageExchange, "direct", true, false);

            await _channel.QueueDeclareAsync("rotate_image", true, true, false);
            await _channel.QueueBindAsync("rotate_image", RabbitmqConnectionConsts.ProcessImageExchange, "rotate.image");

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (ModuleHandle, ea) =>
            {
                var message = Encoding.UTF8.GetString(ea.Body.ToArray());

                try
                {
                    var deserialize = JsonSerializer.Deserialize<RotateImageMessage>(message);

                    await Consume(deserialize);
                }catch(SerializationException ex)
                {
                    _logger.LogError($"Error while trying to deserialize message: {message}");

                    await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
                    throw;
                }catch(FileNotFoundException ex)
                {
                    _logger.LogError($"File wasn't found");
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, true);
                    throw;
                }catch(Exception ex)
                {
                    _logger.LogError(ex, $"An unexpectadly error occured: {ex.Message}");
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, true);
                    throw;
                }
            };

            await _channel.BasicConsumeAsync("rotate_image", false, consumer);
        }

        private async Task Consume(RotateImageMessage message)
        {
            var image = await _storageService.GetImageStreamByName(message.UserIdentifier, message.ImageName);

            var rotateImage = await _imageService.RotateImage(image, message.Degrees, message.ImageType);

            await _storageService.UploadImageOnProcess(rotateImage, message.ImageName);

            if (message.SaveChanges)
                await _storageService.UploadImage(message.UserIdentifier, message.ImageName, rotateImage);

            var imageUrl = await _storageService.GetImageUrlOnProcessByName(message.ImageName);

            using (var httpClient = _httpClient.CreateClient())
            {
                var response = await httpClient.PostAsJsonAsync(message.CallbackUrl, new
                {
                    event_type = "rotate_image_processed",
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
