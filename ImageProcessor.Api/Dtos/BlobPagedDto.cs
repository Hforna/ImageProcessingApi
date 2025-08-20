namespace ImageProcessor.Api.Dtos
{
    /// <summary>
    /// imageInfos return a dictionary containing a key, 
    /// being the image name and a value being the image uri
    /// </summary>
    public sealed record BlobPagedDto
    {
        public Dictionary<string, string> imageInfos { get; set; } = new Dictionary<string, string>();
        public bool HasMorePages { get; set; }
    }
}
