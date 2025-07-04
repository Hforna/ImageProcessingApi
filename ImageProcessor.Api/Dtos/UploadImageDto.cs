namespace ImageProcessor.Api.Dtos
{
    public sealed record UploadImageDto
    {
        public string? ImageName { get; set; }
        public IFormFile File { get; set; }
    }
}
