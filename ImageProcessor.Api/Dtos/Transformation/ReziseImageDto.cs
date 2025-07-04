namespace ImageProcessor.Api.Dtos.Transformation
{
    public sealed record ReziseImageDto
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public bool SaveChanges { get; set; }
    }
}
