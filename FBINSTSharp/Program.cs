using FBINSTSharp.Core;
using FBINSTSharp.Core.Services;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace FBINSTSharp
{
    static class Program
    {
        private static int _verbosity = 0;
        private static string _devicePath = null;

        static int Main(string[] args)
        {
            try
            {
                if (args.Length == 0)
                {
                    PrintHelp();
                    return 1;
                }

                List<string> commandArgs = new List<string>();
                string command = null;
                _devicePath = null;

                for (int i = 0; i < args.Length; i++)
                {
                    string arg = args[i];

                    if (arg == "--help" || arg == "-h")
                    {
                        PrintHelp();
                        return 0;
                    }
                    else if (arg == "--version" || arg == "-V")
                    {
                        PrintVersion();
                        return 0;
                    }
                    else if (arg == "--list" || arg == "-l")
                    {
                        ListDevices();
                        return 0;
                    }
                    else if (arg == "--verbose" || arg == "-v")
                    {
                        _verbosity++;
                        continue;
                    }
                    else if (arg == "--debug" || arg == "-d")
                    {
                        continue;
                    }

                    if (IsDevicePath(arg))
                    {
                        if (_devicePath != null)
                        {
                            Console.Error.WriteLine("fbinst: error: multiple devices specified");
                            return 1;
                        }
                        _devicePath = arg;
                    }
                    else if (command == null && !arg.StartsWith("-"))
                    {
                        command = arg.ToLowerInvariant();
                    }
                    else
                    {
                        commandArgs.Add(arg);
                    }
                }

                if (string.IsNullOrEmpty(command) && _devicePath != null)
                {
                    Console.Error.WriteLine($"fbinst: error: no command specified for device {_devicePath}");
                    PrintHelp();
                    return 1;
                }

                if (string.IsNullOrEmpty(command))
                {
                    Console.Error.WriteLine("fbinst: error: no command specified");
                    PrintHelp();
                    return 1;
                }

                if (_devicePath == null && command != "create" && command != "save" && command != "load")
                {
                    Console.Error.WriteLine($"fbinst: error: device not specified for command '{command}'");
                    PrintHelp();
                    return 1;
                }

                switch (command)
                {
                    case "format":
                        if (_devicePath == null)
                        {
                            Console.Error.WriteLine("fbinst: error: device not specified for format");
                            return 1;
                        }
                        var formatArgs = new List<string> { _devicePath };
                        formatArgs.AddRange(commandArgs);
                        return CommandHandler.HandleFormat(formatArgs.ToArray());

                    case "info":
                        if (_devicePath == null)
                        {
                            Console.Error.WriteLine("fbinst: error: device not specified for info");
                            return 1;
                        }
                        return CommandHandler.HandleInfo(new[] { _devicePath });

                    case "restore":
                        if (_devicePath == null)
                        {
                            Console.Error.WriteLine("fbinst: error: device not specified for restore");
                            return 1;
                        }
                        return CommandHandler.HandleRestore(new[] { _devicePath });

                    case "update":
                        if (_devicePath == null)
                        {
                            Console.Error.WriteLine("fbinst: error: device not specified for update");
                            return 1;
                        }
                        return CommandHandler.HandleUpdate(new[] { _devicePath });

                    case "sync":
                        if (_devicePath == null)
                        {
                            Console.Error.WriteLine("fbinst: error: device not specified for sync");
                            return 1;
                        }
                        return CommandHandler.HandleSync(new[] { _devicePath });

                    case "clear":
                        if (_devicePath == null)
                        {
                            Console.Error.WriteLine("fbinst: error: device not specified for clear");
                            return 1;
                        }
                        return CommandHandler.HandleClear(new[] { _devicePath });

                    case "add":
                        if (_devicePath == null)
                        {
                            Console.Error.WriteLine("fbinst: error: device not specified for add");
                            return 1;
                        }
                        var addArgs = new List<string> { _devicePath };
                        addArgs.AddRange(commandArgs);
                        return CommandHandler.HandleAdd(addArgs.ToArray());

                    case "remove":
                        if (_devicePath == null)
                        {
                            Console.Error.WriteLine("fbinst: error: device not specified for remove");
                            return 1;
                        }
                        var removeArgs = new List<string> { _devicePath };
                        removeArgs.AddRange(commandArgs);
                        return CommandHandler.HandleRemove(removeArgs.ToArray());

                    case "add-menu":
                        if (_devicePath == null)
                        {
                            Console.Error.WriteLine("fbinst: error: device not specified for add-menu");
                            return 1;
                        }
                        var addMenuArgs = new List<string> { _devicePath };
                        addMenuArgs.AddRange(commandArgs);
                        return CommandHandler.HandleAddMenu(addMenuArgs.ToArray());

                    case "resize":
                        if (_devicePath == null)
                        {
                            Console.Error.WriteLine("fbinst: error: device not specified for resize");
                            return 1;
                        }
                        var resizeArgs = new List<string> { _devicePath };
                        resizeArgs.AddRange(commandArgs);
                        return CommandHandler.HandleResize(resizeArgs.ToArray());

                    case "copy":
                        if (_devicePath == null)
                        {
                            Console.Error.WriteLine("fbinst: error: device not specified for copy");
                            return 1;
                        }
                        var copyArgs = new List<string> { _devicePath };
                        copyArgs.AddRange(commandArgs);
                        return CommandHandler.HandleCopy(copyArgs.ToArray());

                    case "move":
                        if (_devicePath == null)
                        {
                            Console.Error.WriteLine("fbinst: error: device not specified for move");
                            return 1;
                        }
                        var moveArgs = new List<string> { _devicePath };
                        moveArgs.AddRange(commandArgs);
                        return CommandHandler.HandleMove(moveArgs.ToArray());

                    case "export":
                        if (_devicePath == null)
                        {
                            Console.Error.WriteLine("fbinst: error: device not specified for export");
                            return 1;
                        }
                        var exportArgs = new List<string> { _devicePath };
                        exportArgs.AddRange(commandArgs);
                        return CommandHandler.HandleExport(exportArgs.ToArray());

                    case "cat":
                        if (_devicePath == null)
                        {
                            Console.Error.WriteLine("fbinst: error: device not specified for cat");
                            return 1;
                        }
                        var catArgs = new List<string> { _devicePath };
                        catArgs.AddRange(commandArgs);
                        return CommandHandler.HandleCat(catArgs.ToArray());

                    case "cat-menu":
                        if (_devicePath == null)
                        {
                            Console.Error.WriteLine("fbinst: error: device not specified for cat-menu");
                            return 1;
                        }
                        var catMenuArgs = new List<string> { _devicePath };
                        catMenuArgs.AddRange(commandArgs);
                        return CommandHandler.HandleCatMenu(catMenuArgs.ToArray());

                    case "pack":
                        if (_devicePath == null)
                        {
                            Console.Error.WriteLine("fbinst: error: device not specified for pack");
                            return 1;
                        }
                        return CommandHandler.HandlePack(new[] { _devicePath });

                    case "check":
                        if (_devicePath == null)
                        {
                            Console.Error.WriteLine("fbinst: error: device not specified for check");
                            return 1;
                        }
                        return CommandHandler.HandleCheck(new[] { _devicePath });

                    case "save":
                        if (_devicePath == null)
                        {
                            Console.Error.WriteLine("fbinst: error: device not specified for save");
                            return 1;
                        }
                        var saveArgs = new List<string> { _devicePath };
                        saveArgs.AddRange(commandArgs);
                        return CommandHandler.HandleSave(saveArgs.ToArray());

                    case "load":
                        if (_devicePath == null)
                        {
                            Console.Error.WriteLine("fbinst: error: device not specified for load");
                            return 1;
                        }
                        var loadArgs = new List<string> { _devicePath };
                        loadArgs.AddRange(commandArgs);
                        return CommandHandler.HandleLoad(loadArgs.ToArray());

                    case "create":
                        return CommandHandler.HandleCreate(commandArgs.ToArray());

                    default:
                        Console.Error.WriteLine($"fbinst: error: unknown command '{command}'");
                        PrintHelp();
                        return 1;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"fbinst: error: {ex.Message}");
                if (_verbosity > 0)
                    Console.Error.WriteLine(ex.StackTrace);
                return 1;
            }
        }

        private static bool IsDevicePath(string arg)
        {
            if (string.IsNullOrEmpty(arg))
                return false;
            if (arg.StartsWith("(hd", StringComparison.OrdinalIgnoreCase) && arg.EndsWith(")"))
                return true;
            if (arg.StartsWith(@"\\.\PHYSICALDRIVE", StringComparison.OrdinalIgnoreCase))
                return true;
            return false;
        }

        #region Глобальные функции

        static void PrintHelp()
        {
            Console.WriteLine(@"
Usage:
    fbinst [OPTIONS] DEVICE_OR_FILE COMMANDS [PARAMETERS]

Global Options:
  --help,-h           Display this message and exit
  --version,-V        Print version information and exit
  --list,-l           List all disks in system and exit
  --verbose,-v        Print verbose messages
  --debug,-d          Use the debug version of mbr

Commands:
  format              Format disk
    --raw,-r          Format with normal layout (not bootable)
    --force,-f        Force the creation of data partition
    --zip,-z          Format as USB-ZIP
    --fat16           Format data partition as FAT16
    --fat32           Format data partition as FAT32
    --align,-a        Align to cluster boundary
    --nalign,-n NUM   NAND alignment
    --unit-size,-u NUM Unit size for FAT16/FAT32 in sectors
    --base,-b NUM     Set base boot sector
    --size,-s NUM     Set size of data partition
    --primary,-p NUM  Set primary data size
    --extended,-e NUM Set extended data size
    --list-size,-l NUM Set size of file list
    --max-sectors NUM Set maximum number of sectors per read
    --chs             Force chs mode
    --archive FILE    Initialize fb using archive file
  restore             Try to restore fb mbr
  update              Update boot code
  sync                Synchronize disk information
    --copy-bpb        Copy bpb from the first partition
    --reset-bpb       Reset bpb to inital state
    --clear-bpb       Clear bpb
    --max-sectors NUM Set maximum number of sectors per read
    --chs             Force chs mode
    --zip,-z          Format as USB-ZIP
  info                Show disk information
  clear               Clear files
  add NAME [FILE]     Add/update file item
    --extended,-e     Store the file in extended data area
    --syslinux,-s     Patch syslinux boot file
  add-menu NAME FILE  Add/update menu file
    --append,-a       Append to existing menu file
    --string,-s       The menu items are passed as command argument
  resize NAME SIZE    Resize/create file item
    --extended,-e     Store the file in extended data area
    --fill,-f NUM     Set fill character for expansion
  copy OLD NEW        Copy file item
  move OLD NEW        Move file item
  export NAME [FILE]  Export file item
  remove NAME         Remove file item
  cat NAME            Show the content of text file
  cat-menu NAME       Show the content of menu file
  pack                Pack free space
  check               Check primary data area for inconsistency
  save FILE           Save to archive file
    --list-size,-l NUM Set size of file list
  load FILE           Load from archive file
  create              Create archive file
    --primary,-p NUM  Set primary data size
    --extended,-e NUM Set extended data size
    --list-size,-l NUM Set size of file list
");
        }

        static void PrintVersion()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            Console.WriteLine($"fbinst version {version?.ToString() ?? "1.0"} (FBinstSharp)");
            Console.WriteLine("Copyright (C) 2009 Bean (original fbinst)");
            Console.WriteLine("C# port and enhancements (c) 2025");
            Console.WriteLine("This is free software; see the source for copying conditions.");
            Console.WriteLine("There is NO warranty; not even for MERCHANTABILITY or");
            Console.WriteLine("FITNESS FOR A PARTICULAR PURPOSE.");
        }

        static void ListDevices()
        {
            for (int diskNumber = 0; diskNumber < 20; diskNumber++)
            {
                string devicePath = $@"\\.\PHYSICALDRIVE{diskNumber}";
                string displayName = $"(hd{diskNumber})";

                try
                {
                    using var diskIo = new DiskIoService();
                    if (!diskIo.Open(devicePath, readOnly: true))
                        continue;

                    ulong totalSectors = diskIo.GetTotalSectors();
                    ulong sizeInGB;

                    if (totalSectors >= (3UL << 20))
                    {
                        sizeInGB = (totalSectors + (1UL << 20)) >> 21;
                    }
                    else
                    {
                        sizeInGB = (totalSectors + (1UL << 10)) >> 11;
                    }

                    string marker = "";
                    try
                    {
                        byte[] mbr = diskIo.ReadSectors(0, 1);
                        uint fbMagic = BitConverter.ToUInt32(mbr, 0x1B4);
                        if (fbMagic == 0x46424246)
                            marker = " *";
                    }
                    catch { }

                    Console.WriteLine($"{displayName}: {totalSectors} ({sizeInGB}g){marker}");
                }
                catch
                {
                    continue;
                }
            }
        }

        #endregion
    }
}