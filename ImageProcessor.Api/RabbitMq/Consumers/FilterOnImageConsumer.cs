
using ImageProcessor.Api.RabbitMq.Messages;
using ImageProcessor.Api.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;

namespace ImageProcessor.Api.RabbitMq.Consumers
{
    public class FilterOnImageConsumer : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IHttpClientFactory _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FilterOnImageConsumer> _logger;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var connection = await new ConnectionFactory()
            {
                Port = _configuration.GetValue<int>("services:rabbitMq:port"),
                HostName = _configuration.GetValue<string>("services:rabbitMq:hostName")!,
                UserName = _configuration.GetValue<string>("services:rabbitMq:username")!,
                Password = _configuration.GetValue<string>("services:rabbitMq:password")!,
            }.CreateConnectionAsync();

            var channel = await connection.CreateChannelAsync();

            await channel.ExchangeDeclareAsync("image_process_exchange", "direct", true, false);

            await channel.QueueDeclareAsync("filter_on_image", true);
            await channel.QueueBindAsync("filter_on_image", "image_process_exchange", "filter.image");

            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.ReceivedAsync += async (ModuleHandle, ea) =>
            {
                var body = ea.Body;
                var message = Encoding.UTF8.GetString(body.ToArray());

                try
                {
                    var deserialize = JsonSerializer.Deserialize<FilterOnImageMessage>(message);

                    await Consume(deserialize!);
                }catch(SerializationException ex)
                {
                    _logger.LogError(ex, $"Error while trying to deserialize message: {message}");

                    await channel.BasicNackAsync(ea.DeliveryTag, false, false);

                    throw;
                }catch(FileNotFoundException ex)
                {
                    _logger.LogError(ex, "Couldn't find image on storage");

                    await channel.BasicNackAsync(ea.DeliveryTag, false, true);

                    throw;
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex, "Error while consume message");

                    await channel.BasicNackAsync(ea.DeliveryTag, false, true);

                    throw;
                }
            };

            await channel.BasicConsumeAsync("filter_on_image", false, consumer);
        }

        private async Task Consume(FilterOnImageMessage message)
        {
            using(var scope = _serviceProvider.CreateScope())
            {
                var imageService = scope.ServiceProvider.GetRequiredService<ImageService>();
                var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();

                var image = await storageService.GetImageStreamByName(message.UserIdentifier, message.ImageName);

                var applyFilter = imageService.ApplyImageFilter(image, message.ImageType, message.FilterName);

                if (message.SaveChanges)
                    await storageService.UploadImage(message.UserIdentifier, message.ImageName, applyFilter);

                await storageService.UploadImageOnProcess(applyFilter, message.ImageName);

                var getProcessUrl = await storageService.GetImageUrlOnProcessByName(message.ImageName);

                using(var client = _httpClient.CreateClient())
                {
                    var response = await client.PostAsJsonAsync(message.CallbackUrl, new
                    {
                        event_type = "filter_on_image_processed",
                        image_url = getProcessUrl,
                        processed = true,
                        processed_at = DateTime.UtcNow,
                        expires_at = DateTime.UtcNow.AddMinutes(30)
                    });

                    if (!response.IsSuccessStatusCode)
                        throw new Exception($"Error while trying to make request to {message.CallbackUrl}");
                }
            }
        }

        public override void Dispose()
        {
            base.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
