using Bogus;
using ImageProcessor.Api.Dtos;
using ImageProcessor.Tests.Mocks;
using Microsoft.AspNetCore.Http;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcessor.Tests.Fakers
{
    public class UploadImageDtoFaker
    {
        public UploadImageDto GenerateUploadImageDto()
        {
            return new Faker<UploadImageDto>()
                .RuleFor(d => d.ImageName, Guid.NewGuid().ToString())
                .RuleFor(d => d.File, FormFileMock.GenerateFormFileMock());
        }
    }


    public class FakeFileData
    {
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public byte[] Content { get; set; }
    }

}
