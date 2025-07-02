using ImageProcessor.Api.Enums;

namespace ImageProcessor.Api.Dtos
{
    public sealed record ImageFormatDto
    {
        public ImageTypesEnum FormatType { get; set; }
    }
}
