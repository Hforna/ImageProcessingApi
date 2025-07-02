using ImageProcessor.Api.Attributes;
using ImageProcessor.Api.Data;
using ImageProcessor.Api.Dtos;
using ImageProcessor.Api.Enums;
using ImageProcessor.Api.Exceptions;
using ImageProcessor.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.TagHelpers;

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

        [HttpPost("{imageName}/format")]
        public async Task<IActionResult> ChangeImageFormat([FromBody]ImageFormatDto request, [FromRoute]string imageName)
        {
            var user = await _tokenService.GetUserByToken(_tokenService.GetRequestToken()!);

            try
            {
                using var image = await _storageService.GetImageByName(user.UserIdentifier, imageName);

                var validate = _imageService.ValidateImage(image);

                if (!validate.isValid)
                {
                    _logger.LogError("File got from storage isn't valid");
                    throw new Exception("Invalid file format");
                }

                var targetFormat = ImageTypesEnum.JPEG.ToString().ToLower();

                if (validate.ext[1..] == targetFormat)
                    return BadRequest("Format type must be different from current");

                var convert = await _imageService.ConvertImageType(image, request.FormatType);
                var baseImageName = Path.GetFileNameWithoutExtension(imageName);

                var returnType = $"img/{targetFormat}";
                return File(convert, returnType, $"{baseImageName}.{targetFormat}");
            } catch(FileNotFoundOnStorageException ex)
            {
                return NotFound(ex.Message);
            }catch(Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
