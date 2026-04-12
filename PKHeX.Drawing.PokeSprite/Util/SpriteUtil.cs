using System;
using System.Buffers;
using PKHeX.Core;
using PKHeX.Drawing.PokeSprite.Properties;
using PKHeX.Drawing.PokeSprite.Util;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace PKHeX.Drawing.PokeSprite;

/// <summary>
/// Singleton that builds sprite images.
/// </summary>
public static class SpriteUtil
{
    /// <summary>Square sprite builder instance</summary>
    public static readonly SpriteBuilder5668s SB8s = new();
    /// <summary>Circle sprite builder instance (used in Legends: Arceus)</summary>
    public static readonly SpriteBuilder5668c SB8c = new();
    /// <summary>Circle sprite builder instance (used in Brilliant Diamond, Shining Pearl, Scarlet, and Violet)</summary>
    public static readonly SpriteBuilder5668a SB8a = new();

    /// <summary>Current sprite builder reference used to build sprites.</summary>
    public static SpriteBuilder Spriter { get; private set; } = SB8s;

    /// <summary>
    /// Changes the builder mode to the requested mode.
    /// </summary>
    /// <param name="mode">Requested sprite builder mode</param>
    /// <remarks>If an out-of-bounds value is provided, will not change.</remarks>
    public static void ChangeMode(SpriteBuilderMode mode) => Spriter = mode switch
    {
        SpriteBuilderMode.SpritesArtwork5668 => SB8a,
        SpriteBuilderMode.CircleMugshot5668 => SB8c,
        SpriteBuilderMode.SpritesClassic5668 => SB8s,
        _ => Spriter,
    };

    private const int MaxSlotCount = 30; // slots in a box
    private static int SpriteWidth => Spriter.Width;
    private static int SpriteHeight => Spriter.Height;
    private static int PartyMarkShiftX => SpriteWidth - 16;
    private static int SlotLockShiftX => SpriteWidth - 14;
    private static int SlotTeamShiftX => SpriteWidth - 19;
    private static int FlagIllegalShiftY => SpriteHeight - 16;

    /// <summary>
    /// Sets up the sprite builder to behave with the input <see cref="sav"/>.
    /// </summary>
    /// <param name="sav">Save File to be generating sprites for.</param>
    public static void Initialize(SaveFile sav)
    {
        ChangeMode(SpriteBuilderUtil.GetSuggestedMode(sav));
        Spriter.Initialize(sav);
    }

    public static Image<Rgba32> GetBallSprite(byte ball)
    {
        string resource = SpriteName.GetResourceStringBall(ball);
        return ResourcesImageSharp.TryGetImageFromResourceManager(resource) ?? ResourcesImageSharp.b_0; // Poké Ball (default)
    }

    public static Image<Rgba32>? GetItemSprite(int item) => ResourcesImageSharp.TryGetImageFromResourceManager($"item_{item}");
    public static Image<Rgba32>? GetItemSpriteA(int item) => ResourcesImageSharp.TryGetImageFromResourceManager($"aitem_{item}");

    public static Image<Rgba32> GetSprite(ushort species, byte form, byte gender, uint formarg, int item, bool isegg, Shiny shiny, EntityContext context = EntityContext.None)
    {
        return Spriter.GetSprite(species, form, gender, formarg, item, isegg, shiny, context);
    }

    private static Image<Rgba32> GetSprite(PKM pk)
    {
        var formarg = pk is IFormArgument f ? f.FormArgument : 0;
        var shiny = ShinyExtensions.GetType(pk);

        var img = GetSprite(pk.Species, pk.Form, pk.Gender, formarg, pk.SpriteItem, pk.IsEgg, shiny, pk.Context);
        if (pk is IShadowCapture { IsShadow: true })
        {
            const ushort Lugia = (int)Species.Lugia;
            if (pk.Species is Lugia) // show XD shadow sprite
                img = Spriter.GetSprite(Spriter.ShadowLugia, Lugia, pk.SpriteItem, pk.IsEgg, shiny, pk.Context);

            GetSpriteGlow(pk, 75, 0, 130, out var pixels, out var baseSprite, true);
            var glowImg = ImageUtilSharp.GetImage(pixels, baseSprite.Width, baseSprite.Height);
            return ImageUtilSharp.LayerImage(glowImg, img, 0, 0);
        }
        if (pk is IGigantamaxReadOnly { CanGigantamax: true })
        {
            var gm = ResourcesImageSharp.dyna;
            return ImageUtilSharp.LayerImage(img, gm, (img.Width - gm.Width) / 2, 0);
        }
        if (pk is IAlphaReadOnly { IsAlpha: true })
        {
            var alpha = ResourcesImageSharp.alpha_alt;
            return ImageUtilSharp.LayerImage(img, alpha, SlotTeamShiftX, 0);
        }
        return img;
    }

