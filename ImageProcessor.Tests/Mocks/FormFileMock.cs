using ImageProcessor.Tests.Fakers;
using Microsoft.AspNetCore.Http;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcessor.Tests.Mocks
{
    public class FormFileMock
    {
        public IFormFile GenerateFormFileMock(FakeFileData fileData = null)
        {
            var faker = fileData ?? new FileDataFaker().GenerateFormFileData();

            var mockFormFile = new Mock<IFormFile>();
            mockFormFile.Setup(f => f.FileName).Returns(faker.FileName);
            mockFormFile.Setup(f => f.ContentType).Returns(faker.ContentType);
            mockFormFile.Setup(f => f.Length).Returns(faker.Content.Length);
            mockFormFile.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(faker.Content));
            mockFormFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Callback<Stream, CancellationToken>((stream, token) => new MemoryStream(faker.Content).CopyTo(stream));

            return mockFormFile.Object;
        }
    }
}
