using System.Security.Cryptography;
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

    [Facet(typeof(PKM), exclude: [
        nameof(PKM.ExtraBytes),
        nameof(PKM.Data),
        nameof(PKM.NicknameTrash),
        nameof(PKM.TrainerIDDisplayFormat),
        nameof(PKM.OriginalTrainerTrash),
        nameof(PKM.HandlingTrainerTrash),
        nameof(PKM.EncryptedBoxData),
        nameof(PKM.EncryptedPartyData),
        nameof(PKM.DecryptedBoxData),
        nameof(PKM.DecryptedPartyData),
        nameof(PKM.FileName),
        nameof(PKM.FileNameWithoutExtension),
        nameof(PKM.Extension),
        nameof(PKM.SIZE_PARTY),
        nameof(PKM.SIZE_STORED),
        nameof(PKM.ShinyXor),
        nameof(PKM.Context),
        nameof(PKM.Gen1),
        nameof(PKM.Gen2),
        nameof(PKM.Gen3),
        nameof(PKM.Gen4),
        nameof(PKM.Gen5),
        nameof(PKM.Gen6),
        nameof(PKM.Gen7),
        nameof(PKM.Gen8),
        nameof(PKM.Gen9),
    ])]
    public partial class PkmDisplayFacet
    {
    }
}
