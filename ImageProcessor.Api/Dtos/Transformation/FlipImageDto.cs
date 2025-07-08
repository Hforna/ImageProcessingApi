using ImageProcessor.Api.Enums;

namespace ImageProcessor.Api.Dtos.Transformation
{
    public sealed record FlipImageDto
    {
        public FlipImageEnum FlipType { get; set; }
    }
}
