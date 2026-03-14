using PKHeX.Core;
using Facet;
namespace PKHeX.Rest.Facets
{
    [Facet(typeof(SaveFile),
        Include = [
        nameof(SaveFile.Version),
        nameof(SaveFile.Generation),
        nameof(SaveFile.SeenCount),
        nameof(SaveFile.CaughtCount),
        nameof(SaveFile.OT),
        nameof(SaveFile.ID32),
        nameof(SaveFile.Money),
        nameof(SaveFile.PlayTimeString)
        ])
    ]
    public partial class SaveFileListingFacet
    {
        public string FileHash { get; set; }

        [MapFrom(nameof(@SaveFile.Metadata.FileName))]
        public string FileName { get; set; }
        public DateTime DateModified { get; set; }
        public string DisplayString => ToString();

        public override string ToString()
        {
            return
                $"Gen:{Generation} V:{Version} OT:{OT}({ID32}) $:{Money} Seen:{SeenCount} Caught:{CaughtCount}{Environment.NewLine}" +
                $"Played:{PlayTimeString} Modified:{DateModified}{Environment.NewLine}" +
                $"File: {FileName}";
        }
    }
}
