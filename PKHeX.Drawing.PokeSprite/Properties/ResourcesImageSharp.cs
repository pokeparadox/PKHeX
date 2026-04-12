using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace PKHeX.Drawing.PokeSprite.Properties;

/// <summary>
/// Cross-platform ImageSharp-based resource loader that loads PNG files directly from embedded resources.
/// This avoids System.Drawing dependencies and embedded resource issues on Linux.
/// </summary>
public static class ResourcesImageSharp
{
    private static readonly Dictionary<string, Image<Rgba32>> _imageCache = new();
    private static readonly object _cacheLock = new();

    /// <summary>
    /// Gets an ImageSharp image by resource name, loading PNG files directly from embedded resources.
    /// </summary>
    public static Image<Rgba32> GetImage(string resourceName)
    {
        lock (_cacheLock)
        {
            if (_imageCache.TryGetValue(resourceName, out var cachedImage))
                return cachedImage;
        }

        var assembly = typeof(ResourcesImageSharp).Assembly;
        string manifestName = GetManifestResourceName(resourceName);
        using var stream = assembly.GetManifestResourceStream(manifestName);
        if (stream == null)
            throw new ArgumentException($"Embedded resource '{manifestName}' not found for '{resourceName}'.", nameof(resourceName));

        var image = Image.Load<Rgba32>(stream);

        lock (_cacheLock)
        {
            _imageCache[resourceName] = image;
        }

        return image;
    }

