using FileTypeChecker.Extensions;
using FileTypeChecker.Types;
using ImageProcessor.Api.Enums;
using ImageProcessor.Api.Services;
using Microsoft.Identity.Client;
using static System.Net.Mime.MediaTypeNames;

namespace ImageProcessor.Api.Extensions
{
    public static class FileTyeExtension
    {
        public static ImageTypesEnum GetImageStreamTypeAsEnum(this Stream image)
        {
            var ext = "";

            if (image.Is<JointPhotographicExpertsGroup>())
            {
                ext = ImageService.GetExtension(JointPhotographicExpertsGroup.TypeExtension);
            }
            else if (image.Is<PortableNetworkGraphic>())
            {
                ext = ImageService.GetExtension(PortableNetworkGraphic.TypeExtension);
            }

            image.Position = 0;

            if (string.IsNullOrEmpty(ext))
                throw new Exception("Stream file isn't an image");

            ImageTypesEnum type;

            switch (ext)
            {
                case (".png"):
                    type = ImageTypesEnum.PNG;
                    break;
                case (".jpeg"):
                    type = ImageTypesEnum.JPEG;
                    break;
                default:
                    throw new Exception("Stream file isn't an image");
            }

            return type;
        }
    }
}
