using ImageProcessor.Api.Data;
using ImageProcessor.Api.Model;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ImageProcessor.Api.Services
{
    public interface ITokenService
    {
        public string GenerateToken(Guid userIdentifier);
        public DateTime GetExpirationTime();
        public string GenerateRefreshToken();
        public Guid ValidateToken(string token);
        public Task<User?> GetUserByToken(string token);
        public string? GetRequestToken();
    }

    public class TokenService : ITokenService
    {
        private readonly string _signKey;
        private readonly int _expireAt;
        private readonly IUnitOfWork _uow;
        private readonly IHttpContextAccessor _httpContext;

        public TokenService(string signKey, int expireAt, 
            IUnitOfWork uow, IHttpContextAccessor httpContext)
        {
            _signKey = signKey;
            _expireAt = expireAt;
            _uow = uow;
            _httpContext = httpContext;
        }

        public string GenerateRefreshToken()
        {
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray());
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

        public DateTime GetExpirationTime()
        {
            return DateTime.UtcNow.AddMinutes(_expireAt);
        }

        public string? GetRequestToken()
        {
            var token = _httpContext.HttpContext.Request.Headers.Authorization.ToString();

            if (string.IsNullOrEmpty(token))
                return null;

            return token["Bearer ".Length..].Trim();
        }

        public async Task<User?> GetUserByToken(string token)
        {
            var handler = new JwtSecurityTokenHandler();

            var read = handler.ReadJwtToken(token);

            var uid = Guid.Parse(read.Claims.FirstOrDefault(d => d.Type == ClaimTypes.Sid)!.Value);

            return await _uow.UserRepository.UserByIdentifier(uid);
        }

        public Guid ValidateToken(string token)
        {
            var validatorParameters = new TokenValidationParameters()
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = GenerateSecurityKey(),
                ValidateIssuer = false,
                ValidateAudience = false
            };

            var handler = new JwtSecurityTokenHandler();

            var validate = handler.ValidateToken(token, validatorParameters, out var validatedToken);

            var uid = validate.Claims.FirstOrDefault(d => d.Type == ClaimTypes.Sid)!.Value;

            return Guid.Parse(uid);
        }

        SymmetricSecurityKey GenerateSecurityKey()
        {
            return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_signKey));
        }
    }
}
