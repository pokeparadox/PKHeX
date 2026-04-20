using PKHeX.Core;
using Facet.Extensions;
using PKHeX.Rest.Extensions;
using PKHeX.Rest.Facets;

namespace PKHeX.Rest.Services
{
    public class SaveFileService
    {
        // The intention is that backups of PKM and Saves will always be taken and stored in the backup folders.
        // The temp folder holds the current saves box organisation.
        // There is a permanent general PKM folder for a user to manage.
        // There is a permanent save folder for the user to manage.
        private static readonly string TempFolderPath = Path.Combine(Path.GetTempPath(), "pkhex_rest");
        private static readonly string BoxFolderPath = Path.Combine(TempFolderPath, "boxes");
        private static readonly string BaseDataFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "data");
        private static readonly string BaseBackupFolderPath = Path.Combine(BaseDataFolderPath, "backup");
        private static readonly string SavesFolderPath = Path.Combine(BaseDataFolderPath, "saves");
        private static readonly string SavesBackupFolderPath = Path.Combine(BaseBackupFolderPath, "saves");
        private static readonly string PkmFolderPath = Path.Combine(BaseDataFolderPath, "pkm");
        private static readonly string PkmBackupFolderPath = Path.Combine(BaseBackupFolderPath, "pkm");

        /// <summary>
        /// Represents the possible locations where a PKM can reside
        /// </summary>
        public enum PkmLocation
        {
            ServerFolder,  // General server PKM folder
            Party,         // Party data in save file
            Box            // Box data in save file
        }

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
                string fileName = $"{hashString}.sav";
                var saveFile = Path.Combine(TempFolderPath, fileName);
                // Write the file to the saves directory
                await File.WriteAllBytesAsync(saveFile, fileBytes, cancel).ConfigureAwait(false);

                // Copy the save to the backup save folder if it doesn't exist there already
                MoveWithBackup(saveFile, Path.Combine(SavesFolderPath, fileName), Path.Combine(SavesBackupFolderPath, fileName));
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
                    // Create a buffer for the party data and write the PKM to it
                    byte[] partyData = new byte[p.SIZE_PARTY];
                    p.WriteDecryptedDataParty(partyData);
                    string hashString = await partyData.HashStringAsync(cancel).ConfigureAwait(false);
                    dumpedFiles.Add(hashString);
                    string fName = $"{hashString}{Path.GetExtension(fileName)}";
                    string outputPath = Path.Combine(PkmFolderPath, fName);
                    if (File.Exists(outputPath))
                    {
                        continue;
                    }
                    await File.WriteAllBytesAsync(filePath, partyData, cancel).ConfigureAwait(false);

                    // Rename the file to use the file hash instead
                    MoveWithBackup(filePath, outputPath, Path.Combine(PkmBackupFolderPath, fName));
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
                if (FileUtil.TryGetPKM(fileData, out PKM? _, Path.GetExtension(filePath), save))
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
            if (FileUtil.TryGetPKM(pkmFileData, out PKM? _, Path.GetExtension(fileName)))
            {
                var tempFile = Path.Combine(TempFolderPath, fileName);
                await File.WriteAllBytesAsync(tempFile, pkmFileData, cancel).ConfigureAwait(false);
                string hashString = await pkmFileData.ToArray().HashStringAsync(cancel).ConfigureAwait(false);
                // Move the hashed file from temp to PKM folder
                string fName = $"{hashString}{Path.GetExtension(fileName)}";
                MoveWithBackup(tempFile, Path.Combine(PkmFolderPath, fName), Path.Combine(PkmBackupFolderPath, fName));
                return hashString;
            }

