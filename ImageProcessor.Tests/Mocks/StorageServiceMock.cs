using ImageProcessor.Api.Services;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcessor.Tests.Mocks
{
    public class StorageServiceMock
    {
        private Mock<IStorageService> _mock = new Mock<IStorageService>();
        public Mock<IStorageService> GetMock() => _mock;
        public IStorageService GetMockObject() => _mock.Object;
        public void SetImageUrl(string url)
        {
            _mock.Setup(d => d.GetImageUrlByName(It.IsAny<Guid>(), It.IsAny<string>())).ReturnsAsync(url);
        }

        public void SetImageUrlByNameThrowingFileNotFound()
        {
            _mock.Setup(d => d.GetImageUrlByName(It.IsAny<Guid>(), It.IsAny<string>())).Throws<FileNotFoundException>();
        }
    }
}
