using System.Runtime.CompilerServices;
using PKHeX_cmd.Helpers;
using PKHeX.Core;

internal static class Program
{
    private static class Cmd
    {
        public const string PkhexCmd = "pkhex-cmd";
        public const string JsonParty = "json-party";
        public const string ViewParty = "view-party";
        public const string CountParty = "count-party";
        public const string DumpParty = "dump-party";
        public const string CountBoxes = "count-boxes";
        public const string DumpBoxes = "dump-boxes";
        public const string DumpBox = "dump-box";
    }

    private enum CmdResult
    {
        UnknownCommand = -4,
        InvalidArgs = -1,
        Success = 0,
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
                case Cmd.ViewParty:
                    if (args.Length < 2)
                    {
                        Console.WriteLine($"Usage: {Cmd.PkhexCmd} {Cmd.ViewParty} <filepath>");
                        return (int)CmdResult.InvalidArgs;
                    }
                    CmdSaveUtil.ViewParty(args[1]);
                    break;
                case Cmd.JsonParty:
                    if (args.Length < 2)
                    {
                        Console.WriteLine($"Usage: {Cmd.PkhexCmd} {Cmd.JsonParty} <filepath>");
                        return (int)CmdResult.InvalidArgs;
                    }
                    CmdSaveUtil.JsonParty(args[1]);
                    break;
                case Cmd.CountParty:
                    if (args.Length < 2)
                    {
                        Console.WriteLine($"Usage: {Cmd.PkhexCmd} {Cmd.CountParty} <filepath>");
                        return (int)CmdResult.InvalidArgs;
                    }
                    return CmdSaveUtil.GetPartyCount(args[1]);
                case Cmd.DumpParty:
                    if (args.Length < 3)
                    {
                        Console.WriteLine($"Usage: {Cmd.PkhexCmd} {Cmd.DumpParty} <filepath> <output path>");
                        return (int)CmdResult.InvalidArgs;
                    }
                    CmdSaveUtil.DumpParty(args[1], args[2]);
                    break;
                case Cmd.CountBoxes:
                    if (args.Length < 2)
                    {
                        Console.WriteLine($"Usage: {Cmd.PkhexCmd} {Cmd.CountBoxes} <filepath>");
                        return (int)CmdResult.InvalidArgs;
                    }
                    return CmdSaveUtil.GetBoxCount(args[1]);
                case Cmd.DumpBoxes:
                    if (args.Length < 3)
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
        Console.WriteLine($"  {Cmd.ViewParty} <path> View the party PKM in a save file");
        Console.WriteLine($"  {Cmd.JsonParty} <path> Output the party PKM in a save file as JSON");
        Console.WriteLine($"  {Cmd.CountParty} <path> Count the number of party PKM in a save file");
        Console.WriteLine($"  {Cmd.DumpParty} <path> <output path> Dump all party PKM files from a save file to the specified folder");
        Console.WriteLine($"  {Cmd.CountBoxes} <path> Count the number of boxes in a save file");
        Console.WriteLine($"  {Cmd.DumpBoxes} <path> Dump all boxes PK files from a save file to the specified folder");
        Console.WriteLine($"  {Cmd.DumpBox} <box number> <path> Dump specific box PK files from a save file to the specified folder");
    }
}
