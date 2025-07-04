using ImageProcessor.Api.Enums;

namespace ImageProcessor.Api.Dtos.Transformation
{
    public sealed record ImageFormatDto
    {
        public ImageTypesEnum FormatType { get; set; }
        public bool SaveChanges { get; set; }
    }
}
