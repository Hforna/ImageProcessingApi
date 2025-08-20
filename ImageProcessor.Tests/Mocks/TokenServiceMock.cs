using ImageProcessor.Api.Model;
using ImageProcessor.Api.Services;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcessor.Tests.Mocks
{
    public static class TokenServiceMock
    {
        private static Mock<ITokenService> _mock = new Mock<ITokenService>();

        public static ITokenService GetMock() => _mock.Object;

        public static void SetGetUserByToken(User? user)
        {
            _mock.Setup(d => d.GetUserByToken(It.IsAny<string>())).ReturnsAsync(user);
        }
    }
}
