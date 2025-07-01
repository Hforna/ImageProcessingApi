using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace ImageProcessor.Api.Services
{
    public class AzureBlobStorageService : IStorageService
    {
        private readonly BlobServiceClient _blobClient;

        public AzureBlobStorageService(BlobServiceClient blobClient)
        {
            _blobClient = blobClient;
        }

        public async Task UpdateImage(Guid userId, string imageName, Stream image)
        {
            var container = _blobClient.GetBlobContainerClient(userId.ToString());
            await container.CreateIfNotExistsAsync();

            var blob = container.GetBlobClient(imageName);

            await blob.UploadAsync(image, overwrite: true);
        }
    }
}
