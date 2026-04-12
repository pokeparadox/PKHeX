using PKHeX.Core;
using PKHeX.Drawing.PokeSprite.Properties;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace PKHeX.Drawing.PokeSprite;

/// <summary>
/// 56 high, 68 wide sprite builder using Artwork Sprites
/// </summary>
public sealed class SpriteBuilder5668a : SpriteBuilder
{
    public override int Height => 56;
    public override int Width => 68;

    protected override int ItemShiftX => 2;
    protected override int ItemShiftY => 2;
    protected override int ItemMaxSize => 32;
    protected override int EggItemShiftX => 18;
    protected override int EggItemShiftY => 1;
    public override bool HasFallbackMethod => true;

    protected override string GetSpriteStringSpeciesOnly(ushort species) => 'a' + $"_{species}";
    protected override string GetSpriteAll(ushort species, byte form, byte gender, uint formarg, bool shiny, EntityContext context) => 'a' + SpriteName.GetResourceStringSprite(species, form, gender, formarg, context, shiny);
    protected override string GetSpriteAllSecondary(ushort species, byte form, byte gender, uint formarg, bool shiny, EntityContext context) => 'b' + SpriteName.GetResourceStringSprite(species, form, gender, formarg, context, shiny);
    protected override string GetItemResourceName(int item) => 'a' + $"item_{item}";
    protected override Image<Rgba32> Unknown => ResourcesImageSharp.b_unknown;
    protected override Image<Rgba32> GetEggSprite(ushort species) => species == (int)Species.Manaphy ? ResourcesImageSharp.a_490_e : ResourcesImageSharp.a_egg;

    public override Image<Rgba32> Hover { get; } = ResourcesImageSharp.slotHover68;
    public override Image<Rgba32> View { get; } = ResourcesImageSharp.slotView68;
    public override Image<Rgba32> Set { get; } = ResourcesImageSharp.slotSet68;
    public override Image<Rgba32> Delete { get; } = ResourcesImageSharp.slotDel68;
    public override Image<Rgba32> Transparent { get; } = ResourcesImageSharp.slotTrans68;
    public override Image<Rgba32> Drag => ResourcesImageSharp.slotDrag68;
    public override Image<Rgba32> UnknownItem => ResourcesImageSharp.bitem_unk;
    public override Image<Rgba32> None { get; } = ResourcesImageSharp.b_0;
    public override Image<Rgba32> ItemTM => ResourcesImageSharp.aitem_tm;
    public override Image<Rgba32> ItemTR => ResourcesImageSharp.bitem_tr;
    public override Image<Rgba32> ShadowLugia => ResourcesImageSharp.b_249x;
}
