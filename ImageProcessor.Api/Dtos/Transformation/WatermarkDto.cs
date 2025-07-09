namespace ImageProcessor.Api.Dtos.Transformation
{
    public sealed record WatermarkDto
    {
        public bool SaveChanges { get; set; }
        public float WatermarkSize { get; set; }
        public string Text { get; set; }
    }
}
