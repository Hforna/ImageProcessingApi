using ImageProcessor.Api.Data;
using ImageProcessor.Api.Services;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.IdentityModel.Tokens;

namespace ImageProcessor.Api.Filters
{
    public class AuthenticatedUser : IAsyncAuthorizationFilter
    {
        private readonly ITokenService _tokenService;
        private readonly IUnitOfWork _uow;

        public AuthenticatedUser(ITokenService tokenService, IUnitOfWork uow)
        {
            _tokenService = tokenService;
            _uow = uow;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var token = _tokenService.GetRequestToken();

            if (string.IsNullOrEmpty(token))
                throw new Exception("User must be authenticated for access this endpoint");

            try
            {
                var validate = _tokenService.ValidateToken(token);

                var user = await _uow.UserRepository.UserByIdentifier(validate, true);

                if (user is null)
                    throw new BadHttpRequestException("Invalid request token");
            }catch(SecurityTokenExpiredException ex)
            {
                throw new UnauthorizedAccessException(ex.Message);
            }catch(Exception ex)
            {
                throw new UnauthorizedAccessException("Couldn't get info about the request user");
            }
        }
    }
}