    /// <summary>
    /// Gets an ImageSharp image by resource name, returning null if not found.
    /// </summary>
    public static Image<Rgba32>? TryGetImageFromResourceManager(string resourceName)
    {
        try
        {
            return GetImage(resourceName);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Maps resource names to manifest resource names in the embedded Resources directory.
    /// </summary>
    private static string GetManifestResourceName(string resourceName)
    {
        var baseName = "PKHeX.Drawing.PokeSprite.Resources.img.";

        // Handle specific files that are in the root img directory
        if (resourceName is "warn" or "hint" or "valid")
        {
            return baseName + $"{resourceName}.png";
        }

        // Handle different resource types based on naming patterns
        if (resourceName.StartsWith("ball_"))
        {
            // Ball sprites: ball_1 -> ball/_ball1.png
            var ballNumber = resourceName.Substring(5);
            return baseName + $"ball._ball{ballNumber}.png";
        }
        else if (resourceName.StartsWith("_ball"))
        {
            // Direct ball sprite names: _ball1 -> ball/_ball1.png
            return baseName + $"ball.{resourceName}.png";
        }
        else if (resourceName.StartsWith("sick"))
        {
            // Status sprites: sicksleep -> Status/sicksleep.png
            return baseName + $"Status.{resourceName}.png";
        }
        else if (resourceName.StartsWith("rare_icon") || resourceName.StartsWith("party") ||
                  resourceName is "team" or "locked" or "starter" or "dyna" or "alpha_alt")
        {
            // UI overlay elements in Pokemon Sprite Overlays directory
            return baseName + $"Pokemon_Sprite_Overlays.{resourceName}.png";
        }
        else if (resourceName.StartsWith("slot"))
        {
            // UI slot elements: slotHover68 -> accents/slotHover68.png
            return baseName + $"accents.{resourceName}.png";
        }
        else if (resourceName.StartsWith("b_") || resourceName.StartsWith("a_"))
        {
            // Pokemon sprites - these are more complex, handle in subdirectories
            // For now, return a placeholder path - this will need more specific mapping
            return baseName + $"Big_Pokemon_Sprites.{resourceName}.png";
        }
        else if (resourceName.StartsWith("item_") || resourceName.StartsWith("aitem_") || resourceName.StartsWith("bitem_"))
        {
            // Item sprites
            return baseName + $"Big_Items.{resourceName}.png";
        }
        else
        {
            // Default to accents directory for UI elements
            return baseName + $"accents.{resourceName}.png";
        }
    }

    /// <summary>
    /// Clears the image cache to free memory.
    /// </summary>
    public static void ClearCache()
    {
        lock (_cacheLock)
        {
            foreach (var image in _imageCache.Values)
                image.Dispose();
            _imageCache.Clear();
        }
    }

    // Convenience properties using dynamic file loading (no hardcoding needed!)
    public static Image<Rgba32> slotHover68 => GetImage(nameof(slotHover68));
    public static Image<Rgba32> slotView68 => GetImage(nameof(slotView68));
    public static Image<Rgba32> slotSet68 => GetImage(nameof(slotSet68));
    public static Image<Rgba32> slotDel68 => GetImage(nameof(slotDel68));
    public static Image<Rgba32> slotTrans68 => GetImage(nameof(slotTrans68));
    public static Image<Rgba32> slotDrag68 => GetImage(nameof(slotDrag68));
    public static Image<Rgba32> b_unknown => GetImage(nameof(b_unknown));
    public static Image<Rgba32> a_egg => GetImage(nameof(a_egg));
    public static Image<Rgba32> a_490_e => GetImage(nameof(a_490_e));
    public static Image<Rgba32> b_490_e => GetImage(nameof(b_490_e));
    public static Image<Rgba32> b_egg => GetImage(nameof(b_egg));
    public static Image<Rgba32> bitem_unk => GetImage(nameof(bitem_unk));
    public static Image<Rgba32> b_0 => GetImage(nameof(b_0));
    public static Image<Rgba32> aitem_tm => GetImage(nameof(aitem_tm));
    public static Image<Rgba32> bitem_tm => GetImage(nameof(bitem_tm));
    public static Image<Rgba32> bitem_tr => GetImage(nameof(bitem_tr));
    public static Image<Rgba32> b_249x => GetImage(nameof(b_249x));
    public static Image<Rgba32> party1 => GetImage(nameof(party1));
    public static Image<Rgba32> party2 => GetImage(nameof(party2));
    public static Image<Rgba32> party3 => GetImage(nameof(party3));
    public static Image<Rgba32> party4 => GetImage(nameof(party4));
    public static Image<Rgba32> party5 => GetImage(nameof(party5));
    public static Image<Rgba32> party6 => GetImage(nameof(party6));
    public static Image<Rgba32> dyna => GetImage(nameof(dyna));
    public static Image<Rgba32> alpha_alt => GetImage(nameof(alpha_alt));
    public static Image<Rgba32> warn => GetImage(nameof(warn));
    public static Image<Rgba32> hint => GetImage(nameof(hint));
    public static Image<Rgba32> team => GetImage(nameof(team));
    public static Image<Rgba32> locked => GetImage(nameof(locked));
    public static Image<Rgba32> starter => GetImage(nameof(starter));
    public static Image<Rgba32> valid => GetImage(nameof(valid));
    public static Image<Rgba32> sicksleep => GetImage(nameof(sicksleep));
    public static Image<Rgba32> sicktoxic => GetImage(nameof(sicktoxic));
    public static Image<Rgba32> sickpoison => GetImage(nameof(sickpoison));
    public static Image<Rgba32> sickburn => GetImage(nameof(sickburn));
    public static Image<Rgba32> sickparalyze => GetImage(nameof(sickparalyze));
    public static Image<Rgba32> sickfrostbite => GetImage(nameof(sickfrostbite));
    public static Image<Rgba32> rare_icon_alt => GetImage(nameof(rare_icon_alt));
    public static Image<Rgba32> rare_icon_alt_2 => GetImage(nameof(rare_icon_alt_2));
    public static Image<Rgba32> Bag_Key => GetImage(nameof(Bag_Key));

    /// <summary>
    /// Gets a ball sprite by ball ID.
    /// </summary>
    public static Image<Rgba32> GetBallSprite(byte ball)
    {
        string resource = $"_ball{ball}";
        return TryGetImageFromResourceManager(resource) ?? b_0;
    }
}
