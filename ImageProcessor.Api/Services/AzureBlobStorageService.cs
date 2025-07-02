using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.IO;

namespace ImageProcessor.Api.Services
{
    public class AzureBlobStorageService : IStorageService
    {
        private readonly BlobServiceClient _blobClient;

        public AzureBlobStorageService(BlobServiceClient blobClient)
        {
            _blobClient = blobClient;
        }

        public async Task UploadImage(Guid userId, string imageName, Stream image)
        {
            var container = _blobClient.GetBlobContainerClient(userId.ToString());
            await container.CreateIfNotExistsAsync();

            var blob = container.GetBlobClient(imageName);

            await blob.UploadAsync(image, overwrite: true);
        }

        public async Task<Stream> GetImageByName(Guid userId, string imageName)
        {
            var container = _blobClient.GetBlobContainerClient(userId.ToString());
            var exists = await container.ExistsAsync();

            if (!exists)
                throw new FileNotFoundException("Image not exists");

            var blob = container.GetBlobClient(imageName);
            exists = await blob.ExistsAsync();

            if(!exists)
                throw new FileNotFoundException("Image not exists");

            var memoryStream = new MemoryStream();

            var image = await blob.DownloadStreamingAsync();
            image.Value.Content.CopyTo(memoryStream);

            memoryStream.Position = 0;

            return memoryStream;
        }
    }
}
