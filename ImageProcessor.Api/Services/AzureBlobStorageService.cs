using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
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

        public async Task<Stream> GetImageStreamByName(Guid userId, string imageName)
        {
            var container = _blobClient.GetBlobContainerClient(userId.ToString());
            var exists = await container.ExistsAsync();

            if (!exists)
                throw new FileNotFoundException("User container doesn't exist");

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

        public async Task<string> GetImageUrlByName(Guid userId, string imageName)
        {
            var container = _blobClient.GetBlobContainerClient(userId.ToString());
            var exists = await container.ExistsAsync();

            if(!exists)
                throw new Exception("User container doesn't exist");

            var blob = container.GetBlobClient(imageName);
            exists = await blob.ExistsAsync();

            if (!exists)
                throw new FileNotFoundException("Image not exists");

            var sasBuilder = new BlobSasBuilder()
            {
                BlobContainerName = container.Name,
                ExpiresOn = DateTime.UtcNow.AddMinutes(30),
                Resource = "b"
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            return blob.GenerateSasUri(sasBuilder).ToString();
        }

        public async Task UploadImageOnProcess(Stream image, string imageName)
        {
            var container = _blobClient.GetBlobContainerClient("images_processing");
            await container.CreateIfNotExistsAsync();

            var blob = container.GetBlobClient(imageName);
            await blob.UploadAsync(image, overwrite: true);
        }
    }
}
