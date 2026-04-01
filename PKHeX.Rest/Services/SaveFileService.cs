using System.Security.Cryptography;
using PKHeX.Core;
using Facet.Extensions;
using PKHeX.Rest.Extensions;
using PKHeX.Rest.Facets;

namespace PKHeX.Rest.Services
{
    public class SaveFileService
    {
        private static readonly string TempFolderPath = Path.Combine(Path.GetTempPath(), "pkhex_rest");
        private static readonly string BaseFolderPath = Directory.GetCurrentDirectory();
        private static readonly string SavesFolderPath = Path.Combine(BaseFolderPath, "saves");
        private static readonly string BoxFolderPath = Path.Combine(TempFolderPath, "boxes");
        private static readonly string PkmFolderPath = Path.Combine(BaseFolderPath, "pkm");

        /// <summary>
        /// Loads a save file hashes and returns the sender the file hash.
        /// The save file is saved to the saves folder with the hash as a filename
        /// <param name="fileBytes">The save data bytes </param>
        /// <param name="cancel">The token allowing cancellation</param>
        /// <returns>The SHA256 hash of the file on success and an empty string on failure</returns>
        /// </summary>
        public async Task<string> LoadSaveFileAsync(Memory<byte> fileBytes, CancellationToken cancel = default)
        {
            // Before commiting to writing the file check it's a valid pkm save
            if (SaveUtil.TryGetSaveFile(fileBytes, out SaveFile? save) && save.ChecksumsValid)
            {
                string hashString =  await fileBytes.ToArray().HashStringAsync(cancel).ConfigureAwait(false);
                Directory.CreateDirectory(SavesFolderPath);
                var savesFolder = Path.Combine(SavesFolderPath, $"{hashString}.sav");
                // Write the file to the saves directory
                await File.WriteAllBytesAsync(savesFolder, fileBytes, cancel).ConfigureAwait(false);
                return hashString;
            }

            return string.Empty;
        }

