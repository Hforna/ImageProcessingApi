namespace ImageProcessor.Api.Dtos.Transformation
{
    public sealed record FilterOnImageDto
    {
        public string FilterName { get; set; }
        public bool SaveChanges { get; set; }
        public IFormFile file { get; set; }
    }
}