    private static Image<Rgba32> GetSprite(PKM pk, SaveFile sav, int box, int slot, SlotVisibilityType visibility = SlotVisibilityType.None, StorageSlotType storage = StorageSlotType.None)
    {
        bool inBox = (uint)slot < MaxSlotCount;
        bool empty = pk.Species == 0;
        var sprite = empty ? Spriter.None : GetSprite(pk);

        if (!empty)
        {
            if (SpriteBuilder.ShowTeraType != SpriteBackgroundType.None && pk is ITeraType t)
            {
                var type = t.TeraType;
                if (TeraTypeUtil.IsOverrideValid((byte)type))
                    ApplyTeraColor((byte)type, sprite, SpriteBuilder.ShowTeraType);
            }
            if (visibility.HasFlag(SlotVisibilityType.CheckLegalityIndicate))
            {
                var la = pk.GetType() == sav.PKMType // quick sanity check
                    ? new LegalityAnalysis(pk, sav.Personal, storage)
                    : new LegalityAnalysis(pk, pk.PersonalInfo, storage);

                if (!la.Valid)
                    sprite = ImageUtilSharp.LayerImage(sprite, ResourcesImageSharp.warn, 0, FlagIllegalShiftY);
                else if (pk.Format >= 8 && MoveInfo.IsDummiedMoveAny(pk))
                    sprite = ImageUtilSharp.LayerImage(sprite, ResourcesImageSharp.hint, 0, FlagIllegalShiftY);

                if (SpriteBuilder.ShowEncounterColorPKM != SpriteBackgroundType.None)
                    ApplyEncounterColor(la.EncounterOriginal, sprite, SpriteBuilder.ShowEncounterColorPKM);

                if (SpriteBuilder.ShowExperiencePercent)
                    ApplyExperience(pk, sprite, la.EncounterMatch);
            }
        }
        if (inBox) // in box
        {
            var flags = sav.GetBoxSlotFlags(box, slot);

            // Indicate any battle box teams & according locked state.
            int team = flags.IsBattleTeam();
            if (team >= 0)
                sprite = ImageUtilSharp.LayerImage(sprite, ResourcesImageSharp.team, SlotTeamShiftX, 0);
            if (flags.HasFlag(StorageSlotSource.Locked))
                sprite = ImageUtilSharp.LayerImage(sprite, ResourcesImageSharp.locked, SlotLockShiftX, 0);

            // Some games store Party directly in the list of Pokémon data (LGP/E). Indicate accordingly.
            int party = flags.IsParty();
            if (party >= 0)
                sprite = ImageUtilSharp.LayerImage(sprite, PartyMarks[party], PartyMarkShiftX, 0);
            if (flags.HasFlag(StorageSlotSource.Starter))
                sprite = ImageUtilSharp.LayerImage(sprite, ResourcesImageSharp.starter, 0, 0);
        }

        if (SpriteBuilder.ShowExperiencePercent && !visibility.HasFlag(SlotVisibilityType.CheckLegalityIndicate))
            ApplyExperience(pk, sprite);

        return sprite;
    }

    private static void ApplyTeraColor(byte elementalType, Image<Rgba32> img, SpriteBackgroundType type)
    {
        var color = TypeColor.GetTeraSpriteColor(elementalType);
        var thk = SpriteBuilder.ShowTeraThicknessStripe;
        var op  = SpriteBuilder.ShowTeraOpacityStripe;
        var bg  = SpriteBuilder.ShowTeraOpacityBackground;
        ApplyColor(img, type, color, thk, op, bg);
    }

