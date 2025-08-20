using Bogus;
using Castle.Core.Logging;
using ImageProcessor.Api.Controllers;
using ImageProcessor.Api.Data;
using ImageProcessor.Api.RabbitMq.Producers;
using ImageProcessor.Api.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcessor.Tests.ControllerTests.Images
{
    public static class ImageControllerMock
    {
        public static ImageController GenerateImageController(ILogger<ImageController>? logger = null, IUnitOfWork? uow = null, 
            ImageService? imageService = null, IStorageService? storageService = null, ITokenService? tokenService = null)
        {
            logger = logger ?? new Mock<ILogger<ImageController>>().Object;
            uow = uow ?? new Mock<IUnitOfWork>().Object;
            storageService = storageService ?? new Mock<IStorageService>().Object;
            tokenService = tokenService ?? new Mock<ITokenService>().Object;
            imageService = imageService ?? new Mock<ImageService>().Object;
            var rabbitProducer = new Mock<IProcessImageProducer>().Object;

            return new ImageController(logger, uow, storageService, imageService, tokenService, rabbitProducer);
        }
    }
}
