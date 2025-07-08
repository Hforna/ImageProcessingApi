namespace ImageProcessor.Api.Services
{
    public interface IStorageService
    {
        public Task UploadImage(Guid userId, string imageName, Stream image);
        public Task<Stream> GetImageStreamByName(Guid userId, string imageName);
        public Task<string> GetImageUrlByName(Guid userId, string imageName);
        public Task<string> GetImageUrlOnProcessByName(string imageName);
        public Task UploadImageOnProcess(Stream image, string imageName);
    }
}
