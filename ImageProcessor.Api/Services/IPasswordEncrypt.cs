using BCrypt.Net;

namespace ImageProcessor.Api.Services
{
    public interface IPasswordEncrypt
    {
        public string EncryptPassword(string password);

        public bool VerifyPassword(string password, string hash);
    }
}
