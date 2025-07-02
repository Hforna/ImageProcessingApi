namespace ImageProcessor.Api.Services
{
    public interface IStorageService
    {
        public Task UploadImage(Guid userId, string imageName, Stream image);
        public Task<Stream> GetImageByName(Guid userId, string imageName);
    }
}
