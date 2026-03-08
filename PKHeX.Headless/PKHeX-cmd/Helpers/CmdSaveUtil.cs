using PKHeX.Core;

namespace PKHeX_cmd.Helpers
{
    public static class CmdSaveUtil
    {
        private static SaveFile? _currentSave;
        private const string DefaultDbPath = "pkmdb";

        public static bool LoadSaveFile(string filePath)
        {
            if (SaveUtil.TryGetSaveFile(filePath, out SaveFile? save))
            {
                _currentSave = save;
                Console.WriteLine($"Successfully loaded save file: {filePath}");
                return true;
            }
            else
            {
                Console.WriteLine($"Failed to load save file: {filePath}");
                return false;
            }
        }

        /// <summary>
        /// Gets the number of boxes in the save file.
        /// <param name="filePath">The path to the save file.</param>
        /// <returns>The count of the number of boxes or -1 if the save file could not be loaded or if we could not read the numbe of boxes</returns>
        /// </summary>
        public static int GetBoxCount(string filePath)
        {
            int count = -1;
            if (LoadSaveFile(filePath))
            {
                count =  _currentSave?.BoxCount ?? -1;
            }

            if(count != -1)
            {
                Console.WriteLine($"The save file {filePath} contains {count} box{(count > 1 ? "es" : string.Empty)}.");
            }
            else
            {
                Console.WriteLine($"Failed to get box count from save file: {filePath}");
            }
            return count;
        }

        /// <summary>
        /// Loads the save file and dumps out all the PK files from the save into the database folder.
        /// </summary>
        /// <param name="filePath">The path to the save file to load.</param>
        /// <param name="outPath">The output directory path for the dumped PK files. Defaults to "pkmdb".</param>
        /// <param name="withBoxes">Whether to include box information when dumping. Defaults to false.</param>
        public static void DumpBoxes(string filePath, string outPath = DefaultDbPath, bool withBoxes = false)
        {
            if (LoadSaveFile(filePath))
            {
                var dumped = _currentSave?.DumpBoxes(outPath, withBoxes) ?? -1;
                if (dumped != -1)
                {
                    Console.WriteLine(
                        $"Dumped {dumped} PK file{(dumped > 1 ? "s" : string.Empty)} from {filePath} to {outPath}");
                }
                else
                {
                    Console.WriteLine($"Failed to dump PK files from {filePath} to {outPath}");
                }
            }
        }

        /// <summary>
        /// Loads the save file and dumps out all the PK files from the save into the database folder.
        /// </summary>
        /// <param name="filePath">The path to the save file to load.</param>
        /// <param name="currentBox">The pkmn box from which to dump the pk files</param>
        /// <param name="outPath">The output directory path for the dumped PK files. Defaults to "pkmdb".</param>
        public static void DumpBox(string filePath, int currentBox = 1, string outPath = DefaultDbPath)
        {
            if (LoadSaveFile(filePath))
            {
                var dumped = _currentSave?.DumpBox(outPath, currentBox) ?? -1;
                if (dumped != -1)
                {
                    Console.WriteLine($"Dumped {dumped} PK file{(dumped > 1? "s" : string.Empty)} from  box #{currentBox} {filePath} to {outPath}");
                }
                else
                {
                    Console.WriteLine($"Failed to dump PK files from {filePath} to {outPath}");
                }
            }
        }
    }
}
