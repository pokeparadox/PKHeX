using Microsoft.AspNetCore.Mvc;
using PKHeX.Rest.Extensions;
using PKHeX.Rest.Facets;
using PKHeX.Rest.Services;

namespace PKHeX.Rest.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SaveFileController(SaveFileService saveFileService) : ControllerBase
    {
        /// <summary>
        /// Uploads and loads a PKM save file.
        /// The save file is validated and stored in a temporary directory with its SHA256 hash as the identifier.
        /// </summary>
        /// <param name="data">The save file data as bytes</param>
        /// <param name="cancel">Cancellation token</param>
        /// <returns>The SHA256 hash of the loaded save file on success</returns>
        /// <response code="200">Save file successfully loaded. Returns the file hash.</response>
        /// <response code="400">File data is empty or the file is not a valid PKM save file</response>
        [HttpPut("save/upload")]
        [ProducesResponseType<string>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<string>> LoadSaveFileAsync([FromBody] byte[] data, CancellationToken cancel = default)
        {
            Memory<byte> memData = data;
            if (memData.IsEmpty)
            {
                return BadRequest("File data is required");
            }
            var hash = await saveFileService.LoadSaveFileAsync(memData, cancel).ConfigureAwait(false);
            if (string.IsNullOrEmpty(hash))
            {
                return BadRequest("Failed to load save file");
            }

            return Ok(hash);
        }


        /// <summary>
        /// Gets the number of party PKM in a save file via the file's hash.
        /// The save file must have been loaded previously via the upload endpoint.
        /// </summary>
        /// <param name="fileHash">The SHA256 hash of the save file returned from the upload endpoint</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The number of PKM currently in the party</returns>
        /// <response code="200">Successfully retrieved the party count</response>
        /// <response code="400">The fileHash is missing, invalid, or the save file could not be loaded</response>
        [ProducesResponseType<int>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [HttpGet("party/count")]
        public async Task<ActionResult<int>> GetPartyPkmCountAsync([FromQuery] string fileHash, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(fileHash))
            {
                return BadRequest("fileHash is required");
            }

            int count = await saveFileService.GetPartyPkmCountAsync(fileHash, cancellationToken).ConfigureAwait(false);
            if (count == -1)
                return BadRequest("Failed to get party count from save file");

            return Ok(count);
        }

        /// <summary>
        /// Gets the number of server PKM.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The number of PKM currently in the server</returns>
        /// <response code="200">Successfully retrieved the server count</response>
        /// <response code="400">The save file could not be loaded</response>
        [ProducesResponseType<int>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [HttpGet("server/count")]
        public async Task<ActionResult<int>> GetServerPkmCountAsync(CancellationToken cancellationToken = default)
        {
            int count = await saveFileService.GetServerPkmCountAsync(cancellationToken).ConfigureAwait(false);
            if (count == -1)
                return BadRequest("Failed to get server count");

            return Ok(count);
        }
        /// <summary>
        /// Gets the server PKM data with limited display information.
        /// This endpoint returns summarized PKM data suitable for UI display (name, level, species, etc.).
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A list of server PKM as display facets with limited information</returns>
        /// <response code="200">Successfully retrieved server display data</response>
        /// <response code="204">The party is empty</response>
        [ProducesResponseType<List<PkmDisplayFacet>>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [HttpGet("server/display")]
        public async Task <ActionResult<List<PkmDisplayFacet>>> GetServerPkmDisplayAsync(CancellationToken cancellationToken = default)
        {
            var data = await saveFileService.GetServerPkmDisplayAsync(cancellationToken).ConfigureAwait(false);
            if (!data.Any())
            {
                return NoContent();
            }

            return Ok(data);
        }

        /// <summary>
        /// Gets the full detailed data for all server PKM.
        /// This endpoint returns complete PKM data including stats, moves, abilities, and more.
        /// </summary>
        /// <param name="cancel">Cancellation token</param>
        /// <returns>A list of server PKM with complete data facets</returns>
        /// <response code="200">Successfully retrieved server data</response>
        /// <response code="204">The server is empty</response>
        [ProducesResponseType<List<PkmFacet>>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [HttpGet("server/data")]
        public async Task<ActionResult<List<PkmFacet>>> GetServerPkmDataAsync(CancellationToken cancel = default)
        {
            var data = await saveFileService.GetServerPkmDataAsync(cancel).ConfigureAwait(false);
            if (!data.Any())
            {
                return NoContent();
            }

            return Ok(data);
        }


        /// <summary>
        /// Gets the party PKM data with limited display information.
        /// This endpoint returns summarized PKM data suitable for UI display (name, level, species, etc.).
        /// </summary>
        /// <param name="fileHash">The SHA256 hash of the save file returned from the upload endpoint</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A list of party PKM as display facets with limited information</returns>
        /// <response code="200">Successfully retrieved party display data</response>
        /// <response code="204">The party is empty</response>
        /// <response code="400">The fileHash is missing or invalid</response>
        [ProducesResponseType<List<PkmDisplayFacet>>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [HttpGet("party/display")]
        public async Task <ActionResult<List<PkmDisplayFacet>>> GetPartyPkmDisplayAsync([FromQuery] string fileHash, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(fileHash))
            {
                return BadRequest("fileHash is required");
            }

            var partyData = await saveFileService.GetPartyPkmDisplayAsync(fileHash, cancellationToken).ConfigureAwait(false);
            if (!partyData.Any())
            {
                return NoContent();
            }

            return Ok(partyData);
        }

        /// <summary>
        /// Gets the full detailed data for all party PKM.
        /// This endpoint returns complete PKM data including stats, moves, abilities, and more.
        /// </summary>
        /// <param name="fileHash">The SHA256 hash of the save file returned from the upload endpoint</param>
        /// <param name="cancel">Cancellation token</param>
        /// <returns>A list of party PKM with complete data facets</returns>
        /// <response code="200">Successfully retrieved party data</response>
        /// <response code="204">The party is empty</response>
        /// <response code="400">The fileHash is missing or invalid</response>
        [ProducesResponseType<List<PkmFacet>>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [HttpGet("party/data")]
        public async Task<ActionResult<List<PkmFacet>>> GetPartyPkmDataAsync([FromQuery] string fileHash, CancellationToken cancel = default)
        {
            if (string.IsNullOrEmpty(fileHash))
            {
                return BadRequest("fileHash is required");
            }

            var partyData = await saveFileService.GetPartyPkmDataAsync(fileHash, cancel).ConfigureAwait(false);
            if (!partyData.Any())
            {
                return NoContent();
            }

            return Ok(partyData);
        }

        /// <summary>
        /// Dumps all party PKM to individual PKM files.
        /// Each PKM is saved as a separate file in the temporary directory and identified by its SHA256 hash.
        /// </summary>
        /// <param name="fileHash">The SHA256 hash of the save file returned from the upload endpoint</param>
        /// <param name="cancel">Cancellation token</param>
        /// <returns>A list of SHA256 hashes identifying each dumped PKM file</returns>
        /// <response code="200">Successfully dumped party PKM. Returns list of file hashes.</response>
        /// <response code="204">The party is empty, no PKM to dump</response>
        /// <response code="400">The fileHash is missing or invalid</response>
        [ProducesResponseType<List<string>>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [HttpPost("party/dump")]
        public async Task<ActionResult<List<string>>> DumpPartyPkmAsync([FromQuery] string fileHash, CancellationToken cancel = default)
        {
            if (string.IsNullOrEmpty(fileHash))
            {
                return BadRequest("fileHash is required");
            }

            var hashes = await saveFileService.DumpPartyPkmAsync(fileHash, cancel).ConfigureAwait(false);
            if (!hashes.Any())
            {
                return NoContent();
            }

            return Ok(hashes);
        }

        /// <summary>
        /// Gets a specific PKM file by its SHA256 hash.
        /// The PKM file must have been dumped previously via one of the dump endpoints.
        /// </summary>
        /// <param name="pkmHash">The SHA256 hash of the PKM file</param>
        /// <param name="saveHash">The SHA256 hash of the save file</param>
        /// <param name="cancel">Cancellation token</param>
        /// <returns>The PKM file data</returns>
        /// <response code="200">Successfully retrieved PKM data</response>
        /// <response code="400">The pkmHash is missing or invalid</response>
        [ProducesResponseType<PkmFileInfoFacet>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [HttpGet("pkm/{pkmHash}/{saveHash}")]
        public async Task<ActionResult<PkmFileInfoFacet?>> GetPkmAsync([FromRoute] string pkmHash, [FromRoute] string saveHash = "", CancellationToken cancel = default)
        {
            if (string.IsNullOrEmpty(pkmHash))
            {
                return BadRequest("pkmHash is required");
            }

            if ((await saveFileService.GetPkmAsync(pkmHash, saveHash, cancel).ConfigureAwait(false)).TryOut(out var pkmData) && pkmData != null)
            {
                return Ok(pkmData);
            }
            else
            {
                return BadRequest("Failed to retrieve PKM file");
            }
        }

        /// <summary>
        /// Upload a PKM to the server store
        /// It will be checked to be a valid PKM file
        /// </summary>
        /// <param name="fileName">The file name of the PKM file</param>
        /// <param name="fileData">The databytes of the PKM file</param>
        /// <param name="cancel">Cancellation token</param>
        /// <returns>The hash of the uploaded PKM file</returns>
        /// <response code="200">Successfully set PKM data</response>
        /// <response code="400">The data or file name is missing or invalid</response>
        [ProducesResponseType<string>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [HttpPut("pkm/upload/{fileName}")]
        public async Task<ActionResult<string>> SetPkmAsync([FromRoute]string fileName, [FromBody] byte[] fileData, CancellationToken cancel = default)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return BadRequest("fileName is required");
            }
            if (fileData.Length == 0)
            {
                return BadRequest("No fileData");
            }

            Memory<byte> memData = fileData;
            var hash = await saveFileService.SetPkmAsync(memData, fileName, cancel).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(hash))
            {
                return Ok(hash);
            }

            return BadRequest("Failed to set PKM file");
        }
        /// <summary>
        /// Gets the total number of boxes in a save file.
        /// The save file must have been loaded previously via the upload endpoint.
        /// </summary>
        /// <param name="fileHash">The SHA256 hash of the save file returned from the upload endpoint</param>
        /// <param name="cancel">Cancellation token</param>
        /// <returns>The total number of boxes in the save file</returns>
        /// <response code="200">Successfully retrieved box count</response>
        /// <response code="400">The fileHash is missing, invalid, or the save file could not be loaded</response>
        [ProducesResponseType<int>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [HttpGet("boxes/count")]
        public async Task<ActionResult<int>> GetBoxPkmCountAsync([FromQuery] string fileHash, CancellationToken cancel = default)
        {
            if (string.IsNullOrEmpty(fileHash))
            {
                return BadRequest("fileHash is required");
            }

            int count = await saveFileService.GetBoxPkmCountAsync(fileHash, cancel).ConfigureAwait(false);
            if (count == -1)
            {
                return BadRequest("Failed to get box count from save file");
            }

            return Ok(count);
        }

        /// <summary>
        /// Dumps all PKM from all boxes to individual PKM files.
        /// Each PKM is saved as a separate file in the temporary PKM directory and identified by its SHA256 hash.
        /// Files are organised by box in the output structure.
        /// </summary>
        /// <param name="fileHash">The SHA256 hash of the save file returned from the upload endpoint</param>
        /// <param name="cancel">Cancellation token</param>
        /// <returns>A list of lists containing SHA256 hashes for each box's PKM, in order</returns>
        /// <response code="200">Successfully dumped all boxes. Returns nested list of file hashes by box.</response>
        /// <response code="204">The save file has no boxes or all boxes are empty</response>
        /// <response code="400">The fileHash is missing, invalid, or the save file could not be loaded</response>
        [ProducesResponseType<List<List<string>>>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
        /// Dumps all PKM from a specific box to individual PKM files.
        /// Each PKM is saved as a separate file in the temporary PKM directory and identified by its SHA256 hash.
        /// </summary>
        /// <param name="boxIndex">The zero-based index of the box to dump (0 for the first box, 1 for the second, etc.)</param>
        /// <param name="fileHash">The SHA256 hash of the save file returned from the upload endpoint</param>
        /// <param name="cancel">Cancellation token</param>
        /// <returns>A list of SHA256 hashes identifying each dumped PKM file from the specified box</returns>
        /// <response code="200">Successfully dumped the box. Returns list of file hashes.</response>
        /// <response code="204">The specified box is empty or contains no PKM</response>
        /// <response code="400">The fileHash is missing, invalid, or the boxIndex is out of range</response>
        [ProducesResponseType<List<string>>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
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











