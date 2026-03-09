using Facet;
using PKHeX.Core;

namespace PKHeX_cmd.Facets
{
    [Facet(typeof(PKM), exclude: [
        nameof(PKM.ExtraBytes),
        nameof(PKM.Data),
        nameof(PKM.NicknameTrash),
        nameof(PKM.OriginalTrainerTrash),
        nameof(PKM.HandlingTrainerTrash),
        nameof(PKM.EncryptedBoxData),
        nameof(PKM.EncryptedPartyData),
        nameof(PKM.DecryptedBoxData),
        nameof(PKM.DecryptedPartyData)
    ])]
    public partial class PkmFacet
    {
    }
}
