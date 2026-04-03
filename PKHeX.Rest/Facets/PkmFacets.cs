using Facet;
using PKHeX.Core;

namespace PKHeX.Rest.Facets
{
    [Facet(typeof(PKM), exclude: [
        nameof(PKM.ExtraBytes),
        nameof(PKM.Data),
        nameof(PKM.NicknameTrash),
        nameof(PKM.OriginalTrainerTrash),
        nameof(PKM.HandlingTrainerTrash),
    ])]
    public partial class PkmFacet
    {
    }

    [Facet(typeof(PKM), Include = [
        nameof(PKM.Nickname),
        nameof(PKM.Generation),
        nameof(PKM.CurrentLevel),
        nameof(PKM.Gender),
        nameof(PKM.EXP),
        nameof(PKM.IsShiny),
    ])]
    public partial class PkmDisplayFacet
    {
    }

    [Facet(typeof(PKM), Include = [
        nameof(PKM.FileName),
    ])]
    public partial class PkmFileInfoFacet
    {
        public Memory<byte> FileData { get; set; }
        public string FileHash { get; set; }
    }
}