    public static void ApplyEncounterColor(IEncounterTemplate enc, Image<Rgba32> img, SpriteBackgroundType type)
    {
        var index = (enc.GetType().Name.GetHashCode() * 0x43FD43FD);
        var color = System.Drawing.Color.FromArgb(index);
        var thk = SpriteBuilder.ShowEncounterThicknessStripe;
        var op = SpriteBuilder.ShowEncounterOpacityStripe;
        var bg = SpriteBuilder.ShowEncounterOpacityBackground;
        ApplyColor(img, type, color, thk, op, bg);
    }

    private static void ApplyColor(Image<Rgba32> img, SpriteBackgroundType type, System.Drawing.Color color, int thick, byte opacStripe, byte opacBack)
    {
        // For ImageSharp images, we work directly with pixel data
        if (type == SpriteBackgroundType.BottomStripe)
        {
            int stripeHeight = thick; // from bottom
            if ((uint)stripeHeight > img.Height) // clamp negative & too-high values back to height.
                stripeHeight = img.Height;

            // Apply color blend to bottom stripe
            ApplyColorStripe(img, color, opacStripe, img.Width * (img.Height - stripeHeight), img.Width * img.Height);
        }
        else if (type == SpriteBackgroundType.TopStripe)
        {
            int stripeHeight = thick; // from top
            if ((uint)stripeHeight > img.Height) // clamp negative & too-high values back to height.
                stripeHeight = img.Height;

            // Apply color blend to top stripe
            ApplyColorStripe(img, color, opacStripe, 0, img.Width * stripeHeight);
        }
        else if (type == SpriteBackgroundType.FullBackground) // full background
        {
            // Apply color to entire background
            ApplyColorBackground(img, color, opacBack);
        }
    }

    private static void ApplyColorStripe(Image<Rgba32> img, System.Drawing.Color color, byte opacity, int startPixel, int endPixel)
    {
        var targetColor = new Rgba32(color.R, color.G, color.B, opacity);
        for (int i = startPixel; i < endPixel; i++)
        {
            int y = i / img.Width;
            int x = i % img.Width;
            if (x >= 0 && x < img.Width && y >= 0 && y < img.Height)
            {
                var pixel = img[x, y];
                // Blend with transparency
                byte newA = (byte)((pixel.A * (255 - opacity)) / 255);
                img[x, y] = new Rgba32(color.R, color.G, color.B, newA);
            }
        }
    }

    private static void ApplyColorBackground(Image<Rgba32> img, System.Drawing.Color color, byte opacity)
    {
        var targetColor = new Rgba32(color.R, color.G, color.B, opacity);
        for (int y = 0; y < img.Height; y++)
        {
            for (int x = 0; x < img.Width; x++)
            {
                var pixel = img[x, y];
                if (pixel.A > 0)
                {
                    // Blend color with existing pixel
                    byte newA = Math.Min((byte)255, (byte)(pixel.A + opacity));
                    img[x, y] = new Rgba32(
                        (byte)(((color.R * opacity) + (pixel.R * pixel.A)) / 255),
                        (byte)(((color.G * opacity) + (pixel.G * pixel.A)) / 255),
                        (byte)(((color.B * opacity) + (pixel.B * pixel.A)) / 255),
                        newA
                    );
                }
            }
        }
    }

    private static void ApplyExperience(PKM pk, Image<Rgba32> img, IEncounterTemplate? enc = null)
    {
        const int bpp = 4;
        int start = bpp * SpriteWidth * (SpriteHeight - 1);
        var level = pk.CurrentLevel;
        System.Drawing.Color barColor;

        if (level == Experience.MaxLevel)
        {
            barColor = System.Drawing.Color.Lime;
            WritePixels(img, barColor, start, start + (SpriteWidth * bpp));
            return;
        }

        var pct = Experience.GetEXPToLevelUpPercentage(level, pk.EXP, pk.PersonalInfo.EXPGrowth);
        if (pct is not 0)
        {
            barColor = System.Drawing.Color.DodgerBlue;
            WritePixels(img, barColor, start, start + (int)(SpriteWidth * pct * bpp));
            return;
        }

        var encLevel = enc is { IsEgg: true } ? enc.LevelMin : pk.MetLevel;
        barColor = level != encLevel && pk.HasOriginalMetLocation ? System.Drawing.Color.DarkOrange : System.Drawing.Color.Yellow;
        WritePixels(img, barColor, start, start + (SpriteWidth * bpp));
    }

