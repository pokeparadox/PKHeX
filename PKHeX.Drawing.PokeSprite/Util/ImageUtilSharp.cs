using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace PKHeX.Drawing.PokeSprite.Util;

/// <summary>
/// ImageSharp-compatible image utility methods for cross-platform sprite rendering.
/// </summary>
public static class ImageUtilSharp
{
    /// <summary>
    /// Layers an image over another with transparency support.
    /// </summary>
    public static Image<Rgba32> LayerImage(Image<Rgba32> baseLayer, Image<Rgba32> overLayer, int x, int y, double transparency = 1.0)
    {
        // Clone the base layer
        var result = baseLayer.Clone();

        // Apply transparency to overlay if needed
        if (transparency < 1.0)
        {
            var overlay = overLayer.Clone();
            result.Mutate(img => img.DrawImage(overlay, new Point(x, y), 1.0f));
            overlay.Dispose();
        }
        else
        {
            // Draw the overlay onto the result at full opacity
            result.Mutate(img => img.DrawImage(overLayer, new Point(x, y), 1.0f));
        }

        return result;
    }

    /// <summary>
    /// Creates a Bitmap from raw pixel data (RGBA format).
    /// </summary>
    public static Image<Rgba32> GetImage(byte[] pixels, int width, int height)
    {
        var image = new Image<Rgba32>(width, height);
        int index = 0;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                byte r = pixels[index++];
                byte g = pixels[index++];
                byte b = pixels[index++];
                byte a = pixels[index++];
                image[x, y] = new Rgba32(r, g, b, a);
            }
        }

        return image;
    }
}


