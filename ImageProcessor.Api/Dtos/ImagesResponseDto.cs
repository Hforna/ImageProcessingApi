namespace ImageProcessor.Api.Dtos
{
    public sealed record ImagesResponseDto
    {
        public bool HasMorePages { get; set; }
        public List<ImageResponseDto> Images { get; set; }
    }
}
