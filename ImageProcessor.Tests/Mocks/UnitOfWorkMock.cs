using ImageProcessor.Api.Data;
using ImageProcessor.Api.Services;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcessor.Tests.Mocks
{
    public class UnitOfWorkMock
    {
        private Mock<IUnitOfWork> _mock => new Mock<IUnitOfWork>();

        public IUnitOfWork GetMockObject() => _mock.Object;
        public Mock<IUnitOfWork> GetMock() => _mock;
    }
}
