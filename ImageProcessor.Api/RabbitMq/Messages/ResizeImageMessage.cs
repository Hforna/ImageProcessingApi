using ImageProcessor.Api.Enums;

namespace ImageProcessor.Api.RabbitMq.Messages
{
    public class ResizeImageMessage
    {
        public Guid UserIdentifier { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string ImageName { get; set; }
        public bool SaveImage { get; set; }
        public ImageTypesEnum ImageType { get; set; }
    }
}
