using ImageProcessor.Api.Enums;

namespace ImageProcessor.Api.RabbitMq.Messages
{
    public class RotateImageMessage
    {
        public Guid UserIdentifier { get; set; }
        public bool SaveChanges { get; set; }
        public string ImageName { get; set; }
        public float Degrees { get; set; }
        public string CallbackUrl { get; set; }
        public ImageTypesEnum ImageType { get; set; }
    }
}
