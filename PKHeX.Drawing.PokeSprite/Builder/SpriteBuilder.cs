using PKHeX.Core;
using PKHeX.Drawing.PokeSprite.Properties;
using PKHeX.Drawing.PokeSprite.Util;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Image = SixLabors.ImageSharp.Image;

namespace PKHeX.Drawing.PokeSprite;

public abstract class SpriteBuilder : ISpriteBuilder<Image<Rgba32>>
{
    public static bool ShowEggSpriteAsItem { get; set; } = true;
    public static bool ShowEncounterBall { get; set; } = true;
    public static SpriteBackgroundType ShowEncounterColor { get; set; } = SpriteBackgroundType.FullBackground;
    public static SpriteBackgroundType ShowEncounterColorPKM { get; set; }
    public static SpriteBackgroundType ShowTeraType { get; set; } = SpriteBackgroundType.TopStripe;
    public static bool ShowExperiencePercent { get; set; }
    public static byte ShowTeraOpacityStripe { get; set; }
    public static int ShowTeraThicknessStripe { get; set; }
    public static byte ShowTeraOpacityBackground { get; set; }
    public static byte ShowEncounterOpacityStripe { get; set; }
    public static byte ShowEncounterOpacityBackground { get; set; }
    public static int ShowEncounterThicknessStripe { get; set; }
    public static float FilterMismatchOpacity { get; set; }
    public static float FilterMismatchGrayscale { get; set; }

    /// <summary> Width of the generated Sprite image. </summary>
    public abstract int Width { get; }
    /// <summary> Height of the generated Sprite image. </summary>
    public abstract int Height { get; }

    /// <summary> Minimum amount of padding on the right side of the image when layering an item sprite. </summary>
    protected abstract int ItemShiftX { get; }
    /// <summary> Minimum amount of padding on the bottom side of the image when layering an item sprite. </summary>
    protected abstract int ItemShiftY { get; }
    /// <summary> Max width / height of an item image. </summary>
    protected abstract int ItemMaxSize { get; }

    protected abstract int EggItemShiftX { get; }
    protected abstract int EggItemShiftY { get; }

    public abstract bool HasFallbackMethod { get; }

    public abstract Image<Rgba32> Hover { get; }
    public abstract Image<Rgba32> View { get; }
    public abstract Image<Rgba32> Set { get; }
    public abstract Image<Rgba32> Delete { get; }
    public abstract Image<Rgba32> Transparent { get; }
    public abstract Image<Rgba32> Drag { get; }
    public abstract Image<Rgba32> UnknownItem { get; }
    public abstract Image<Rgba32> None { get; }
    public abstract Image<Rgba32> ItemTM { get; }
    public abstract Image<Rgba32> ItemTR { get; }

    private const double UnknownFormTransparency = 0.5;
    private const double ShinyTransparency = 0.7;
    private const double EggUnderLayerTransparency = 0.33;

    protected abstract string GetSpriteStringSpeciesOnly(ushort species);

    protected abstract string GetSpriteAll(ushort species, byte form, byte gender, uint formarg, bool shiny, EntityContext context);
    protected abstract string GetSpriteAllSecondary(ushort species, byte form, byte gender, uint formarg, bool shiny, EntityContext context);
    protected abstract string GetItemResourceName(int item);
    protected abstract Image<Rgba32> Unknown { get; }
    protected abstract Image<Rgba32> GetEggSprite(ushort species);
    public abstract Image<Rgba32> ShadowLugia { get; }

    /// <summary>
    /// Ensures all data is set up to generate sprites for the save file.
    /// </summary>
    public void Initialize(SaveFile sav)
    {
        if (sav.Generation != 3)
            return;

        // If the game is indeterminate, we might have different form sprites.
        // Currently, this only applies to Gen3's FireRed / LeafGreen
        Version = sav.Version;
        if (Version == GameVersion.FRLG)
            Version = ReferenceEquals(sav.Personal, PersonalTable.FR) ? GameVersion.FR : GameVersion.LG;
    }

    private GameVersion Version;

    private static byte GetDeoxysForm(GameVersion version) => version switch
    {
        GameVersion.FR => 1, // Attack
        GameVersion.LG => 2, // Defense
        GameVersion.E => 3, // Speed
        _ => 0,
    };

