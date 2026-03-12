using System.Security.Cryptography;
using PKHeX.Core;
using Facet.Extensions;
using PKHeX.Rest.Extensions;
using PKHeX.Rest.Facets;

namespace PKHeX.Rest.Services
{
    public class SaveFileService
    {
        private const string DefaultDbPath = "pkmdb";
        private static readonly string TempFolderPath = Path.Combine(Path.GetTempPath(), "pkhex_rest");
        private static readonly string SavesFolderPath = Path.Combine(TempFolderPath, "saves");
        private static readonly string BoxFolderPath = Path.Combine(TempFolderPath, "boxes");
        private static readonly string PkmFolderPath = Path.Combine(TempFolderPath, "pkm");

        /// <summary>
        /// Loads a save file hashes and returns the sender the file hash.
        /// The save file is saved to a temp folder with the hash as a filename
        /// <param name="fileBytes">The save data bytes encoded to base64</param>
        /// <param name="cancel">The token allowing cancellation</param>
        /// <returns>The SHA256 hash of the file on success and an empty string on failure</returns>
        /// </summary>
        public async Task<string> LoadSaveFileAsync(Memory<byte> fileBytes, CancellationToken cancel = default)
        {
            // Before commiting to writing the file check it's a valid pkm save
            if (SaveUtil.TryGetSaveFile(fileBytes, out SaveFile? save) && save.ChecksumsValid)
            {
                await using var stream = new MemoryStream(fileBytes.ToArray());
                var hashedBytes = await SHA256.HashDataAsync(stream, cancel).ConfigureAwait(false);
                // Convert the hashed bytes into a string value
                string hashString = BitConverter.ToString(hashedBytes).Replace("-", "");
                Directory.CreateDirectory(SavesFolderPath);
                var tempFilePath = Path.Combine(SavesFolderPath, $"{hashString}.sav");
                // Write the file to the temp directory
                await File.WriteAllBytesAsync(tempFilePath, fileBytes, cancel).ConfigureAwait(false);
                return hashString;
            }

            return string.Empty;
        }

        /// <summary>
        /// Tries to retrieve a save file from the temp folder using the given hash.
        /// <param name="fileHash">The hash of a PKM save file</param>
        /// <param name="cancel">The token allowing cancellation</param>
        /// <returns>true if a valid PKM savefile is retrieved from the temp folder and returns the save</returns>
        /// </summary>
        private Task<(bool, SaveFile?)> TryRetrieveSaveFileAsync(string fileHash, CancellationToken cancel = default)
        {
            return Task.Run(() =>
            {
                // Check if there is a temp file with the given hash
                var tempFilePath = Path.Combine(SavesFolderPath, $"{fileHash}.sav");
                if (File.Exists(tempFilePath) && SaveUtil.TryGetSaveFile(tempFilePath, out SaveFile? save))
                {
                    return Task.FromResult<(bool, SaveFile?)>((true, save));
                }
                return Task.FromResult<(bool, SaveFile?)>((false, null));
            }, cancel);
        }

        /// <summary>
        /// Gets the number of party PKM in the save file.
        /// </summary>
        /// <param name="fileHash">The SHA256 hash of the PKM save file in the temp folder</param>
        /// <param name="cancel">The token allowing cancellation</param>
        /// <returns>the number of party PKM</returns>
        public async Task<int> GetPartyCountAsync(string fileHash, CancellationToken cancel = default)
        {
            if ((await TryRetrieveSaveFileAsync(fileHash, cancel).ConfigureAwait(false)).TryOut(out var save) && save?.HasParty == true)
            {
                return save.PartyCount;
            }
            return -1;
        }

