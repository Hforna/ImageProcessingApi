using Microsoft.Identity.Client;

namespace ImageProcessor.Api.Exceptions
{
    public class FileNotFoundOnStorageException : SystemException
    {
        public FileNotFoundOnStorageException(string message) : base(message) { }
    }

    public class UploadImageException : SystemException
    {
        public UploadImageException(string message) : base(message) { }
    }
}
