using ImageProcessor.Api.Enums;

namespace ImageProcessor.Api.RabbitMq.Messages
{
    public class ApplyWatermarkMessage
    {
        public Guid UserId { get; set; }
        public string ImageName { get; set; }
        public string Text { get; set; }
        public float WatermarkSize { get; set; }
        public ImageTypesEnum ImageType { get; set; }
        public bool SaveChanges { get; set; }
        public string CallbackUrl { get; set; }
    }
}