    private static byte GetArceusForm4(byte form) => form switch
    {
        > 9 => --form, // Realign to Gen5+ type indexes
        9 => byte.MaxValue, // Curse, make it show as unrecognized form since we don't have a sprite.
        _ => form,
    };

    /// <summary>
    /// Builds a new sprite image with the requested parameters.
    /// </summary>
    /// <param name="species">Entity Species ID</param>
    /// <param name="form">Entity Form index</param>
    /// <param name="gender">Entity gender</param>
    /// <param name="formarg">Entity <see cref="IFormArgument.FormArgument"/> raw value</param>
    /// <param name="heldItem">Entity held item ID</param>
    /// <param name="isEgg">Is currently in an egg</param>
    /// <param name="shiny">Is it shiny</param>
    /// <param name="context">Context the sprite is for</param>
    public Image<Rgba32> GetSprite(ushort species, byte form, byte gender, uint formarg, int heldItem, bool isEgg, Shiny shiny = Shiny.Never, EntityContext context = EntityContext.None)
    {
        if (species == 0)
            return None;

        if (context == EntityContext.Gen3 && species == (int)Species.Deoxys) // Depends on Gen3 save file version
            form = GetDeoxysForm(Version);
        else if (context == EntityContext.Gen4 && species == (int)Species.Arceus) // Curse type's existence in Gen4
            form = GetArceusForm4(form);

        var baseImage = GetBaseImage(species, form, gender, formarg, shiny.IsShiny(), context);
        return GetSprite(baseImage, species, heldItem, isEgg, shiny, context);
    }

    public Image<Rgba32> GetSprite(Image<Rgba32> baseSprite, ushort species, int heldItem, bool isEgg, Shiny shiny, EntityContext context = EntityContext.None)
    {
        if (isEgg)
            baseSprite = LayerOverImageEgg(baseSprite, species, heldItem != 0);
        if (heldItem > 0)
            baseSprite = LayerOverImageItem(baseSprite, heldItem, context);
        if (shiny.IsShiny())
        {
            if (shiny == Shiny.AlwaysSquare && !context.IsSquareShinyDifferentiated)
                shiny = Shiny.Always;
            baseSprite = LayerOverImageShiny(baseSprite, shiny);
        }
        return baseSprite;
    }

    private Image<Rgba32> GetBaseImage(ushort species, byte form, byte gender, uint formarg, bool shiny, EntityContext context)
    {
        var img = FormInfo.IsTotemForm(species, form, context)
            ? GetBaseImageTotem(species, form, gender, formarg, shiny, context)
            : GetBaseImageDefault(species, form, gender, formarg, shiny, context);
        return img ?? GetBaseImageFallback(species, form, gender, formarg, shiny, context);
    }

    private Image<Rgba32>? GetBaseImageTotem(ushort species, byte form, byte gender, uint formarg, bool shiny, EntityContext context)
    {
        var baseform = FormInfo.GetTotemBaseForm(species, form);
        var b = GetBaseImageDefault(species, baseform, gender, formarg, shiny, context);
        if (b is null)
            return null;

        SpriteUtil.GetSpriteGlow(b, 0, 165, 255, out var pixels, true);
        var layer = ImageUtilSharp.GetImage(pixels, b.Width, b.Height);
        return ImageUtilSharp.LayerImage(b, layer, 0, 0);
    }

    private Image<Rgba32>? GetBaseImageDefault(ushort species, byte form, byte gender, uint formarg, bool shiny, EntityContext context)
    {
        var file = GetSpriteAll(species, form, gender, formarg, shiny, context);
        var resource = GetImageFromResource(file);
        if (resource is null && HasFallbackMethod)
        {
            file = GetSpriteAllSecondary(species, form, gender, formarg, shiny, context);
            resource = GetImageFromResource(file);
        }
        return resource;
    }