        /// <summary>
        /// Gets the party data as display facets (limited info).
        /// </summary>
        /// <param name="fileHash">The SHA256 hash of the PKM save file in the temp folder</param>
        /// <param name="cancel">The token allowing cancellation</param>
        /// <returns>The list of PKM as summarised data facets</returns>
        public async Task<List<PkmDisplayFacet>> GetPartyDisplayAsync(string fileHash, CancellationToken cancel = default)
        {
            if ((await TryRetrieveSaveFileAsync(fileHash, cancel).ConfigureAwait(false)).TryOut(out var save) && save?.HasParty == true)
            {
                return save.PartyData
                    .SelectFacets<PkmDisplayFacet>()
                    .ToList();
            }
            return [];
        }

        /// <summary>
        /// Gets the party data as full facets.
        /// </summary>
        /// <param name="fileHash">The SHA256 hash of the PKM save file in the temp folder</param>
        /// <param name="cancel">The token allowing cancellation</param>
        /// <returns>The list of PKM as full data facets</returns>
        public async Task<List<PkmFacet>> GetPartyDataAsync(string fileHash, CancellationToken cancel = default)
        {
            if ((await TryRetrieveSaveFileAsync(fileHash, cancel).ConfigureAwait(false)).TryOut(out var save) && save?.HasParty == true)
            {
                return save.PartyData
                    .SelectFacets<PkmFacet>()
                    .ToList();
            }
            return [];
        }

        /// <summary>
        /// Dumps all party PKM to files to the temp directory and hashes them, returning a list of the hash files
        /// </summary>
        /// <param name="fileHash">The SHA256 hash of the PKM save file in the temp folder</param>
        /// <param name="cancel">The token allowing cancellation</param>
        /// <returns>The list of SHA256 hashes relating to the dumped PKM files</returns>
        public async Task<List<string>> DumpPartyAsync(string fileHash, CancellationToken cancel = default)
        {
            if((await TryRetrieveSaveFileAsync(fileHash, cancel).ConfigureAwait(false)).TryOut(out var save) && save?.HasParty == true)
            {
                Directory.CreateDirectory(PkmFolderPath);
                List<string> dumpedFiles = new List<string>();

                foreach (var p in save.PartyData)
                {
                    var fileName = PathUtil.CleanFileName(p.FileName);
                    var filePath = Path.Combine(PkmFolderPath, fileName);
                    if (File.Exists(filePath))
                        continue;

                    await File.WriteAllBytesAsync(filePath, p.DecryptedPartyData, cancel).ConfigureAwait(false);
                    // Hash the dumped file and add the hash to the list of dumped files
                    await using var stream = File.OpenRead(filePath);
                    var hashedBytes = await SHA256.HashDataAsync(stream, cancel).ConfigureAwait(false);
                    string hashString = BitConverter.ToString(hashedBytes).Replace("-", "");
                    dumpedFiles.Add(hashString);
                    // Rename the file to use the file hash instead
                    File.Move(filePath, Path.Combine(PkmFolderPath, $"{hashString}.{p.Extension}"));
                }

                return dumpedFiles;
            }

            return [];
        }
        /// <summary>
        /// Retrieve the data of the PKM file with this hash
        /// </summary>
        /// <param name="saveFileHash">The SHA256 hash of the PKM save file in the temp folder</param>
        /// <param name="pkmFileHash">The SHA256 hash of the PKM file in the PKM folder</param>
        /// <param name="cancel">The token allowing cancellation</param>
        /// <returns>PkmFileInfoFacet on success</returns>
        public async Task<(bool, PkmFileInfoFacet?)> GetPkmAsync(string pkmFileHash, string saveFileHash = "", CancellationToken cancel = default)
        {
            // If possible retrieve the save file
            var saveFileTask = TryRetrieveSaveFileAsync(saveFileHash, cancel);

            // Look for the pkm file in the PKM folder we don't know the file extension, but they start PK
            // Check if there is a temp file with the given hash
            string[] matchingFiles = Directory.GetFiles(PkmFolderPath, $"{pkmFileHash}.pk*");
            if (matchingFiles.Length > 0)
            {
                string filePath = matchingFiles[0]; // Take the first match
                Memory<byte> fileData = await File.ReadAllBytesAsync(filePath, cancel).ConfigureAwait(false);
                (await saveFileTask.ConfigureAwait(false)).TryOut(out var save);
                if (FileUtil.TryGetPKM(fileData, out PKM? pkm, Path.GetExtension(filePath), save))
                {
                    return (true, new PkmFileInfoFacet
                    {
                        FileName = Path.GetFileName(filePath),
                        FileData = fileData,
                        FileHash = pkmFileHash
                    });
                }
            }

            return (false, null);
        }

