using Microsoft.AspNetCore.Mvc;
using PKHeX.Rest.Facets;
using PKHeX.Rest.Services;

namespace PKHeX.Rest.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SaveFileController(SaveFileService saveFileService) : ControllerBase
    {
        /// <summary>
        /// Gets the number of party PKM in a save file via the files hash.
        /// The save should have been loaded previously
        /// </summary>
        [ProducesResponseType<int>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [HttpGet("party/count")]
        public async Task<ActionResult<int>> GetPartyCountAsync([FromQuery] string fileHash, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(fileHash))
            {
                return BadRequest("fileHash is required");
            }

            int count = await saveFileService.GetPartyCountAsync(fileHash, cancellationToken).ConfigureAwait(false);
            if (count == -1)
                return BadRequest("Failed to get party count from save file");

            return Ok(count);
        }

        /// <summary>
        /// Gets the party data with limited display information.
        /// </summary>
        [ProducesResponseType<List<PkmDisplayFacet>>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [HttpGet("party/display")]
        public async Task <ActionResult<List<PkmDisplayFacet>>> GetPartyDisplayAsync([FromQuery] string fileHash, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(fileHash))
            {
                return BadRequest("fileHash is required");
            }

            var partyData = await saveFileService.GetPartyDisplayAsync(fileHash, cancellationToken).ConfigureAwait(false);
            if (!partyData.Any())
            {
                return NoContent();
            }

            return Ok(partyData);
        }

        /// <summary>
        /// Gets the full party data
        /// </summary>
        [HttpGet("party/data")]
        public async Task<ActionResult<List<PkmFacet>>> GetPartyDataAsync([FromQuery] string fileHash, CancellationToken cancel = default)
        {
            if (string.IsNullOrEmpty(fileHash))
            {
                return BadRequest("fileHash is required");
            }

            var partyData = await saveFileService.GetPartyDataAsync(fileHash, cancel).ConfigureAwait(false);
            if (!partyData.Any())
            {
                return NoContent();
            }

            return Ok(partyData);
        }

        /// <summary>
        /// Dumps all party PKM to files.
        /// </summary>
        [HttpPost("party/dump")]
        public async Task<ActionResult<List<string>>> DumpPartyAsync([FromQuery] string fileHash, CancellationToken cancel = default)
        {
            if (string.IsNullOrEmpty(fileHash))
            {
                return BadRequest("fileHash is required");
            }

            var hashes = await saveFileService.DumpPartyAsync(fileHash, cancel).ConfigureAwait(false);
            if (!hashes.Any())
            {
                return NoContent();
            }

            return Ok(hashes);
        }

        /// <summary>
        /// Gets the number of boxes in a save file.
        /// </summary>
        [HttpGet("boxes/count")]
        public async Task<ActionResult<int>> GetBoxCountAsync([FromQuery] string fileHash, CancellationToken cancel = default)
        {
            if (string.IsNullOrEmpty(fileHash))
            {
                return BadRequest("fileHash is required");
            }

            int count = await saveFileService.GetBoxCountAsync(fileHash, cancel).ConfigureAwait(false);
            if (count == -1)
            {
                return BadRequest("Failed to get box count from save file");
            }

            return Ok(count);
        }

        /// <summary>
        /// Dumps all PKM from all boxes.
        /// </summary>
        [HttpPost("boxes/dump")]
        public async Task<ActionResult<List<List<string>>>> DumpBoxesAsync([FromQuery] string fileHash, CancellationToken cancel = default)
        {
            if (string.IsNullOrEmpty(fileHash))
            {
                return BadRequest("fileHash is required");
            }

            var allBoxes = await saveFileService.DumpBoxesAsync(fileHash, cancel).ConfigureAwait(false);
            if (!allBoxes.Any())
                return NoContent();

            return Ok(allBoxes);
        }

        /// <summary>
        /// Dumps PKM from a specific box.
        /// </summary>
        [HttpPost("boxes/{boxIndex:int}/dump")]
        public async Task<ActionResult<List<string>>> DumpBoxAsync([FromRoute] int boxIndex, [FromQuery] string fileHash, CancellationToken cancel = default)
        {
            if (string.IsNullOrEmpty(fileHash))
                return BadRequest("fileHash is required");

            if (boxIndex < 0)
                return BadRequest("boxIndex must be >= 0");

            var hashes = await saveFileService.DumpBoxAsync(fileHash, boxIndex, cancel).ConfigureAwait(false);
            if (!hashes.Any())
            {
                return NoContent();
            }

            return Ok(hashes);
        }
    }
}



