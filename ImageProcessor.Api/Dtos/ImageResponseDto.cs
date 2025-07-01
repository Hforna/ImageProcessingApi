namespace ImageProcessor.Api.Dtos
{
    public sealed record ImageResponseDto
    {
        public string ImageName { get; set; }
        public string ExtensionType { get; set; }
    }
}
