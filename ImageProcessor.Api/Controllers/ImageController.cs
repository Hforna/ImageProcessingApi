using ImageProcessor.Api.Attributes;
using ImageProcessor.Api.Data;
using ImageProcessor.Api.Dtos;
using ImageProcessor.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ImageProcessor.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AuthenticatedUser]
    public class ImageController : ControllerBase
    {
        private readonly ILogger<ImageController> _logger;
        private readonly IUnitOfWork _uow;
        private readonly IStorageService _storageService;
        private readonly ImageService _imageService;
        private readonly ITokenService _tokenService;

        public ImageController(ILogger<ImageController> logger, IUnitOfWork uow, 
            IStorageService storageService, ImageService imageService, ITokenService tokenService)
        {
            _logger = logger;
            _uow = uow;
            _storageService = storageService;
            _imageService = imageService;
            _tokenService = tokenService;
        }

        [HttpPost]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            var user = await _tokenService.GetUserByToken(_tokenService.GetRequestToken()!);

            using var stream = file.OpenReadStream();

            var validateFile = _imageService.ValidateImage(stream);
            
            if (!validateFile.isValid)
            {
                _logger.LogError("User image is not valid");
                return BadRequest("File must be an image");
            }

            var imageName = $"{Guid.NewGuid()}{validateFile.ext}";

            await _storageService.UpdateImage(user.Id, imageName, stream);
            _logger.LogInformation($"User {user.UserName} uploaded an image: {imageName}");

            return Ok(new ImageResponseDto()
            {
                ExtensionType = validateFile.ext,
                ImageName = imageName
            });
        }
    }
}
