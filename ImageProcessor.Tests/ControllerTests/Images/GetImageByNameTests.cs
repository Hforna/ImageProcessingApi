using FluentAssertions;
using ImageProcessor.Api.Dtos;
using ImageProcessor.Api.Exceptions;
using ImageProcessor.Api.Services;
using ImageProcessor.Tests.Fakers;
using ImageProcessor.Tests.Mocks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcessor.Tests.ControllerTests.Images
{
    public class GetImageByNameTests
    {
        [Fact]
        public async Task ImageByName_NotInStorage_ReturnNotFound()
        {
            ///Arrange
            var imageName = $"{Guid.NewGuid()}.png";
            var user = new UserModelFaker().GenerateRandomUser();

            var tokenService = new TokenServiceMock();
            tokenService.SetGetUserByToken(user);
            var storageService = new StorageServiceMock();
            var strServiceMock = storageService.GetMock();
            strServiceMock.Setup(d => d.GetImageUrlByName(user.UserIdentifier, imageName))
                .Throws(new FileNotFoundOnStorageException("Image not exists"));

            ///Act
            var controller = new ImageControllerMock().Generate(
                tokenService: tokenService.GetMockObject(), 
                storageService: storageService.GetMockObject());

            var result = await controller.GetImageByName(imageName);

            ///Assert
            result.Should().BeOfType<NotFoundObjectResult>().Which.Value.Should().Be("Image not exists");
        }

        [Fact]
        public async Task ImageByName_EmtpyImageName_ReturnBadRequest()
        {
            var imageName = "";
            var user = new UserModelFaker().GenerateRandomUser();

            var tokenService = new TokenServiceMock();
            tokenService.SetGetUserByToken(user);
            var storageService = new StorageServiceMock();
            var strServiceMock = storageService.GetMock();
            strServiceMock.Setup(d => d.GetImageUrlByName(user.UserIdentifier, imageName))
                .ReturnsAsync("https://imageurl.com/adsfasdf");

            var controller = new ImageControllerMock().Generate(
                tokenService: tokenService.GetMockObject(), 
                storageService: storageService.GetMockObject());

            var result = await controller.GetImageByName(imageName);

            result.Should().BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should().Be("Image name cannot be null or empty");
        }

        [Fact]
        public async Task ImageByName_ExternalError_ReturnException()
        {
            var imageName = $"{Guid.NewGuid()}.jpeg";
            var user = new UserModelFaker().GenerateRandomUser();

            var tokenService = new TokenServiceMock();
            tokenService.SetGetUserByToken(user);
            var storageService = new StorageServiceMock();
            var strServiceMock = storageService.GetMock();
            strServiceMock.Setup(d => d.GetImageUrlByName(user.UserIdentifier, imageName))
                .ThrowsAsync(new Exception("Random internal server error"));

            var controller = new ImageControllerMock().Generate(
                tokenService: tokenService.GetMockObject(),
                storageService: storageService.GetMockObject());

            var result = await controller.GetImageByName(imageName);

            result.Should().BeOfType<ObjectResult>()
                .Which
                .StatusCode.Should().Be(500)
                .And
                .HaveValue("Random internal server error");
        }

        [Fact]
        public async Task ImageByName_ReturnsOk()
        {
            //Arrange
            var imageName = $"{Guid.NewGuid()}.png";
            var user = new UserModelFaker().GenerateRandomUser();

            var tokenService = new TokenServiceMock();
            tokenService.SetGetUserByToken(user);
            var storageService = new StorageServiceMock();
            var strServiceMock = storageService.GetMock();
            strServiceMock.Setup(d => d.GetImageUrlByName(user.UserIdentifier, imageName))
                .ReturnsAsync("https://imageurl.com/adsfasdf");

            //Act
            var controller = new ImageControllerMock().Generate(
                tokenService: tokenService.GetMockObject(),
                storageService: storageService.GetMockObject());

            var result = await controller.GetImageByName(imageName);

            result.Should().BeOfType<OkObjectResult>();
            OkObjectResult objOk = result as OkObjectResult;
            var objRes = objOk!.Value as ImageResponseDto;
            Assert.Equal(".png", objRes.ExtensionType);
            Assert.Equal(imageName, objRes.ImageName);
            Assert.Equal("https://imageurl.com/adsfasdf", objRes.ImageUrl);
        }
    }
}