    private static void WritePixels(Image<Rgba32> img, System.Drawing.Color color, int startByte, int endByte)
    {
        var rgba = new Rgba32(color.R, color.G, color.B, color.A);
        int pixelStart = startByte / 4;
        int pixelEnd = endByte / 4;

        for (int i = pixelStart; i < pixelEnd && i < img.Width * img.Height; i++)
        {
            int y = i / img.Width;
            int x = i % img.Width;
            if (x >= 0 && x < img.Width && y >= 0 && y < img.Height)
                img[x, y] = rgba;
        }
    }

    private static readonly Image<Rgba32>[] PartyMarks =
    [
        ResourcesImageSharp.party1, ResourcesImageSharp.party2, ResourcesImageSharp.party3, ResourcesImageSharp.party4, ResourcesImageSharp.party5, ResourcesImageSharp.party6,
    ];

    public static void GetSpriteGlow(PKM pk, byte blue, byte green, byte red, out byte[] pixels, out Image<Rgba32> baseSprite, bool forceHollow = false)
    {
        bool egg = pk.IsEgg;
        var formarg = pk is IFormArgument f ? f.FormArgument : 0;
        var shiny = pk.IsShiny ? Shiny.Always : Shiny.Never;
        baseSprite = GetSprite(pk.Species, pk.Form, pk.Gender, formarg, 0, egg, shiny, pk.Context);
        GetSpriteGlow(baseSprite, blue, green, red, out pixels, forceHollow || egg);
    }

    public static void GetSpriteGlow(Image<Rgba32> baseSprite, byte blue, byte green, byte red, out byte[] pixels, bool forceHollow = false)
    {
        // Convert Image<Rgba32> to byte array (RGBA format)
        pixels = new byte[baseSprite.Width * baseSprite.Height * 4];
        int index = 0;
        for (int y = 0; y < baseSprite.Height; y++)
        {
            for (int x = 0; x < baseSprite.Width; x++)
            {
                var pixel = baseSprite[x, y];
                pixels[index++] = pixel.R;
                pixels[index++] = pixel.G;
                pixels[index++] = pixel.B;
                pixels[index++] = pixel.A;
            }
        }

        if (!forceHollow)
        {
            ImageUtil.GlowEdges(pixels, blue, green, red, baseSprite.Width);
            return;
        }

        // If the image has any transparency, any derived background will bleed into it.
        // Need to undo any transparency values if any present.
        // Remove opaque pixels from original image, leaving only the glow effect pixels.
        var temp = ArrayPool<byte>.Shared.Rent(pixels.Length);
        var original = temp.AsSpan(0, pixels.Length);
        pixels.CopyTo(original);

        ImageUtil.SetAllUsedPixelsOpaque(pixels);
        ImageUtil.GlowEdges(pixels, blue, green, red, baseSprite.Width);
        ImageUtil.RemovePixels(pixels, original);

        original.Clear();
        ArrayPool<byte>.Shared.Return(temp);
    }

    public static Image<Rgba32> GetLegalIndicator(bool valid) => valid ? ResourcesImageSharp.valid : ResourcesImageSharp.warn;

    // Extension Methods - these need to be converted to internal Image<Rgba32> versions
    // Keep Bitmap versions for backward compatibility
    public static Image<Rgba32> SpriteImage(this PKM pk) => GetSprite(pk);

    public static Image<Rgba32> SpriteImage(this IEncounterTemplate enc)
    {
        if (enc is MysteryGift g)
            return GetMysteryGiftPreviewPoke(g);
        var gender = GetDisplayGender(enc);
        var shiny = enc.IsShiny ? Shiny.Always : Shiny.Never;
        var img = GetSprite(enc.Species, enc.Form, gender, 0, 0, enc.IsEgg, shiny, enc.Context);
        if (SpriteBuilder.ShowEncounterBall && enc is {FixedBall: not Ball.None})
        {
            var ballSprite = GetBallSprite((byte)enc.FixedBall);
            img = ImageUtilSharp.LayerImage(img, ballSprite, 0, img.Height - ballSprite.Height);
        }
        if (enc is IGigantamaxReadOnly {CanGigantamax: true})
        {
            var gm = ResourcesImageSharp.dyna;
            img = ImageUtilSharp.LayerImage(img, gm, (img.Width - gm.Width) / 2, 0);
        }
        if (enc is IAlphaReadOnly { IsAlpha: true })
        {
            var alpha = ResourcesImageSharp.alpha_alt;
            img = ImageUtilSharp.LayerImage(img, alpha, SlotTeamShiftX, 0);
        }
        if (SpriteBuilder.ShowEncounterColor != SpriteBackgroundType.None)
            ApplyEncounterColor(enc, img, SpriteBuilder.ShowEncounterColor);
        return img;
    }

