using ImageProcessor.Api.Services;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcessor.Tests.Mocks
{
    public static class StorageServiceMock
    {
        private static Mock<IStorageService> _mock = new Mock<IStorageService>();

        public static IStorageService GetMock() => _mock.Object;
        public static void SetImageUrl(string url)
        {
            _mock.Setup(d => d.GetImageUrlByName(It.IsAny<Guid>(), It.IsAny<string>())).ReturnsAsync(url);
        }
    }
}
