using PKHeX.Core;
using PKHeX.Drawing;
using PKHeX.Drawing.PokeSprite;
using PKHeX.Rest.Extensions;
using PKHeX.Rest.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace PKHeX.Rest.Services
{
    public class SpriteService
    {
        /// <summary>
        /// Tries to retrieve a pkm sprite from the temp folder using the given hash.
        /// <param name="pkmFile">The PKM file</param>
        /// <param name="cancel">The token allowing cancellation</param>
        /// <returns>true if a valid PKM savefile is retrieved from the temp folder and returns the save</returns>
        /// </summary>
        public async Task<(bool, FileModel?)> TryRetrievePkmSpriteAsync(PKM pkmFile, CancellationToken cancel = default)
        {
            /*SkiaSharp
            using (var bmap = new SKBitmap(1, 1, false))
            using (var img = SKImage.FromBitmap(bmap))
            using (var bytes = img.Encode(SKEncodedImageFormat.Jpeg, 0)) {
                return this.File(bytes.ToArray(), "image/jpeg");
            }
            */


            //public static Image<Rgba32> GetSprite(ushort species, byte form, byte gender, uint formarg, int item, bool isegg, Shiny shiny, EntityContext context = EntityContext.None)
            var image = SpriteUtil.GetSprite(pkmFile.Species, pkmFile.Form, pkmFile.Gender, pkmFile.Form, 0,
                pkmFile.IsEgg, pkmFile.IsShiny ? Shiny.Always : Shiny.Never);
            var width = SpriteUtil.Spriter.Width;
            var height = SpriteUtil.Spriter.Height;
            if (width > 0 && height > 0)
            {

                // Encode as PNG using ImageSharp
                using var ms = new MemoryStream();
                await image.SaveAsPngAsync(ms, cancel).ConfigureAwait(false);
                var fileData = ms.ToArray();
                var fileHash = await fileData.HashStringAsync(cancel).ConfigureAwait(false);
                var fileModel = new FileModel
                {
                    FileName = $"{pkmFile.Species}_{pkmFile.Form}_{pkmFile.Gender}_{pkmFile.IsEgg}_{pkmFile.IsShiny}.png",
                    FileHash = fileHash,
                    FileData = fileData
                };
                return (true, fileModel);
            }

            return (false, null);
        }
    }
}
