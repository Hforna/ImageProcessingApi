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
    public class TokenServiceMock
    {
        private Mock<ITokenService> _mock = new Mock<ITokenService>();
        public Mock<ITokenService> GetMock() => _mock;
        public ITokenService GetMockObject() => _mock.Object;

        public void SetGetUserByToken(User? user)
        {
            _mock.Setup(d => d.GetUserByToken()).ReturnsAsync(user);
        }
    }
}