            return string.Empty;
        }

        /// <summary>
        /// Moves a PKM from one location to another, identified by its file hash.
        /// </summary>
        /// <param name="pkmFileHash">The SHA256 hash of the PKM file</param>
        /// <param name="saveFileHash">The SHA256 hash of the save file (required for party/box operations)</param>
        /// <param name="sourceLocation">Where the PKM is currently located</param>
        /// <param name="destinationLocation">Where to move the PKM</param>
        /// <param name="destinationIndex">For party/box destinations, the index position to place the PKM</param>
        /// <param name="destinationBoxIndex">For box destinations, which box to move to</param>
        /// <param name="withCopy">If true the PKM is left in the source location</param>
        /// <param name="cancel">The token allowing cancellation</param>
        /// <returns>true if the move was successful</returns>
        public async Task<bool> MovePkmAsync(
            string pkmFileHash,
            string saveFileHash,
            PkmLocation sourceLocation,
            PkmLocation destinationLocation,
            int destinationIndex = 0,
            int destinationBoxIndex = 0,
            bool withCopy = false,
            CancellationToken cancel = default)
        {
            // Retrieve the PKM from source location
            var (pkmFound, pkmData) = await GetPkmAsync(pkmFileHash, saveFileHash, cancel).ConfigureAwait(false);
            if (!pkmFound || pkmData?.FileData == null)
                return false;

            // Retrieve the save file
            var (saveFound, save) = await TryRetrieveSaveFileAsync(saveFileHash, cancel).ConfigureAwait(false);
            if (!saveFound || save == null)
                return false;

            // Load the PKM object
            if (!FileUtil.TryGetPKM(pkmData.FileData, out PKM? pkm, Path.GetExtension(pkmData.FileName)))
                return false;

            if (!withCopy)
            {
                await FreePkmAsync(pkmFileHash, cancel).ConfigureAwait(false);
                // We also need to remove from the source of the save file if it's party or box
                switch (sourceLocation)
                {
                    case PkmLocation.Party:
                        if (save.HasParty)
                        {
                            for (int i = 0; i < save.PartyCount; ++i)
                            {
                                var chkPk = save.GetPartySlotAtIndex(i);
                                if(chkPk?.FileName == pkm.FileName)
                                {
                                    save.SetPartySlotAtIndex(save.BlankPKM, i);
                                    break;
                                }
                            }
                        }

                        break;
                    case PkmLocation.Box:
                        if (save.BoxData?.Any() == true)
                        {
                            for (int b = 0; b < save.BoxCount; ++b)
                            {
                                var box = save.GetBoxData(b);
                                if (box?.Any() == true)
                                {
                                    for (int s = 0; s < save.BoxSlotCount; ++s)
                                    {
                                        var boxPkm = box[s];
                                        if (boxPkm != save.BlankPKM && boxPkm.FileName == pkm.FileName)
                                        {
                                            box[s] = save.BlankPKM;
                                            save.SetBoxData(box, b);
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        break;
                }
            }

            // Handle destination operations
            switch (destinationLocation)
            {
                case PkmLocation.Party:
                    if (destinationIndex >= 0 && destinationIndex < save.PartyCount)
                    {
                        save.PartyData[destinationIndex] = pkm;
                        return true;
                    }
                    break;

                case PkmLocation.Box:
                    if (destinationBoxIndex >= 0 && destinationBoxIndex < save.BoxCount)
                    {
                        var box = save.GetBoxData(destinationBoxIndex);
                        if (box?.Any() == true)
                        {
                            box[destinationIndex] = pkm;
                            save.SetBoxData(box, destinationBoxIndex);
                            return true;
                        }
                    }
                    break;

                case PkmLocation.ServerFolder:
                    // Get ext of pkm file
                    string ext = pkm.Extension;
                    string fileName = $"{pkmFileHash}{ext}";
                    // Write file to temp
                    byte[] partyData = new byte[pkm.SIZE_PARTY];
                    pkm.WriteDecryptedDataParty(partyData);
                    string hashString = await partyData.HashStringAsync(cancel).ConfigureAwait(false);
                    await File.WriteAllBytesAsync(Path.Combine(TempFolderPath, fileName), partyData, cancel).ConfigureAwait(false);
                    MoveWithBackup(Path.Combine(TempFolderPath, fileName), Path.Combine(PkmFolderPath, fileName), Path.Combine(PkmBackupFolderPath, fileName));
                    return true;
            }

            return false;
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
                        string fileName = $"{hashString}{Path.GetExtension(p)}";
                        MoveWithBackup(p, Path.Combine(PkmFolderPath, fileName), Path.Combine(PkmBackupFolderPath, fileName));
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
                    string fileName = $"{hashString}{Path.GetExtension(p)}";
                    MoveWithBackup(p, Path.Combine(PkmFolderPath, fileName), Path.Combine(PkmBackupFolderPath, fileName));
                }
                return box;
            }

            return [];
        }

        private static void MoveWithBackup(string sourceFilePath, string destinationFilePath, string backupFilePath)
        {
            // Rename the file to use the file hash instead
            File.Move(sourceFilePath, destinationFilePath);
            // Copy the PKM to the backup PKM folder if it doesn't already exist
            if (!File.Exists(backupFilePath))
            {
                File.Copy(destinationFilePath, backupFilePath);
            }
        }
    }
}


