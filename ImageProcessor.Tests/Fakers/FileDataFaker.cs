using Bogus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcessor.Tests.Fakers
{
    public enum EImageType
    {
        PNG,
        JPEG,
        GIF,
        WEBP
    }

    public class FileDataFaker
    {
        public FakeFileData GenerateFormFileData(EImageType? enumType = null)
        {
            var contentTypes = new List<string>() { "image/png", "image/jpeg", "image/gif", "image/webp" };

            var contentType = enumType is null
            ? new Faker().PickRandom(contentTypes)
            : $"image/{enumType.ToString().ToLower()}";

            var faker = new Faker<FakeFileData>()
                .RuleFor(d => d.FileName, f => f.System.FileName())
                .RuleFor(d => d.ContentType, f => f.PickRandom<string>(contentTypes))
                .RuleFor(d => d.Content, GeneratePngImageBytes()).Generate();

            switch(enumType)
            {
                case EImageType.PNG:
                    faker.Content = GeneratePngImageBytes();
                    break;
                default:
                    faker.Content = new byte[1024];
                    break;
            }

            return faker;
        }

        private byte[] GeneratePngImageBytes(int width = 100, int height = 100)
        {
            byte[] pngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

            using (var ms = new MemoryStream())
            {
                ms.Write(pngSignature, 0, pngSignature.Length);

                var simplePng = new byte[]
                {
                    // PNG signature
                    0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
                    // IHDR chunk
                    0x00, 0x00, 0x00, 0x0D, // Length
                    0x49, 0x48, 0x44, 0x52, // "IHDR"
                    0x00, 0x00, 0x00, 0x01, // Width
                    0x00, 0x00, 0x00, 0x01, // Height
                    0x08, // Bit depth
                    0x02, // Color type (RGB)
                    0x00, // Compression method
                    0x00, // Filter method
                    0x00, // Interlace method
                    0xAE, 0x42, 0x60, 0x82, // CRC
                    // IDAT chunk (minimal)
                    0x00, 0x00, 0x00, 0x0C, // Length
                    0x49, 0x44, 0x41, 0x54, // "IDAT"
                    0x78, 0x9C, 0x63, 0x00, 0x00, 0x00, 0x02, 0x00, 0x01, // Compressed data
                    0xE5, 0x90, 0x27, 0x28, // CRC
                    // IEND chunk
                    0x00, 0x00, 0x00, 0x00, // Length
                    0x49, 0x45, 0x4E, 0x44, // "IEND"
                    0xAE, 0x42, 0x60, 0x82  // CRC
                };

                return simplePng;
            }
        }
    }
}
