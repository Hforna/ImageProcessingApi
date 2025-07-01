using FileTypeChecker.Extensions;
using FileTypeChecker.Types;

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

        private string GetExtension(string ext)
        {
            return ext.StartsWith('.') ? ext : $".{ext}";
        }
    }
}
