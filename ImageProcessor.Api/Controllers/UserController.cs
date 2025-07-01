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
            if (request.Password.Length < 8)
                return BadRequest("Password length must be greather or equal than 8");

            var user = _mapper.Map<User>(request);
            user.PasswordHash = _passwordEncrypt.EncryptPassword(request.Password);

            await _uow.UserRepository.AddAsync(user);
            await _uow.Commit();

            var response = _mapper.Map<UserDto>(user);

            return Created(string.Empty, response);
        }

    }
}
