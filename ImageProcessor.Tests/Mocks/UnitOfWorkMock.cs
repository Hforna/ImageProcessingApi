using ImageProcessor.Api.Data;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcessor.Tests.Mocks
{
    public static class UnitOfWorkMock
    {
        private static IMock<IUnitOfWork> _mock => new Mock<IUnitOfWork>();

        public static IUnitOfWork GetMock() => _mock.Object;
    }
}
