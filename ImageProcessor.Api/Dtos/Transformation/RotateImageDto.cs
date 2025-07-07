namespace ImageProcessor.Api.Dtos.Transformation
{
    public sealed record RotateImageDto
    {
        public bool SaveChanges { get; set; }
        public float Degrees { get; set; }
    }
}
