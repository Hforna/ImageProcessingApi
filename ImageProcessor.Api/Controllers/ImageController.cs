﻿using ImageProcessor.Api.Attributes;
using ImageProcessor.Api.Data;
using ImageProcessor.Api.Dtos;
using ImageProcessor.Api.Dtos.Transformation;
using ImageProcessor.Api.Enums;
using ImageProcessor.Api.Exceptions;
using ImageProcessor.Api.Extensions;
using ImageProcessor.Api.Model;
using ImageProcessor.Api.RabbitMq.Consumers;
using ImageProcessor.Api.RabbitMq.Messages;
using ImageProcessor.Api.RabbitMq.Producers;
using ImageProcessor.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Routing;
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
                ImageType = image.GetImageStreamTypeAsEnum(),
                UserIdentifier = user.UserIdentifier,
                SaveImage = request.SaveChanges,
                CallbackUrl = callbackUrl,
            };

            await _imageProducer.SendImageForResize(message);

            return Ok($"Message is being processed, callbackUrl: await on {callbackUrl}");
        }

        /// <summary>
        /// Apply a filter on an image storaged, avaliable filters are: grayscale and sepia 
        /// </summary>
        /// <param name="request">filter name must be the avaliables and
        /// save changes if user wanna keep filters applied on default storage image</param>
        /// <param name="callbackUrl">callback url for user recive the image with filter applied</param>
        /// <param name="imageName">image name storage by user before</param>
        /// <returns>return an ok with callback path for user await for the image</returns>
        [HttpPost("{imageName}/filters")]
        public async Task<IActionResult> ApplyFiltersOnImage([FromBody] FilterOnImageDto request, [FromQuery]string callbackUrl, [FromRoute]string imageName)
        {
            var user = await _tokenService.GetUserByToken(_tokenService.GetRequestToken()!);

            try
            {
                var image = await _storageService.GetImageStreamByName(user.UserIdentifier, imageName);

                var message = new FilterOnImageMessage()
                {
                    CallbackUrl = callbackUrl,
                    FilterName = request.FilterName,
                    ImageName = imageName,
                    ImageType = image.GetImageStreamTypeAsEnum(),
                    SaveChanges = request.SaveChanges,
                    UserIdentifier = user.UserIdentifier
                };

                var validate = _imageService.ValidateImage(image);

                if (!validate.isValid)
                    throw new Exception("Invalid file format");

                await _imageProducer.SendMessageForApplyFilter(message);

                return Ok($"Message is being processed await on: {callbackUrl}");
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogError(ex, $"Image: {imageName} was not found");

                return NotFound("Image wasn't found");
            }catch(Exception ex)
            {
                _logger.LogError(ex, $"Unexpectadly error occured: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// apply watermark on the image corner, 
        /// user can choose the text as watermark and watermark size
        /// </summary>
        /// <param name="imageName">The image name that user uploaded before</param>
        /// <param name="callbackUrl">callback url for recive the image transformed</param>
        /// <param name="request">watermark size for select the size of watermark on image, 
        /// save change for save changes on image storage and the text for be applied on image</param>
        /// <returns>return an ok with a call back url for recive the image</returns>
        [HttpPost("{imageName}/watermark")]
        public async Task<IActionResult> ApplyWatermark([FromRoute]string imageName, [FromQuery]string callbackUrl, [FromBody]WatermarkDto request)
        {
            if (request.WatermarkSize < 1)
                return BadRequest("Watermark size too short");

            var user = await _tokenService.GetUserByToken(_tokenService.GetRequestToken()!);

            try
            {
                using var image = await _storageService.GetImageStreamByName(user.UserIdentifier, imageName);

                var validate = _imageService.ValidateImage(image);

                if (!validate.isValid)
                    throw new Exception("Invalid file format");

                var message = new ApplyWatermarkMessage()
                {
                    CallbackUrl = callbackUrl,
                    ImageName = imageName,
                    ImageType = (ImageTypesEnum)image.GetImageStreamTypeAsEnum()!,
                    SaveChanges = request.SaveChanges,
                    UserId = user.UserIdentifier,
                    WatermarkSize = request.WatermarkSize,
                    Text = request.Text
                };

                await _imageProducer.SendImageForApplyWatermark(message);

                return Ok($"Message is being processed await on: {callbackUrl}");

            }catch(FileNotFoundException ex)
            {
                return NotFound("Image wasn't found");
            }
        }


        /// <summary>
        /// Rotate an image 
        /// </summary>
        /// <param name="request">set on request if can save changes, for upload the image from storage, degrees number for rotate be rotated</param>
        /// <param name="imageName">image name that user wanna rotate</param>
        /// <param name="callbackUrl">callback for user recive their image if image size be higher than 5 mb</param>
        /// <returns>return an image url or an ok if message is being processed in background</returns>
        /// <exception cref="Exception"></exception>
        [HttpGet("{imageName}/rotate")]
        public async Task<IActionResult> RotateImage([FromBody]RotateImageDto request, [FromRoute]string imageName, [FromQuery]string callbackUrl)
        {
            var user = await _tokenService.GetUserByToken(_tokenService.GetRequestToken()!);
            
            var image = await _storageService.GetImageStreamByName(user.UserIdentifier, imageName);

            var validate = _imageService.ValidateImage(image);

            if (!validate.isValid)
            {
                _logger.LogError("File got from storage isn't a valid");
                throw new Exception("Invalid file format");
            }

            if(_imageService.GetImageSizeInMb(image.Length) > 5)
            {
                var message = new RotateImageMessage()
                {
                    CallbackUrl = callbackUrl,
                    Degrees = request.Degrees,
                    ImageName = imageName,
                    ImageType = image.GetImageStreamTypeAsEnum()!,
                    UserIdentifier = user.UserIdentifier,
                    SaveChanges = request.SaveChanges,
                };

                await _imageProducer.SendImageForRotate(message);

                return Ok($"Message is begin processed, callbackUrl: await on {callbackUrl}");
            }
            var imageType = image.GetImageStreamTypeAsEnum()!;
            var rotate = await _imageService.RotateImage(image, request.Degrees, imageType!);

            if (request.SaveChanges)
                await _storageService.UploadImage(user.UserIdentifier, imageName, rotate);

            await _storageService.UploadImageOnProcess(rotate, imageName);

            var imageUrl = await _storageService.GetImageUrlOnProcessByName(imageName);

            return Ok(new ImageResponseDto()
            {
                ExtensionType = Path.GetExtension(imageName),
                ImageName = imageName,
                ImageUrl = imageUrl
            });
        }

        /// <summary>
        /// crop an image by width and height, user select an image that they uploaded before
        /// </summary>
        /// <param name="imageName">image name user wanna crop</param>
        /// <param name="request">width and heigh configuration for crop, and save images if user wanna keep it on</param>
        /// <param name="callbackUrl">callback url for user recive image when be processed</param>
        /// <returns>return an ok with callback url where user will receive their image</returns>
        [HttpPost("{imageName}/crop")]
        public async Task<IActionResult> CropImage([FromRoute]string imageName, [FromBody]ImageCropDto request, [FromQuery]string callbackUrl)
        {
            var user = await _tokenService.GetUserByToken(_tokenService.GetRequestToken()!);

            var image = await _storageService.GetImageStreamByName(user.UserIdentifier, imageName);
            
            var validate = _imageService.ValidateImage(image);

            if(!validate.isValid)
            {
                _logger.LogError("File got from storage isn't a valid");
                throw new Exception("Invalid file format");
            }

            var message = new CropImageMessage()
            {
                CallbackUrl = callbackUrl,
                Height = request.Height,
                Width = request.Width,
                ImageName = imageName,
                ImageType = image.GetImageStreamTypeAsEnum()!,
                SaveImage = request.SaveChanges,
                UserIdentifier = user.UserIdentifier
            };

            await _imageProducer.SendImageForCrop(message);

            return Ok($"Message is being processed, callbackUrl: {callbackUrl}");
        }

        /// <summary>
        /// Flip a message on horizontal or vertical position, user get image that was they uploaded
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="request">user can set if wanna flip image as horizontal or vertical, choosing 1 for horizontal or 2 for vertical</param>
        /// <returns>return a response containing image name, extension type and an image url containing the image transformed</returns>
        [HttpPost("{imageName}/flip")]
        public async Task<IActionResult> FlipImage([FromRoute]string imageName, [FromBody]FlipImageDto request)
        {
            var user = await _tokenService.GetUserByToken(_tokenService.GetRequestToken()!);

            try
            {
                var image = await _storageService.GetImageStreamByName(user.UserIdentifier, imageName);

                var validate = _imageService.ValidateImage(image);

                if(!validate.isValid)
                {
                    _logger.LogError("File got isn't valid");
                    throw new Exception("Invalid file format");
                }

                var imageType = image.GetImageStreamTypeAsEnum()!;

                var flip = await _imageService.FlipImage(image, imageType, request.FlipType);

                await _storageService.UploadImageOnProcess(flip, imageName);

                var imageUrl = await _storageService.GetImageUrlOnProcessByName(imageName);

                return Ok(new ImageResponseDto()
                {
                    ExtensionType = Path.GetExtension(imageName),
                    ImageName = imageName,
                    ImageUrl = imageUrl
                });
            }catch(FileNotFoundException ex)
            {
                _logger.LogError(ex, "Image name is invalid and wasn't found");
                return NotFound();
            }catch(Exception ex)
            {
                _logger.LogError(ex, $"An unexpectadly error occured: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Changes the format of an existing image that user uploaded before to the specified format
        /// </summary>
        /// <param name="request">Image format conversion request containing the target format</param>
        /// <param name="imageName">Name of the image file to convert</param>
        /// <returns>An url containing the image transformed</returns>
        [HttpPost("{imageName}/format")]
        public async Task<IActionResult> ChangeImageFormat([FromBody]ImageFormatDto request, [FromRoute]string imageName)
        {
            var user = await _tokenService.GetUserByToken(_tokenService.GetRequestToken()!);

            try
            {
                var image = await _storageService.GetImageStreamByName(user.UserIdentifier, imageName);

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

                await _storageService.UploadImageOnProcess(convert, imageName);

                var imageUrl = await _storageService.GetImageUrlOnProcessByName(imageName);

                return Ok(new ImageResponseDto()
                {
                    ExtensionType = Path.GetFileNameWithoutExtension(imageName),
                    ImageName = imageName,
                    ImageUrl = imageUrl
                });
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


        [HttpPost]
        public async Task<IActionResult> UploadImage([FromForm] UploadImageDto request)
        {
            var user = await _tokenService.GetUserByToken(_tokenService.GetRequestToken()!);

            var stream = request.File.OpenReadStream();

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

            var imageUrl = await _storageService.GetImageUrlByName(user.UserIdentifier, imageName);

            return Ok(new ImageResponseDto()
            {
                ExtensionType = validateFile.ext,
                ImageName = imageName,
                ImageUrl = imageUrl
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetUserImages([FromQuery]int page, [FromQuery]int quantity)
        {
            var user = await _tokenService.GetUserByToken(_tokenService.GetRequestToken()!);

            try
            {
                var images = await _storageService.GetAllImagesFromUserContainerPaginated(page, quantity, user!.UserIdentifier);

                if (images.imageInfos!.Count <= 0)
                    return NoContent();

                var response = new ImagesResponseDto() { HasMorePages = images.HasMorePages };
                response.Images = images.imageInfos.Select(d =>
                {
                    var imageResponse = new ImageResponseDto()
                    {
                        ImageName = d.Key,
                        ImageUrl = d.Value,
                        ExtensionType = Path.GetExtension(d.Key)
                    };

                    return imageResponse;   
                }).ToList();

                return Ok(response);
            }catch(FileNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"An error occured while trying to get user images: {ex.Message}");
                throw;
            }
        }


        [HttpGet("{imageName}")]
        public async Task<IActionResult> GetImageByName([FromRoute] string imageName)
        {
            var user = await _tokenService.GetUserByToken(_tokenService.GetRequestToken()!);

            try
            {
                var image = await _storageService.GetImageUrlByName(user.UserIdentifier, imageName);
                _logger.LogInformation($"Image sas was generated: {image}");

                return Ok(new ImageResponseDto()
                {
                    ExtensionType = Path.GetExtension(imageName),
                    ImageName = imageName,
                    ImageUrl = image
                });
            }
            catch (FileNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Internal error: {ex.Message}");

                return StatusCode(500, ex.Message);
            }
        }

    }
}
