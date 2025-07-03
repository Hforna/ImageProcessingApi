using ImageProcessor.Api.Attributes;
using ImageProcessor.Api.Data;
using ImageProcessor.Api.Dtos;
using ImageProcessor.Api.Enums;
using ImageProcessor.Api.Exceptions;
using ImageProcessor.Api.Model;
using ImageProcessor.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

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

            await _storageService.UploadImage(user.Id, imageName, stream);
            _logger.LogInformation($"User {user.UserName} uploaded an image: {imageName}");

            return Ok(new ImageResponseDto()
            {
                ExtensionType = validateFile.ext,
                ImageName = imageName
            });
        }

        [HttpPost("{imageName}/resize")]
        public async Task<IActionResult> ResizeImage([FromBody]ReziseImageDto request, [FromRoute]string imageName)
        {
            var user = await _tokenService.GetUserByToken(_tokenService.GetRequestToken()!);

            var image = await _storageService.GetImageStreamByName(user.UserIdentifier, imageName);

            var validate = _imageService.ValidateImage(image);

            if(!validate.isValid)
            {
                _logger.LogError("File got from storage isn't valid");
                throw new Exception("Invalid file format");
            }
            var resizeImage = await _imageService.ReziseImage(image, request.Width, request.Height);

            var targetType = validate.ext[1..];
            var baseName = Path.GetFileNameWithoutExtension(imageName);

            return File(resizeImage, $"img/{targetType}", $"{baseName}.{targetType}");
        }

        [HttpGet("{imageName}")]
        public async Task<IActionResult> GetImageByName([FromRoute]string imageName)
        {
            var user = await _tokenService.GetUserByToken(_tokenService.GetRequestToken()!);

            try
            {
                var image = await _storageService.GetImageUrlByName(user.UserIdentifier, imageName);
                _logger.LogInformation($"Image sas was generated: {image}");

                return Ok(image);
            }catch(FileNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"Internal error: {ex.Message}");

                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("{imageName}/format")]
        public async Task<IActionResult> ChangeImageFormat([FromBody]ImageFormatDto request, [FromRoute]string imageName)
        {
            var user = await _tokenService.GetUserByToken(_tokenService.GetRequestToken()!);

            try
            {
                using var image = await _storageService.GetImageStreamByName(user.UserIdentifier, imageName);

                var validate = _imageService.ValidateImage(image);

                if (!validate.isValid)
                {
                    _logger.LogError("File got from storage isn't valid");
                    throw new Exception("Invalid file format");
                }

                var targetFormat = request.FormatType.ToString().ToLower();

                if (validate.ext[1..] == targetFormat)
                    return BadRequest("Format type must be different from current");

                var convert = await _imageService.ConvertImageType(image, request.FormatType);
                var baseImageName = Path.GetFileNameWithoutExtension(imageName);

                var returnType = $"img/{targetFormat}";
                return File(convert, returnType, $"{baseImageName}.{targetFormat}");
            } catch(FileNotFoundOnStorageException ex)
            {
                _logger.LogError(ex, $"Image: {imageName} not found on storage");

                return NotFound(ex.Message);
            }catch(Exception ex)
            {
                _logger.LogError(ex, $"An error occured: {ex.Message}");
                return StatusCode(500, ex.Message);
            }
        }
    }
}
