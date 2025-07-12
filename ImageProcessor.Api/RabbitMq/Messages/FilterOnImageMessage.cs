using ImageProcessor.Api.Enums;

namespace ImageProcessor.Api.RabbitMq.Messages
{
    public class FilterOnImageMessage
    {
        public Guid UserIdentifier { get; set; }
        public string ImageName { get; set; }
        public bool SaveChanges { get; set; }
        public string FilterName { get; set; }
        public ImageTypesEnum ImageType { get; set; }
        public string CallbackUrl { get; set; }
    }
}
