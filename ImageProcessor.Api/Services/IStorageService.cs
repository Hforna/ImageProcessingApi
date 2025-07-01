namespace ImageProcessor.Api.Services
{
    public interface IStorageService
    {
        public Task UpdateImage(Guid userId, string imageName, Stream image);
    }
}
