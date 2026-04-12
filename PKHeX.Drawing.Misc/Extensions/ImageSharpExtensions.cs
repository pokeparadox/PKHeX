using System;

namespace PKHeX.Drawing.Misc.Extensions
{
    using SixLabors.ImageSharp;
    using SixLabors.ImageSharp.PixelFormats;

    public static class ImageSharpExtensions
    {
        public static void ChangeOpacity(this Image<Rgba32> image, float opacity)
        {
            if (opacity < 0 || opacity > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(opacity), "Opacity must be between 0 and 1.");
            }

            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    Rgba32 pixel = image[x, y];
                    pixel.A = (byte)(pixel.A * opacity);
                    image[x, y] = pixel;
                }
            }
        }
    }

}
