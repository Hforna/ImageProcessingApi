using AutoMapper;
using ImageProcessor.Api.Data;
using ImageProcessor.Api.Dtos;
using ImageProcessor.Api.Model;
using ImageProcessor.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ImageProcessor.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;
        private readonly IUnitOfWork _uow;
        private readonly ITokenService _tokenService;
        private readonly IMapper _mapper;
        private readonly IPasswordEncrypt _passwordEncrypt;

        public UserController(ILogger<UserController> logger, IUnitOfWork uow, 
            ITokenService tokenService, IMapper mapper, IPasswordEncrypt passwordEncrypt)
        {
            _logger = logger;
            _uow = uow;
            _tokenService = tokenService;
            _mapper = mapper;
            _passwordEncrypt = passwordEncrypt;
        }

        [HttpPost]
        public async Task<IActionResult> SignUp([FromBody]SignUpDto request)
        {
            var userEmailExists = await _uow.UserRepository.UserByEmail(request.Email);

            if (userEmailExists is not null)
                return BadRequest("User with this e-mail already exists");

            if (request.Password.Length < 8)
            {
                _logger.LogError($"Password {request.Password} invalid when validated length");
                return BadRequest("Password length must be greather or equal than 8");
            }

            var user = _mapper.Map<User>(request);
            user.PasswordHash = _passwordEncrypt.EncryptPassword(request.Password);

            await _uow.UserRepository.AddAsync(user);
            await _uow.Commit();

            _logger.LogInformation($"User {user.UserName} was created with id {user.Id}");

            var response = _mapper.Map<UserDto>(user);

            return Created(string.Empty, response);
        }

        [HttpPost("sign-in")]
        public async Task<IActionResult> SignIn([FromBody]SignInDto request)
        {
            var user = await _uow.UserRepository.UserByEmail(request.Email);

            if (user is null)
                return NotFound("User with this e-mail not exists");

            var isValidPassword = _passwordEncrypt.VerifyPassword(request.Password, user.PasswordHash);

            if (!isValidPassword)
                return BadRequest("Invalid password");

            var token = _tokenService.GenerateToken(user.UserIdentifier);
            user.RefreshToken = _tokenService.GenerateRefreshToken();
            user.RefreshExpiresAt = _tokenService.GetExpirationTime();

            _uow.UserRepository.Update(user);
            await _uow.Commit();

            var response = new SignInResponseDto()
            {
                AccessToken = token,
                RefreshToken = user.RefreshToken,
                ExpiresAt = user.RefreshExpiresAt
            };

            return Ok(response);
        }
    }
}
