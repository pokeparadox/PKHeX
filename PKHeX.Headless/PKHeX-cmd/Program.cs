using System.Runtime.CompilerServices;
using PKHeX_cmd.Helpers;
using PKHeX.Core;

internal static class Program
{
    private static class Cmd
    {
        public const string PkhexCmd = "pkhex-cmd";
        public const string CountBoxes = "count-boxes";
        public const string DumpBoxes = "dump-boxes";
        public const string DumpBox = "dump-box";
    }

    public enum CmdResult
    {
        Success = 0,
        InvalidArgs = 1,
        UnknownCommand = 4
    }

    static int Main(string[] args)
    {
        try
        {
            if (args.Length == 0)
            {
                PrintUsage();
                return  (int)CmdResult.InvalidArgs;
            }

            switch (args[0])
            {
                case Cmd.CountBoxes:
                    if (args.Length < 2)
                    {
                        Console.WriteLine($"Usage: {Cmd.PkhexCmd} {Cmd.CountBoxes} <filepath>");
                        return (int)CmdResult.InvalidArgs;
                    }

                    return CmdSaveUtil.GetBoxCount(args[1]);
                case Cmd.DumpBoxes:
                    if (args.Length < 2)
                    {
                        Console.WriteLine($"Usage: {Cmd.PkhexCmd} {Cmd.DumpBoxes} <filepath> <output path>");
                        return (int)CmdResult.InvalidArgs;
                    }
                    CmdSaveUtil.DumpBoxes(args[1], args[2]);
                    break;
                case Cmd.DumpBox:
                    if (args.Length < 3)
                    {
                        Console.WriteLine($"Usage: {Cmd.PkhexCmd} {Cmd.DumpBox} <box number> <path>");
                        return (int)CmdResult.InvalidArgs;
                    }
                    if(int.TryParse(args[2], out var boxNum))
                    {
                        Console.WriteLine($"Invalid box number: {args[2]}");
                        return (int)CmdResult.InvalidArgs;
                    }
                    CmdSaveUtil.DumpBox(args[1],boxNum, args[3]);
                    break;

                default:
                    Console.WriteLine("Unknown command");
                    PrintUsage();
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return (int)CmdResult.UnknownCommand;
        }

        return (int)CmdResult.Success;
    }

    static void PrintUsage()
    {
        Console.WriteLine($"Usage: {Cmd.PkhexCmd} <command> [options]");
        Console.WriteLine("Commands:");
        Console.WriteLine($"  {Cmd.CountBoxes} <path> Count the number of boxes in a save file");
        Console.WriteLine($"  {Cmd.DumpBoxes} <path> Dump all boxes PK files from a save file to the specified folder");
        Console.WriteLine($"  {Cmd.DumpBox} <box number> <path> Dump specific box PK files from a save file to the specified folder");
    }

    static void LoadSaveFile(string path)
    {
        CmdSaveUtil.LoadSaveFile(path);
        if (!SaveUtil.TryGetSaveFile(path, out var sav))
        {
            Console.WriteLine("Failed to load save file");
            return;
        }

        Console.WriteLine($"Loaded {sav.Version} save");
        Console.WriteLine($"Trainer: {sav.OT}");
        Console.WriteLine($"TID: {sav.TID16}");
        Console.WriteLine($"SID: {sav.SID16}");
        Console.WriteLine($"Playtime: {sav.PlayTimeString}");
        Console.WriteLine($"Checksums valid: {sav.ChecksumsValid}");
    }

    static void CheckPKM(string path)
    {
        var obj = FileUtil.GetSupportedFile(path);
        if (obj is not PKM pk)
        {
            Console.WriteLine("Failed to load PKM file");
            return;
        }

        var la = new LegalityAnalysis(pk);
        Console.WriteLine($"PKM: {pk.Nickname} ({pk.Species})");
        Console.WriteLine($"Valid: {la.Valid}");
        foreach (var result in la.Results)
        {
            Console.WriteLine($"{result.Judgement}: {result.Result}");
        }
    }
}
