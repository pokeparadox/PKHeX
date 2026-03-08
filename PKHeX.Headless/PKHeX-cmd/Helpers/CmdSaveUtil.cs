using System.Text;
using PKHeX.Core;

namespace PKHeX_cmd.Helpers
{
    public static class CmdSaveUtil
    {
        private static SaveFile? _currentSave;
        private const string DefaultDbPath = "pkmdb";

        private static bool LoadSaveFile(string filePath)
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
        /// Gets the number of party PKM in the save file.
        /// <param name="filePath">The path to the save file.</param>
        /// <returns>The count of the number of PKM or -1 if the save file could not be loaded or if we could not read the number of PKM in the party</returns>
        /// </summary>
        public static int GetPartyCount(string filePath)
        {
            int count = -1;
            if (LoadSaveFile(filePath))
            {
                count =  _currentSave?.PartyCount ?? -1;
            }

            if(count != -1)
            {
                Console.WriteLine($"The save file {filePath} contains {count} party PKM.");
            }
            else
            {
                Console.WriteLine($"Failed to get party count from save file: {filePath}");
            }
            return count;
        }

        /// <summary>
        /// Loads the save file and prints out all the PK files from the party.
        /// </summary>
        /// <param name="filePath">The path to the save file to load.</param>
        public static void ViewParty(string filePath)
        {
            if (LoadSaveFile(filePath) && _currentSave?.HasParty == true)
            {
                int printed = 0;
                var sb = new StringBuilder();
                foreach(var p in _currentSave.PartyData)
                {
                    sb.AppendLine($"PKM Party Slot {printed + 1}:")
                        .AppendLine($" PKM: {p.FileNameWithoutExtension} Lvl:{p.CurrentLevel}")
                        .AppendLine($" EXP: {p.EXP}")
                        .AppendLine($" EVS( HP:{p.EV_HP} ATK:{p.EV_ATK} DEF:{p.EV_DEF} SPA:{p.EV_SPA} SPD:{p.EV_SPD} SPE:{p.EV_SPE})")
                        .AppendLine($" Trainer: {p.OriginalTrainerName} (ID: {p.ID32})")
                        .AppendLine("-----------------------------")
                        ;

                    printed++;
                }

                if (printed > 0)
                {
                    Console.WriteLine(sb.ToString());
                }
                else
                {
                    Console.WriteLine($"Failed to view PKM from {filePath}");
                }
            }
        }

        /// <summary>
        /// Loads the save file and dumps out all the PK files from the party into the database folder.
        /// </summary>
        /// <param name="filePath">The path to the save file to load.</param>
        /// <param name="outPath">The output directory path for the dumped PK files. Defaults to "pkmdb".</param>
        public static void DumpParty(string filePath, string outPath = DefaultDbPath)
        {
            if (LoadSaveFile(filePath) && _currentSave?.HasParty == true)
            {
                int dumpedCount = 0;
                foreach(var p in _currentSave.PartyData)
                {
                    var fileName = PathUtil.CleanFileName(p.FileName);
                    var fn = Path.Combine(outPath, fileName);
                    if (File.Exists(fn))
                        continue;

                    File.WriteAllBytes(fn, p.DecryptedPartyData);
                    Console.WriteLine($"Dumped PKM: {fn}");
                    dumpedCount++;
                }
                if (dumpedCount > 0)
                {
                    Console.WriteLine($"Dumped {dumpedCount} PK file{(dumpedCount > 1? "s" : string.Empty)} from  party {filePath} to {outPath}");
                }
                else
                {
                    Console.WriteLine($"Failed to dump PK files from {filePath} to {outPath}");
                }
            }
        }

        /// <summary>
        /// Gets the number of boxes in the save file.
        /// <param name="filePath">The path to the save file.</param>
        /// <returns>The count of the number of boxes or -1 if the save file could not be loaded or if we could not read the number of boxes</returns>
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
