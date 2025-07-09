using FileTypeChecker.Extensions;
using FileTypeChecker.Types;
using ImageProcessor.Api.Enums;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
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

        public double GetImageSizeInMb(long imageLength) => imageLength / (1024.0 * 1024.0);
        public double GetImageSizeInKb(long imageLength) => imageLength / 1024.0;

        public async Task<Stream> CropImage(Stream imageStream, int width, int height, ImageTypesEnum imageType)
        {
            var outputStream = new MemoryStream();

            using(var image = await Image.LoadAsync(imageStream))
            {
                image.Mutate(d => d.Crop(width, height));

                await SaveImageBasedOnImageType(image, outputStream, imageType);
            }

            outputStream.Position = 0;

            return outputStream;
        }

        public async Task<Stream> RotateImage(Stream imageStream, float degrees, ImageTypesEnum imageType)
        {
            var outputStream = new MemoryStream();

            using (var image = await Image.LoadAsync(imageStream))
            {
                image.Mutate(d => d.Rotate(degrees));

                await SaveImageBasedOnImageType(image, outputStream, imageType);
            }

            outputStream.Position = 0;

            return outputStream;
        }

        private async Task SaveImageBasedOnImageType(Image image, Stream outputStream, ImageTypesEnum imageType)
        {
            IImageEncoder imageEncoder;

            switch(imageType)
            {
                case ImageTypesEnum.JPEG:
                    imageEncoder = new JpegEncoder();
                    break;
                case ImageTypesEnum.PNG:
                    imageEncoder = new PngEncoder();
                    break;
                default:
                    imageEncoder = new PngEncoder();
                    break;
            }

            await image.SaveAsync(outputStream, imageEncoder);
        }

        public async Task<Stream> FlipImage(Stream imageStream, ImageTypesEnum imageType, FlipImageEnum flipType)
        {
            var outputStream = new MemoryStream();

            using (var image = await Image.LoadAsync(imageStream))
            {
                var type = flipType == FlipImageEnum.Horizontal ? FlipMode.Horizontal : FlipMode.Vertical;
                image.Mutate(d => d.Flip(type));

                await SaveImageBasedOnImageType(image, outputStream, imageType);
            }

            outputStream.Position = 0;

            return outputStream;
        }

        public async Task<Stream> ApplyWatermarkOnImage(Stream imageStream, ImageTypesEnum imageType, string text, float watermarkSize)
        {
            var fontCollection = new FontCollection();

            var robotoFamily = fontCollection.Add("fonts/ARIAL.TTF");
            var font = robotoFamily.CreateFont(watermarkSize, FontStyle.Bold);

            var textOptions = new TextOptions(font)
            {
                Dpi = 72,
                KerningMode = KerningMode.Standard
            };

            var rect = TextMeasurer.MeasureSize(text, textOptions);

            var outputStream = new MemoryStream();

            using(var image = await Image.LoadAsync(imageStream))
            {
                image.Mutate(d => d.DrawText(text, font,
                    new Color(Rgba32.ParseHex("#FFFFFF")),
                    new PointF(image.Width - rect.Width - 18f,
                               image.Height - rect.Height - 18f)));

                await SaveImageBasedOnImageType(image, outputStream, imageType);
            }

            outputStream.Position = 0;

            return outputStream;
        }

        public async Task<Stream> ResizeImage(Stream imageStream, int width, int height, ImageTypesEnum imageType)
        {
            var outputStream = new MemoryStream();

            using(var image = await Image.LoadAsync(imageStream))
            {
                width /= 2;
                height /= 2;

                image.Mutate(d => d.Resize(new ResizeOptions()
                {
                    Size = new Size(width, height),
                    Sampler = KnownResamplers.Lanczos8
                }));

                await SaveImageBasedOnImageType(image, outputStream, imageType);
            }

            outputStream.Position = 0;

            return outputStream;
        }

        public async Task<Stream> ConvertImageType(Stream imageStream, ImageTypesEnum imageType)
        {
            var outputStream = new MemoryStream();

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

        public static string GetExtension(string ext)
        {
            return ext.StartsWith('.') ? ext : $".{ext}";
        }
    }
}