        /// <summary>
        /// List all the seen save files seen by the server with an information overview
        /// <param name="cancel">The token allowing cancellation</param>
        /// <returns>The list of SaveFileListingFacet</returns>
        /// </summary>
        public Task<List<SaveFileListingFacet>> GetSaveFileListingsAsync(CancellationToken cancel = default)
        {
            return Task.Run(() =>
            {
                if (Directory.Exists(SavesFolderPath))
                {
                    var files = Directory.GetFiles(SavesFolderPath, $"*.sav");
                    var output = new List<SaveFileListingFacet>();
                    foreach (var file in files)
                    {
                        if (SaveUtil.TryGetSaveFile(file, out SaveFile? save))
                        {
                            var facet = save.ToFacet<SaveFileListingFacet>();
                            facet.FileHash = Path.GetFileNameWithoutExtension(file);
                            facet.DateModified = File.GetLastWriteTime(file);
                            output.Add(facet);
                        }
                    }
                    return output;
                }
                return [];
            }, cancel);
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
                var saveFile = Path.Combine(SavesFolderPath, $"{fileHash}.sav");
                if (File.Exists(saveFile) && SaveUtil.TryGetSaveFile(saveFile, out SaveFile? save))
                {
                    return Task.FromResult<(bool, SaveFile?)>((true, save));
                }
                return Task.FromResult<(bool, SaveFile?)>((false, null));
            }, cancel);
        }


        /// <summary>
        /// Tries to delete a save file from the saves folder using the given hash.
        /// <param name="fileHash">The hash of a PKM save file</param>
        /// <param name="cancel">The token allowing cancellation</param>
        /// <returns>true if a valid PKM savefile is deleted from the saves folder</returns>
        /// </summary>
        public Task<bool> DeleteSaveFileAsync(string fileHash, CancellationToken cancel = default)
        {
            return Task.Run(() =>
            {
                var saveFile = Path.Combine(SavesFolderPath, $"{fileHash}.sav");
                if (File.Exists(saveFile))
                {
                    File.Delete(saveFile);
                    return true;
                }

                return false;
            }, cancel);
        }

        /// <summary>
        /// Gets the number of party PKM in the server.
        /// </summary>
        /// <param name="cancel">The token allowing cancellation</param>
        /// <returns>the number of server PKM</returns>
        public Task<int> GetServerPkmCountAsync(CancellationToken cancel = default)
        {
            return Task.Run(() =>
            {
                if (Directory.Exists(PkmFolderPath))
                {
                    var files = Directory.GetFiles(PkmFolderPath, $"*.pk*");
                    return files.Length;
                }

                return -1;
            }, cancel);
        }

        /// <summary>
        /// Gets the server data as display facets (limited info).
        /// </summary>
        /// <param name="cancel">The token allowing cancellation</param>
        /// <returns>The list of PKM as summarised data facets of type T</returns>
        private async Task<List<T>> GetServerPkmAsync<T>(CancellationToken cancel = default) where T : class
        {
            if (Directory.Exists(PkmFolderPath))
            {
                var files = Directory.GetFiles(PkmFolderPath, $"*.pk*");
                var output = new List<T>();
                foreach (var file in files)
                {
                    var bytes = await File.ReadAllBytesAsync(file, cancel).ConfigureAwait(false);
                    if (FileUtil.TryGetPKM(bytes, out PKM? pkm, Path.GetExtension(file)))
                    {
                        output.Add(pkm.ToFacet<T>());
                    }
                }
                return output;
            }
            return [];
        }

        /// <summary>
        /// Gets the server data as display facets (limited info).
        /// </summary>
        /// <param name="cancel">The token allowing cancellation</param>
        /// <returns>The list of PKM as summarised data facets</returns>
        public Task<List<PkmDisplayFacet>> GetServerPkmDisplayAsync(CancellationToken cancel = default)
        {
            return GetServerPkmAsync<PkmDisplayFacet>(cancel);
        }

        /// <summary>
        /// Gets the server data as display facets (limited info).
        /// </summary>
        /// <param name="cancel">The token allowing cancellation</param>
        /// <returns>The list of PKM as summarised data facets</returns>
        public Task<List<PkmFacet>> GetServerPkmDataAsync(CancellationToken cancel = default)
        {
            return GetServerPkmAsync<PkmFacet>(cancel);
        }

        /// <summary>
        /// Gets the number of party PKM in the save file.
        /// </summary>
        /// <param name="fileHash">The SHA256 hash of the PKM save file in the temp folder</param>
        /// <param name="cancel">The token allowing cancellation</param>
        /// <returns>the number of party PKM</returns>
        public async Task<int> GetPartyPkmCountAsync(string fileHash, CancellationToken cancel = default)
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
        public async Task<List<PkmDisplayFacet>> GetPartyPkmDisplayAsync(string fileHash, CancellationToken cancel = default)
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
        public async Task<List<PkmFacet>> GetPartyPkmDataAsync(string fileHash, CancellationToken cancel = default)
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
        public async Task<List<string>> DumpPartyPkmAsync(string fileHash, CancellationToken cancel = default)
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
                    string hashString = await p.DecryptedPartyData.HashStringAsync(cancel).ConfigureAwait(false);
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
        ///  Put a PKM file into the server PKM folder
        /// </summary>
        /// <param name="pkmFileData">The file data of the PKM file in a byte array</param>
        /// <param name="fileName">The filename including the file extension of the PKM file</param>
        /// <param name="cancel">The token allowing cancellation</param>
        /// <returns>The PKM file SHA256 hash on success or empty on failure</returns>
        public async Task<string> SetPkmAsync(Memory<byte> pkmFileData, string fileName, CancellationToken cancel = default)
        {
            Directory.CreateDirectory(PkmFolderPath);
            if (FileUtil.TryGetPKM(pkmFileData, out PKM? pkm, Path.GetExtension(fileName)))
            {
                var tempFile = Path.Combine(TempFolderPath, fileName);
                await File.WriteAllBytesAsync(tempFile, pkmFileData, cancel).ConfigureAwait(false);
                string hashString = await pkmFileData.ToArray().HashStringAsync(cancel).ConfigureAwait(false);
                // Move the hashed file from temp to PKM folder
                File.Move(tempFile, Path.Combine(PkmFolderPath, $"{hashString}{Path.GetExtension(fileName)}"));
                return hashString;
            }

            return string.Empty;
        }

        /// <summary>
        ///  Frees a PKM from the server permanently by the file hash, deleting the file from the PKM folder
        /// </summary>
        /// <param name="pkmFileHash">The SHA256 hash of the PKM file in the server folder</param>
        /// <param name="cancel">The token allowing cancellation</param>
        /// <returns>true when a PKM was freed</returns>
        public Task<bool> FreePkmAsync(string pkmFileHash, CancellationToken cancel = default)
        {
            return Task.Run(() =>
            {
                string[] matchingFiles = Directory.GetFiles(PkmFolderPath, $"{pkmFileHash}.pk*");
                if (matchingFiles.Length > 0)
                {
                    string filePath = matchingFiles[0]; // Take the first match
                    File.Delete(filePath);
                    return true;
                }
                return false;
            }, cancel);
        }

        /// <summary>
        /// Gets the number of boxes in the save file.
        /// </summary>
        /// <param name="fileHash">The SHA256 hash of the PKM save file in the temp folder</param>
        /// <param name="cancel">The token allowing cancellation</param>
        /// <returns>the number boxes in the save</returns>
        public async Task<int> GetBoxesCountAsync(string fileHash, CancellationToken cancel = default)
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
                        var dataBytes = await File.ReadAllBytesAsync(p, cancel).ConfigureAwait(false);
                        string hashString = await dataBytes.HashStringAsync(cancel).ConfigureAwait(false);
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
                    var dataBytes = await File.ReadAllBytesAsync(p, cancel).ConfigureAwait(false);
                    string hashString = await dataBytes.HashStringAsync(cancel).ConfigureAwait(false);
                    box.Add(hashString);
                    File.Move(p, Path.Combine(PkmFolderPath, $"{hashString}{Path.GetExtension(p)}"));
                }
                return box;
            }

            return [];
        }
    }
}