        /// <summary>
        /// Gets the number of boxes in the save file.
        /// </summary>
        /// <param name="fileHash">The SHA256 hash of the PKM save file in the temp folder</param>
        /// <param name="cancel">The token allowing cancellation</param>
        /// <returns>the number boxes in the save</returns>
        public async Task<int> GetBoxCountAsync(string fileHash, CancellationToken cancel = default)
        {
            if ((await TryRetrieveSaveFileAsync(fileHash, cancel).ConfigureAwait(false)).TryOut(out var save) && save != null)
            {
                return save.BoxCount;
            }
            return -1;
        }

        /// <summary>
        /// Dumps all PKM from all boxes and returns the hashes
        /// <param name="fileHash">The SHA256 hash of the PKM save file in the temp folder</param>
        /// <param name="cancel">The token allowing cancellation</param>
        /// <returns>a list of lists of hashes. The lists returned in order of the boxes</returns>
        /// </summary>
        public async Task<List<List<string>>> DumpBoxesAsync(string fileHash, CancellationToken cancel = default)
        {
            if((await TryRetrieveSaveFileAsync(fileHash, cancel).ConfigureAwait(false)).TryOut(out var save) && save != null)
            {
                if (Directory.Exists(BoxFolderPath))
                {
                    Directory.Delete(BoxFolderPath, true);
                }
                Directory.CreateDirectory(BoxFolderPath);
                save.DumpBoxes(BoxFolderPath, true);
                var folders = Directory.GetDirectories(BoxFolderPath);
                var output = new List<List<string>>();
                foreach (var folder in folders)
                {
                    var files = Directory.GetFiles(folder, $"*.pk*");
                    var row = new  List<string>();
                    foreach (var p in files)
                    {
                        await using var stream = File.OpenRead(p);
                        var hashedBytes = await SHA256.HashDataAsync(stream, cancel).ConfigureAwait(false);
                        string hashString = BitConverter.ToString(hashedBytes).Replace("-", "");
                        row.Add(hashString);
                        File.Move(p, Path.Combine(PkmFolderPath, $"{hashString}{Path.GetExtension(p)}"));
                    }
                    output.Add(row);
                }
                return output;
            }

            return [];
        }

        /// <summary>
        /// Dumps all PKM from a specific box.
        /// </summary>
        public async Task<List<string>> DumpBoxAsync(string fileHash, int boxIndex = 0, CancellationToken cancel = default)
        {
            if ((await TryRetrieveSaveFileAsync(fileHash, cancel).ConfigureAwait(false)).TryOut(out var save) && save != null)
            {
                if (Directory.Exists(BoxFolderPath))
                {
                    Directory.Delete(BoxFolderPath, true);
                }
                Directory.CreateDirectory(BoxFolderPath);
                save.DumpBox(BoxFolderPath, boxIndex);
                var files = Directory.GetFiles(BoxFolderPath, $"*.pk*");
                var box = new List<string>();
                foreach (var p in files)
                {
                    await using var stream = File.OpenRead(p);
                    var hashedBytes = await SHA256.HashDataAsync(stream, cancel).ConfigureAwait(false);
                    string hashString = BitConverter.ToString(hashedBytes).Replace("-", "");
                    box.Add(hashString);
                    File.Move(p, Path.Combine(PkmFolderPath, $"{hashString}{Path.GetExtension(p)}"));
                }
                return box;
            }

            return [];
        }
    }
}


