using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using ImageProcessor.Api.Dtos;
using ImageProcessor.Api.Exceptions;
using System.IO;

namespace ImageProcessor.Api.Services
{
    public class AzureBlobStorageService : IStorageService
    {
        private readonly BlobServiceClient _blobClient;
        private const string ProcessContainer = "images_processing";

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
                throw new FileNotFoundOnStorageException("User container doesn't exist");

            var blob = container.GetBlobClient(imageName);
            exists = await blob.ExistsAsync();

            if(!exists)
                throw new FileNotFoundOnStorageException("Image not exists");

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
                throw new FileNotFoundOnStorageException("Image not exists");

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
            var container = _blobClient.GetBlobContainerClient(ProcessContainer);
            await container.CreateIfNotExistsAsync();

            var blob = container.GetBlobClient(imageName);
            await blob.UploadAsync(image, overwrite: true);
        }

        public async Task<string> GetImageUrlOnProcessByName(string imageName)
        {
            var container = _blobClient.GetBlobContainerClient(ProcessContainer);
            var exists = await container.ExistsAsync();

            if (!exists)
                throw new FileNotFoundOnStorageException("Process container doesn't exist");

            var blob = container.GetBlobClient(imageName);
            exists = await blob.ExistsAsync();

            if (!exists)
                throw new FileNotFoundOnStorageException("Image not exists");

            var sasBuilder = new BlobSasBuilder()
            {
                BlobContainerName = container.Name,
                ExpiresOn = DateTime.UtcNow.AddMinutes(30),
                Resource = "b"
            };
            sasBuilder.SetPermissions(BlobAccountSasPermissions.Read);

            return blob.GenerateSasUri(sasBuilder).ToString();
        }

        public async Task<BlobPagedDto> GetAllImagesFromUserContainerPaginated(int page, int quantity, Guid userId)
        {
            var container = _blobClient.GetBlobContainerClient(userId.ToString());
            var exists = await container.ExistsAsync();

            if (!exists)
                throw new FileNotFoundOnStorageException("User container doesn't exist");

            var currentPage = 1;
            string? continuationToken = null;

            var response = new BlobPagedDto();

            await foreach(Page<BlobItem> blobPage in container.GetBlobsAsync().AsPages(continuationToken, quantity))
            {
                continuationToken = blobPage.ContinuationToken;

                if (page == currentPage)
                {
                    var sasBuilder = new BlobSasBuilder()
                    {
                        BlobContainerName = userId.ToString(),
                        ExpiresOn = DateTime.UtcNow.AddMinutes(30),
                        Resource = "b"
                    };
                    sasBuilder.SetPermissions(BlobAccountSasPermissions.Read);

                    response.imageInfos = blobPage
                                .Values
                                .Select(d => d.Name)
                                .ToDictionary(k => k, val => container.GetBlobClient(val).GenerateSasUri(sasBuilder).ToString());

                    response.HasMorePages = continuationToken is null == false;
                    
                    return response;
                }

                currentPage++;
            }

            return response;
        }
    }
}
