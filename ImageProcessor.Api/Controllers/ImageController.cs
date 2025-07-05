using ImageProcessor.Api.Attributes;
using ImageProcessor.Api.Data;
using ImageProcessor.Api.Dtos;
using ImageProcessor.Api.Dtos.Transformation;
using ImageProcessor.Api.Enums;
using ImageProcessor.Api.Exceptions;
using ImageProcessor.Api.Extensions;
using ImageProcessor.Api.Model;
using ImageProcessor.Api.RabbitMq.Messages;
using ImageProcessor.Api.RabbitMq.Producers;
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
        private readonly IProcessImageProducer _imageProducer;

        public ImageController(ILogger<ImageController> logger, IUnitOfWork uow, 
            IStorageService storageService, ImageService imageService, 
            ITokenService tokenService, IProcessImageProducer imageProducer)
        {
            _logger = logger;
            _uow = uow;
            _imageProducer = imageProducer;
            _storageService = storageService;
            _imageService = imageService;
            _tokenService = tokenService;
        }

        [HttpPost]
        public async Task<IActionResult> UploadImage([FromForm]UploadImageDto request)
        {
            var user = await _tokenService.GetUserByToken(_tokenService.GetRequestToken()!);

            using var stream = request.File.OpenReadStream();

            var validateFile = _imageService.ValidateImage(stream);
            
            if (!validateFile.isValid)
            {
                _logger.LogError("User image is not valid");
                return BadRequest("File must be an image");
            }

            var imageName = string.IsNullOrEmpty(request.ImageName) 
                ? $"{Guid.NewGuid()}{validateFile.ext}" 
                : $"{request.ImageName}{validateFile.ext}";

            await _storageService.UploadImage(user.UserIdentifier, imageName, stream);
            _logger.LogInformation($"User {user.UserName} uploaded an image: {imageName}");

            return Ok(new ImageResponseDto()
            {
                ExtensionType = validateFile.ext,
                ImageName = imageName
            });
        }

        /// <summary>
        /// Resize a image from user images by heigh and width, 
        /// return a ok and webhook configured will recive the image when
        /// </summary>
        /// <param name="request"></param>
        /// <param name="imageName">image name got from endpoint /api/image/{imageName}</param>
        /// <param name="highQuality">set if user wanna image resized in high quality</param>
        /// <param name="callbackUrl">callback for user recive their image</param>
        /// <returns>Return ok if message and request is valid</returns>
        [HttpPost("{imageName}/resize")]
        public async Task<IActionResult> ResizeImage([FromBody]ReziseImageDto request, [FromRoute]string imageName, [FromQuery]string callbackUrl)
        {
            var user = await _tokenService.GetUserByToken(_tokenService.GetRequestToken()!);

            var image = await _storageService.GetImageStreamByName(user.UserIdentifier, imageName);

            var validate = _imageService.ValidateImage(image);

            if(!validate.isValid)
            {
                _logger.LogError("File got from storage isn't a valid");
                throw new Exception("Invalid file format");
            }

            var message = new ResizeImageMessage()
            {
                Height = request.Height,
                Width = request.Width,
                ImageName = imageName,
                ImageType = (ImageTypesEnum)image.GetImageStreamTypeAsEnum()!,
                UserIdentifier = user.UserIdentifier,
                SaveImage = request.SaveChanges,
                CallbackUrl = callbackUrl,
            };

            await _imageProducer.SendImageForResize(message);

            return Ok("message is being processed");
        }

        [HttpPost("{imageName}/rotate/{degrees}")]
        public async Task<IActionResult> RotateImage([FromRoute]string imageName, [FromRoute]float degrees)
        {

        }

        [HttpPost("{imageName}/crop")]
        public async Task<IActionResult> CropImage([FromRoute]string imageName, [FromBody]ImageCropDto request)
        {
            var user = await _tokenService.GetUserByToken(_tokenService.GetRequestToken()!);

            var image = await _storageService.GetImageStreamByName(user.UserIdentifier, imageName);
            
            var validate = _imageService.ValidateImage(image);

            if(!validate.isValid)
            {
                _logger.LogError("File got from storage isn't a valid");
                throw new Exception("Invalid file format");
            }

            var crop = await _imageService.CropImage(image, request.Width, request.Height, (ImageTypesEnum)image.GetImageStreamTypeAsEnum()!);

            var imageType = validate.ext[1..];
            var baseName = Path.GetFileNameWithoutExtension(imageName);

            if (request.SaveChanges)
                await _storageService.UploadImage(user.UserIdentifier, imageName, crop);

            return File(crop, $"img/{imageType}", $"{baseName}.{imageType}");
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

                if (request.SaveChanges)
                    await _storageService.UploadImage(user.UserIdentifier, imageName, convert);

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
