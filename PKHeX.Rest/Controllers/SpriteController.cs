using Microsoft.AspNetCore.Mvc;
using PKHeX.Core;
using PKHeX.Rest.Extensions;
using PKHeX.Rest.Models;
using PKHeX.Rest.Services;

namespace PKHeX.Rest.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SpriteController(SpriteService spriteService, SaveFileService saveFileService) : ControllerBase
    {
        // Get the PKM Sprite FileModel from the hash for the PKM
        /// <summary>
        /// Gets a specific PKM sprite file by its SHA256 hash.
        /// The PKM file must have been dumped previously via one of the dump endpoints.
        /// </summary>
        /// <param name="pkmHash">The SHA256 hash of the PKM file</param>
        /// <param name="saveHash">The SHA256 hash of the save file</param>
        /// <param name="cancel">Cancellation token</param>
        /// <returns>The PKM file data</returns>
        /// <response code="200">Successfully retrieved PKM data</response>
        /// <response code="400">The pkmHash is missing or invalid</response>
        [ProducesResponseType<FileModel>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [HttpGet("pkm/{pkmHash}/{saveHash}/sprite")]
        public async Task<ActionResult<FileModel?>> GetPkmSpriteAsync([FromRoute] string pkmHash, [FromRoute] string saveHash = "", CancellationToken cancel = default)
        {
            if (string.IsNullOrEmpty(pkmHash))
            {
                return BadRequest("pkmHash is required");
            }

            if ((await saveFileService.GetPkmAsync(pkmHash, saveHash, cancel).ConfigureAwait(false)).TryOut(out var pkmData) && pkmData != null)
            {
                // Parse the PKM data into a PKM object
                if (!FileUtil.TryGetPKM(pkmData.FileData, out PKM? pkm, Path.GetExtension(pkmData.FileName)))
                {
                    return BadRequest("Failed to parse PKM data");
                }

                // Get the sprite
                if ((await spriteService.TryRetrievePkmSpriteAsync(pkm, cancel)).TryOut(out var spriteData) && spriteData != null)
                {
                    return Ok(spriteData);
                }
            }

            return BadRequest("Failed to retrieve PKM file");
        }
    }
}