    public static byte GetDisplayGender(IEncounterTemplate enc) => enc switch
    {
        IFixedGender { IsFixedGender: true } s => Math.Max((byte)0, s.Gender),
        IPogoSlot g => (byte)((int)g.Gender & 1),
        _ => 0,
    };

    public static Image<Rgba32> Sprite(this PKM pk, SaveFile sav, int box = -1, int slot = -1,
        SlotVisibilityType visibility = SlotVisibilityType.None, StorageSlotType storage = StorageSlotType.None)
    {
        var result = GetSprite(pk, sav, box, slot, visibility, storage);
        if (visibility.HasFlag(SlotVisibilityType.FilterMismatch))
        {
            // Fade out the sprite
            result.Mutate(x => x.Grayscale(SpriteBuilder.FilterMismatchGrayscale));
            // Apply opacity by modifying alpha of all pixels
            float opacityFactor = SpriteBuilder.FilterMismatchOpacity;
            result.Mutate(x => x.Opacity(opacityFactor));
        }
        return result;
    }

    public static Image<Rgba32> GetMysteryGiftPreviewPoke(MysteryGift gift)
    {
        if (gift is { IsEgg: true, Species: (int)Species.Manaphy }) // Manaphy Egg
            return GetSprite((int)Species.Manaphy, 0, 2, 0, 0, true, Shiny.Never, gift.Context);

        var gender = Math.Max((byte)0, gift.Gender);
        var img = GetSprite(gift.Species, gift.Form, gender, 0, gift.HeldItem, gift.IsEgg, gift.IsShiny ? Shiny.Always : Shiny.Never, gift.Context);

        if (SpriteBuilder.ShowEncounterBall && gift is { FixedBall: not Ball.None })
        {
            var ballSprite = GetBallSprite((byte)gift.FixedBall);
            img = ImageUtilSharp.LayerImage(img, ballSprite, 0, img.Height - ballSprite.Height);
        }

        if (gift is IGigantamaxReadOnly { CanGigantamax: true })
        {
            var gm = ResourcesImageSharp.dyna;
            img = ImageUtilSharp.LayerImage(img, gm, (img.Width - gm.Width) / 2, 0);
        }
        return img;
    }

    public static Image<Rgba32>? GetStatusSprite(this StatusCondition value)
    {
        if (value == 0)
            return null;
        if (value < StatusCondition.Poison)
            return ResourcesImageSharp.sicksleep;
        if (value.HasFlag(StatusCondition.PoisonBad))
            return ResourcesImageSharp.sicktoxic;
        if (value.HasFlag(StatusCondition.Poison))
            return ResourcesImageSharp.sickpoison;
        if (value.HasFlag(StatusCondition.Burn))
            return ResourcesImageSharp.sickburn;
        if (value.HasFlag(StatusCondition.Paralysis))
            return ResourcesImageSharp.sickparalyze;
        if (value.HasFlag(StatusCondition.Freeze))
            return ResourcesImageSharp.sickfrostbite;
        return null;
    }

    public static Image<Rgba32>? GetStatusSprite(this StatusType value)
    {
        return value switch
        {
            StatusType.None => null,
            StatusType.Paralysis => ResourcesImageSharp.sickparalyze,
            StatusType.Sleep => ResourcesImageSharp.sicksleep,
            StatusType.Freeze => ResourcesImageSharp.sickfrostbite,
            StatusType.Burn => ResourcesImageSharp.sickburn,
            StatusType.Poison => ResourcesImageSharp.sickpoison,
            _ => null,
        };
    }
}
