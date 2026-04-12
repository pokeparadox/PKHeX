using System.Drawing;
using PKHeX.Core;
using PKHeX.Drawing.Misc.Extensions;
using PKHeX.Drawing.PokeSprite;
using PKHeX.Drawing.PokeSprite.Properties;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Resources = PKHeX.Drawing.Misc.Properties.Resources;

namespace PKHeX.Drawing.Misc;

/// <summary>
/// Provides utility methods for retrieving and composing sprites for Mystery Gifts.
/// </summary>
public static class MysteryGiftSpriteUtil
{
    /// <summary>
    /// Gets the sprite image for the specified <see cref="MysteryGift"/>.
    /// </summary>
    /// <param name="gift">The mystery gift to get the sprite for.</param>
    /// <returns>A <see cref="Bitmap"/> representing the sprite image.</returns>
    public static Image<Rgba32> Sprite(this MysteryGift gift) => GetSprite(gift);

    private static Image<Rgba32> GetSprite(MysteryGift gift)
    {
        if (gift.IsEmpty)
            return SpriteUtil.Spriter.None;

        var img = GetBaseImage(gift);
        if (SpriteBuilder.ShowEncounterColor != SpriteBackgroundType.None)
            SpriteUtil.ApplyEncounterColor(gift, img, SpriteBuilder.ShowEncounterColor);
        if (gift.GiftUsed)
            img.ChangeOpacity(0.3f);
        return img;
    }

    private static Image<Rgba32> GetBaseImage(MysteryGift gift)
    {
        if (gift is { IsEgg: true, Species: (int)Species.Manaphy }) // Manaphy Egg
            return SpriteUtil.GetMysteryGiftPreviewPoke(gift);
        if (gift.IsEntity)
            return SpriteUtil.GetMysteryGiftPreviewPoke(gift);

        if (gift.IsItem)
        {
            var item = (ushort)gift.ItemID;
            if (ItemStorage7USUM.GetCrystalHeld(item, out var value))
                item = value;
            return SpriteUtil.GetItemSprite(item) ?? ResourcesImageSharp.Bag_Key;
        }
        return ResourcesImageSharp.b_unknown;
    }
}
