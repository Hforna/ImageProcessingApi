using FluentAssertions;
using ImageProcessor.Api.Controllers;
using ImageProcessor.Api.Dtos;
using ImageProcessor.Api.Services;
using ImageProcessor.Tests.Fakers;
using ImageProcessor.Tests.Mocks;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcessor.Tests.ControllerTests.Images
{
    public class UploadImageTests
    {
        [Fact]
        public async Task UploadImage_AsNotValidFormat_ReturnBadRequest()
        {
            ///Arrange
            var user = new UserModelFaker().GenerateRandomUser();
            var request = new UploadImageDtoFaker().GenerateUploadImageDto();

            TokenServiceMock.SetGetUserByToken(user);
            var tokenService = TokenServiceMock.GetMock();
            var imageService = new ImageService(new List<IImageFilter>());
            var controller = ImageControllerMock.GenerateImageController(
            tokenService: tokenService,
            imageService: imageService);

            ///Act
            var result = await controller.UploadImage(request);

            ///Assert
            result.Should().BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should().Be("File must be an image");
        }

        [Fact]
        public async Task UploadImage_AsValidFormat_ReturnOk()
        {
            var user = new UserModelFaker().GenerateRandomUser();
            var request = new UploadImageDtoFaker().GenerateUploadImageDto();
            var imageType = EImageType.PNG;
            request.File = FormFileMock.GenerateFormFileMock(new FileDataFaker().GenerateFormFileData(imageType));

            TokenServiceMock.SetGetUserByToken(user);
            var tokenService = TokenServiceMock.GetMock();

            var imageService = new ImageService(new List<IImageFilter>());
            var imageName = $"{request.ImageName}.{imageType.ToString().ToLower()}";

            StorageServiceMock.SetImageUrl("https://imageurl.com");

            var storageService = StorageServiceMock.GetMock();

            var controller = ImageControllerMock.GenerateImageController(
            tokenService: tokenService,
            imageService: imageService,
            storageService: storageService);

            var result = await controller.UploadImage(request);

            result.Should().BeOfType<OkObjectResult>().Which.Value.Should();
            var obj = result as OkObjectResult;
            var value = obj.Value as ImageResponseDto;
            value.ImageUrl.Should().Be("https://imageurl.com");
            value.ImageName.Should().Be(imageName);
        }
    }
}
