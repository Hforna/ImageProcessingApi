using ImageProcessor.Api.Data;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ImageProcessor.Api.Services
{
    public interface ITokenService
    {
        public string GenerateToken(Guid userIdentifier);
    }

    public class TokenService : ITokenService
    {
        private readonly string _signKey;
        private readonly int _expireAt;
        private readonly IUnitOfWork _uow;

        public TokenService(string signKey, int expireAt, IUnitOfWork uow)
        {
            _signKey = signKey;
            _expireAt = expireAt;
            _uow = uow;
        }

        public string GenerateToken(Guid userIdentifier)
        {
            var claims = new List<Claim>() { new Claim(ClaimTypes.Sid, userIdentifier.ToString()) };

            var descriptor = new SecurityTokenDescriptor()
            {
                Subject = new ClaimsIdentity(claims),
                SigningCredentials = new SigningCredentials(GenerateSecurityKey(), SecurityAlgorithms.HmacSha256Signature),
                Expires = DateTime.UtcNow.AddMinutes(_expireAt)
            };

            var handler = new JwtSecurityTokenHandler();

            var create = handler.CreateToken(descriptor);

            return handler.WriteToken(create);
        }

        SymmetricSecurityKey GenerateSecurityKey()
        {
            return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_signKey));
        }
    }
}