    private Image<Rgba32> GetBaseImageFallback(ushort species, byte form, byte gender, uint formarg, bool shiny, EntityContext context)
    {
        if (shiny) // try again without shiny
        {
            var img = GetBaseImageDefault(species, form, gender, formarg, false, context);
            if (img is not null)
                return img;
        }

        // try again without form
        var baseImage = GetImageFromResource(GetSpriteStringSpeciesOnly(species));
        if (baseImage is null) // failed again
            return Unknown;
        return ImageUtilSharp.LayerImage(baseImage, Unknown, 0, 0, UnknownFormTransparency);
    }

    private Image<Rgba32> LayerOverImageItem(Image<Rgba32> baseImage, int item, EntityContext context)
    {
        var lump = HeldItemLumpUtil.GetIsLump(item, context);
        var itemimg = lump switch
        {
            HeldItemLumpImage.TechnicalMachine => ItemTM,
            HeldItemLumpImage.TechnicalRecord => ItemTR,
            _ => GetImageFromResource(GetItemResourceName(item)) ?? UnknownItem,
        };

        // Redraw item in bottom right corner; since images are cropped, try to not have them at the edge
        int x = baseImage.Width - itemimg.Width - ((ItemMaxSize - itemimg.Width) / 4) - ItemShiftX;
        int y = baseImage.Height - itemimg.Height - ItemShiftY;
        return ImageUtilSharp.LayerImage(baseImage, itemimg, x, y);
    }

    private static Image<Rgba32> LayerOverImageShiny(Image<Rgba32> baseImage, Shiny shiny)
    {
        // Add shiny star to top left of image.
        Image<Rgba32> rare;
        if (shiny is Shiny.AlwaysSquare)
            rare = ResourcesImageSharp.rare_icon_alt_2;
        else
            rare = ResourcesImageSharp.rare_icon_alt;
        return ImageUtilSharp.LayerImage(baseImage, rare, 0, 0, ShinyTransparency);
    }

    private Image<Rgba32> LayerOverImageEgg(Image<Rgba32> baseImage, ushort species, bool hasItem)
    {
        if (ShowEggSpriteAsItem && !hasItem)
            return LayerOverImageEggAsItem(baseImage, species);
        return LayerOverImageEggTransparentSpecies(baseImage, species);
    }

    private Image<Rgba32> LayerOverImageEggTransparentSpecies(Image<Rgba32> baseImage, ushort species)
    {
        // Partially transparent species.
        baseImage.Mutate(x => x.Opacity((float)EggUnderLayerTransparency));
        // Add the egg layer over-top with full opacity.
        var egg = GetEggSprite(species);
        return ImageUtilSharp.LayerImage(baseImage, egg, 0, 0);
    }

    private Image<Rgba32> LayerOverImageEggAsItem(Image<Rgba32> baseImage, ushort species)
    {
        var egg = GetEggSprite(species);
        return ImageUtilSharp.LayerImage(baseImage, egg, EggItemShiftX, EggItemShiftY); // similar to held item, since they can't have any
    }

    public static void LoadSettings(ISpriteSettings sprite)
    {
        ShowEggSpriteAsItem = sprite.ShowEggSpriteAsHeldItem;
        ShowEncounterBall = sprite.ShowEncounterBall;

        ShowEncounterColor = sprite.ShowEncounterColor;
        ShowEncounterColorPKM = sprite.ShowEncounterColorPKM;
        ShowEncounterThicknessStripe = sprite.ShowEncounterThicknessStripe;
        ShowEncounterOpacityBackground = sprite.ShowEncounterOpacityBackground;
        ShowEncounterOpacityStripe = sprite.ShowEncounterOpacityStripe;
        ShowExperiencePercent = sprite.ShowExperiencePercent;

        ShowTeraType = sprite.ShowTeraType;
        ShowTeraThicknessStripe   = sprite.ShowTeraThicknessStripe;
        ShowTeraOpacityBackground = sprite.ShowTeraOpacityBackground;
        ShowTeraOpacityStripe     = sprite.ShowTeraOpacityStripe;

        FilterMismatchOpacity = sprite.FilterMismatchOpacity;
        FilterMismatchGrayscale = sprite.FilterMismatchGrayscale;
    }
    private static Image<Rgba32>? GetImageFromResource(string name)
    {
        return ResourcesImageSharp.TryGetImageFromResourceManager(name);
    }
}
