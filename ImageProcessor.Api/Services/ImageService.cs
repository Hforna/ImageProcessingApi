using FileTypeChecker.Extensions;
using FileTypeChecker.Types;
using ImageProcessor.Api.Enums;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;

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

        public async Task<Stream> ConvertImageType(Stream imageStream, ImageTypesEnum imageType)
        {
            var outputStream = new MemoryStream();

            using(var image = await Image.LoadAsync(""))
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
