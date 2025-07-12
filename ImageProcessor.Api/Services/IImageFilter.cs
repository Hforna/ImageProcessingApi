using ImageProcessor.Api.Enums;
using SkiaSharp;

namespace ImageProcessor.Api.Services
{
    public interface IImageFilter
    {
        public string Name { get; }
        public Stream ApplyFilter(Stream image, ImageTypesEnum imageType);
    }


    public class GrayscaleFilter : IImageFilter
    {
        public string Name => "grayscale";

        public Stream ApplyFilter(Stream image, ImageTypesEnum imageType)
        {
            var outputStream = new MemoryStream();

            var grayscaleMatrix = new float[]
            {
                0.299f, 0.587f, 0.114f, 0, 0,
                0.299f, 0.587f, 0.114f, 0, 0,
                0.299f, 0.587f, 0.114f, 0, 0,
                0,      0,      0,      1, 0
            };

            using (var originalBitmap = SKBitmap.Decode(image))
            {
                if (originalBitmap == null)
                    throw new InvalidOperationException("Failed to decode the input image");

                using (var grayscaleBitmap = new SKBitmap(originalBitmap.Width, originalBitmap.Height))
                using (var canvas = new SKCanvas(grayscaleBitmap))
                using (var paint = new SKPaint())
                {
                    paint.ColorFilter = SKColorFilter.CreateColorMatrix(grayscaleMatrix);
                    canvas.DrawBitmap(originalBitmap, 0, 0, paint);
                    canvas.Flush();

                    using (var filteredImage = SKImage.FromBitmap(grayscaleBitmap))
                    using (var encodedData = filteredImage.Encode(ConvertTypeFilter.GetImageFormatByImageType(imageType), 100))
                    {
                        encodedData.SaveTo(outputStream);
                    }
                }
            }
            outputStream.Position = 0;

            return outputStream;
        }
    }

    public class SepiaFilter : IImageFilter
    {
        public string Name => "sepia";

        public Stream ApplyFilter(Stream image, ImageTypesEnum imageType)
        {
            var outputStream = new MemoryStream();

            using var decodeImage = SKBitmap.Decode(image);
            using (var sepia = new SKBitmap(decodeImage.Width, decodeImage.Height))
            {
                using(var canvas = new SKCanvas(sepia))
                {
                    float[] sepiaMatrix = new float[]
                    {
                        0.393f, 0.769f, 0.189f, 0, 0, // R
                        0.349f, 0.686f, 0.168f, 0, 0, // G
                        0.272f, 0.534f, 0.131f, 0, 0, // B
                        0,      0,      0,      1, 0  // A
                    };

                    var colorFilter = SKColorFilter.CreateColorMatrix(sepiaMatrix);

                    using (var paint = new SKPaint())
                    {
                        paint.ColorFilter = colorFilter;
                        canvas.DrawBitmap(decodeImage, 0, 0, paint);
                    }

                    using var newImage = SKImage.FromBitmap(sepia);
                    using (var data = sepia.Encode(ConvertTypeFilter.GetImageFormatByImageType(imageType), 100))
                    {
                        data.SaveTo(outputStream);
                    }
                }
            }

            outputStream.Position = 0;

            return outputStream;
        }
    }

    public static class ConvertTypeFilter
    {
        public static SKEncodedImageFormat GetImageFormatByImageType(ImageTypesEnum imageType)
        {
            SKEncodedImageFormat format;

            switch (imageType)
            {
                case ImageTypesEnum.JPEG:
                    format = SKEncodedImageFormat.Jpeg;
                    break;
                case ImageTypesEnum.PNG:
                    format = SKEncodedImageFormat.Png;
                    break;
                default:
                    format = SKEncodedImageFormat.Png;
                    break;
            }

            return format;
        }
    }
}
