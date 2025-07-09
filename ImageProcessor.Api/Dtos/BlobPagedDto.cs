namespace ImageProcessor.Api.Dtos
{
    public sealed record BlobPagedDto
    {
        public Dictionary<string, string> imageInfos { get; set; } = new Dictionary<string, string>();
        public bool HasMorePages { get; set; }
    }
}
