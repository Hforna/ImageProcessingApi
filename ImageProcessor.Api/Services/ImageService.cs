using FileTypeChecker.Extensions;
using FileTypeChecker.Types;
using ImageProcessor.Api.Enums;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;

namespace ImageProcessor.Api.Services
{
    public class ImageService
    {
        public (bool isValid, string ext) ValidateImage(Stream image)
        {
            var valid = false;
            var ext = "";

            if(image.Is<JointPhotographicExpertsGroup>())
            {
                valid = true;
                ext = GetExtension(JointPhotographicExpertsGroup.TypeExtension);
            } else if(image.Is<PortableNetworkGraphic>())
            {
                valid = true;
                ext = GetExtension(PortableNetworkGraphic.TypeExtension);
            }

            image.Position = 0;

            return (valid, ext);
        }

        public async Task<Stream> ReziseImage(Stream imageStream, int width, int height)
        {
            using var outputStream = new MemoryStream();

            using(var image = await Image.LoadAsync(imageStream))
            {
                width /= 2;
                height /= 2;

                image.Mutate(d => d.Resize(width: width, height: height));

                await image.SaveAsync(outputStream, new PngEncoder());
            }

            outputStream.Position = 0;

            return outputStream;
        }

        public async Task<Stream> ConvertImageType(Stream imageStream, ImageTypesEnum imageType)
        {
            using var outputStream = new MemoryStream();

            using(var image = await Image.LoadAsync(imageStream))
            {
                switch(imageType)
                {
                    case ImageTypesEnum.PNG:
                        await image.SaveAsPngAsync(outputStream);
                        break;
                    case ImageTypesEnum.JPEG:
                        await image.SaveAsJpegAsync(outputStream);
                        break;
                }
            }

            outputStream.Position = 0;

            return outputStream;
        }

        private string GetExtension(string ext)
        {
            return ext.StartsWith('.') ? ext : $".{ext}";
        }
    }
}
