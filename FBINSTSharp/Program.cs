using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FBINSTSharp.Core.Parsers;
using FBINSTSharp.Core.Services;
using FBINSTSharp.Core.Interfaces;

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
                        return HandleFormat(formatArgs.ToArray());

                    case "info":
                        if (_devicePath == null)
                        {
                            Console.Error.WriteLine("fbinst: error: device not specified for info");
                            return 1;
                        }
                        return HandleInfo(new[] { _devicePath });

                    case "restore":
                        if (_devicePath == null)
                        {
                            Console.Error.WriteLine("fbinst: error: device not specified for restore");
                            return 1;
                        }
                        return HandleRestore(new[] { _devicePath });
                    case "update":
                    case "sync":
                    case "clear":
                    case "add":
                    case "add-menu":
                    case "resize":
                    case "copy":
                    case "move":
                    case "export":
                    case "remove":
                        if (_devicePath == null)
                        {
                            Console.Error.WriteLine("fbinst: error: device not specified for remove");
                            return 1;
                        }
                        var removeArgs = new List<string> { _devicePath };
                        removeArgs.AddRange(commandArgs);
                        return HandleRemove(removeArgs.ToArray());
                    case "cat":
                    case "cat-menu":
                    case "pack":
                    case "check":
                        if (_devicePath == null)
                        {
                            Console.Error.WriteLine($"fbinst: error: device not specified for {command}");
                            return 1;
                        }
                        Console.WriteLine($"{command} command - to be implemented");
                        return 0;

                    case "save":
                        return HandleSave(commandArgs.ToArray());
                    case "load":
                        return HandleLoad(commandArgs.ToArray());
                    case "create":
                        return HandleCreate(commandArgs.ToArray());

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

        #region Обработчики команд

        static int HandleInfo(string[] args)
        {
            try
            {
                if (args.Length == 0 || string.IsNullOrEmpty(args[0]))
                {
                    Console.Error.WriteLine("fbinst: error: device not specified");
                    return 1;
                }

                string devicePath = args[0];
                using var diskIo = new DiskIoService();

                try
                {
                    if (!diskIo.Open(devicePath))
                    {
                        Console.Error.WriteLine($"fbinst: error: failed to open device {devicePath}");
                        return 1;
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"fbinst: error: {ex.Message}");
                    return 1;
                }

                // Читаем MBR (сектор 0)
                byte[] mbr = diskIo.ReadSectors(0, 1);
                if (mbr == null || mbr.Length < 512)
                {
                    Console.Error.WriteLine("fbinst: error: MBR read failed");
                    return 1;
                }

                uint fbMagic = BitConverter.ToUInt32(mbr, 0x1B4);
                if (fbMagic != 0x46424246)
                {
                    Console.WriteLine("fbinst: error: fb mbr not initialized");
                    return 1;
                }

                // Читаем структуру fb_data из сектора boot_base + 1
                ushort bootBase = BitConverter.ToUInt16(mbr, 0x1B2);
                ulong fbDataSector = (ulong)(bootBase + 1);
                byte[] fbData = diskIo.ReadSectors(fbDataSector, 1);
                if (fbData == null || fbData.Length < 16)
                {
                    Console.Error.WriteLine($"fbinst: error: failed to read fb_data from sector {fbDataSector}");
                    return 1;
                }

                // --- ВЫВОД В ПОРЯДКЕ, КАК В ОРИГИНАЛЕ ---
                byte verMajor = fbData[4];
                byte verMinor = fbData[5];
                Console.WriteLine($"version: {verMajor}.{verMinor}");

                Console.WriteLine($"base boot sector: {bootBase}");

                ushort bootSize = BitConverter.ToUInt16(fbData, 0);
                Console.WriteLine($"boot code size: {bootSize}");

                ushort priSize = BitConverter.ToUInt16(fbData, 10);
                Console.WriteLine($"primary data size: {priSize}");

                uint extSize = BitConverter.ToUInt32(fbData, 12);
                Console.WriteLine($"extended data size: {extSize}");

                bool isDebug = (mbr[0x1A8] != 0);
                Console.WriteLine($"debug version: {(isDebug ? "yes" : "no")}");

                string bpbStatus;
                if (mbr[0xD] != 0)
                    bpbStatus = "copy";
                else if (mbr[0x18] != 0)
                    bpbStatus = "init";
                else
                    bpbStatus = "zero";
                Console.WriteLine($"bpb status: {bpbStatus}");

                Console.WriteLine($"format options:");

                ushort listSize = BitConverter.ToUInt16(fbData, 8);
                Console.WriteLine($"file list size: {listSize}");

                ushort listUsed = BitConverter.ToUInt16(fbData, 6);
                Console.WriteLine($"file list used: {listUsed}");

                Console.WriteLine("files:");

                // --- ПАРСИМ СПИСОК ФАЙЛОВ И СВОБОДНОЕ МЕСТО ---
                List<FbFileEntry> files = new List<FbFileEntry>();
                uint currentPos = 0;

                if (listSize > 0 && listSize < 10000 && listUsed > 0 && listUsed <= listSize)
                {
                    uint listStartSector = (uint)(bootBase + 1 + bootSize);
                    ulong listStart = (ulong)listStartSector;
                    uint listSectors = listSize;

                    ulong totalSectors = diskIo.GetTotalSectors();
                    if (listStart + listSectors <= totalSectors)
                    {
                        try
                        {
                            byte[] fileList = diskIo.ReadSectors(listStart, listSectors);
                            if (fileList != null && fileList.Length > 0)
                            {
                                int offset = 0;
                                while (offset < fileList.Length)
                                {
                                    byte size = fileList[offset];
                                    if (size == 0)
                                        break;

                                    if (offset + 14 >= fileList.Length)
                                        break;

                                    uint dataStart = BitConverter.ToUInt32(fileList, offset + 2);
                                    uint dataSize = BitConverter.ToUInt32(fileList, offset + 6);
                                    uint dataTime = BitConverter.ToUInt32(fileList, offset + 10);

                                    int nameLen = size - 12;
                                    if (nameLen <= 0 || offset + 14 + nameLen > fileList.Length)
                                        break;

                                    string name = System.Text.Encoding.ASCII.GetString(fileList, offset + 14, nameLen).TrimEnd('\0');

                                    files.Add(new FbFileEntry
                                    {
                                        Name = name,
                                        DataStart = dataStart,
                                        DataSize = dataSize,
                                        DataTime = dataTime,
                                        IsExtended = dataStart >= priSize
                                    });

                                    offset += size + 2;
                                }

                                // --- ВТОРОЙ ПРОХОД: выводим файлы и свободное место ---
                                currentPos = listStartSector + listSize;
                                uint totalPrimaryFree = 0;
                                uint totalExtendedFree = 0;

                                foreach (var file in files)
                                {
                                    if (file.DataStart > currentPos)
                                    {
                                        uint freeSize = file.DataStart - currentPos;
                                        if (currentPos < priSize)
                                        {
                                            Console.WriteLine($"  0*   0x{currentPos:x} 0x{freeSize:x}");
                                            totalPrimaryFree += freeSize;
                                        }
                                        else
                                        {
                                            Console.WriteLine($"  1*   0x{currentPos:x} 0x{freeSize:x}");
                                            totalExtendedFree += freeSize;
                                        }
                                    }

                                    string type = file.IsExtended ? "1*" : "0";
                                    DateTime time = DateTimeOffset.FromUnixTimeSeconds(file.DataTime).LocalDateTime;
                                    string timeStr = time.ToString("yyyy-MM-dd HH:mm:ss");
                                    Console.WriteLine($"  {type}  \"{file.Name}\" 0x{file.DataStart:x} {file.DataSize} ({timeStr})");

                                    uint sectorSize = file.IsExtended ? 512u : 510u;
                                    currentPos = file.DataStart + (uint)((file.DataSize + sectorSize - 1) / sectorSize);
                                }

                                if (currentPos < priSize)
                                {
                                    uint freeSize = priSize - currentPos;
                                    Console.WriteLine($"  0*   0x{currentPos:x} 0x{freeSize:x}");
                                    totalPrimaryFree += freeSize;
                                }
                                else if (currentPos < priSize + extSize)
                                {
                                    uint freeSize = (uint)(priSize + extSize - currentPos);
                                    Console.WriteLine($"  1*   0x{currentPos:x} 0x{freeSize:x}");
                                    totalExtendedFree += freeSize;
                                }

                                Console.WriteLine($"primary area free space: {totalPrimaryFree * 510}");
                                Console.WriteLine($"extended area free space: {totalExtendedFree * 512}");
                            }
                            else
                            {
                                Console.WriteLine("  (empty file list)");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"  (failed to read file list: {ex.Message})");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"  (invalid file list: extends beyond disk size)");
                    }
                }
                else
                {
                    Console.WriteLine($"  (invalid file list size: {listSize}, used: {listUsed})");
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"fbinst: error: {ex.Message}");
                return 1;
            }
        }

        private class FbFileEntry
        {
            public string Name { get; set; }
            public uint DataStart { get; set; }
            public uint DataSize { get; set; }
            public uint DataTime { get; set; }
            public bool IsExtended { get; set; }
        }

        static int HandleFormat(string[] args)
        {
            try
            {
                var options = FormatCommandParser.Parse(args);

                if (string.IsNullOrEmpty(options.DevicePath))
                {
                    Console.Error.WriteLine("fbinst: error: device not specified");
                    return 1;
                }

                using var diskIo = new DiskIoService();
                if (!diskIo.Open(options.DevicePath))
                {
                    Console.Error.WriteLine($"fbinst: error: failed to open device {options.DevicePath}");
                    return 1;
                }

                Console.WriteLine($"Formatting {options.DevicePath}...");

                if (!diskIo.IsRemovable)
                {
                    Console.Error.WriteLine($"fbinst: error: device {options.DevicePath} is not removable, aborting for safety.");
                    return 1;
                }

                if (!options.Force && !options.Raw)
                {
                    Console.Write("Are you sure you want to format this device? (y/N): ");
                    var key = Console.ReadKey();
                    Console.WriteLine();
                    if (char.ToUpper(key.KeyChar) != 'Y')
                    {
                        Console.WriteLine("Format cancelled.");
                        return 0;
                    }
                }

                // Используем FbFormatterService для полного форматирования
                var formatter = new FbFormatterService(diskIo);
                formatter.FormatAsync(options).Wait();

                Console.WriteLine("Format completed successfully.");
                return 0;
            }
            catch (AggregateException ex) when (ex.InnerException != null)
            {
                Console.Error.WriteLine($"fbinst: error: {ex.InnerException.Message}");
                if (_verbosity > 0)
                    Console.Error.WriteLine(ex.InnerException.StackTrace);
                return 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"fbinst: error: {ex.Message}");
                if (_verbosity > 0)
                    Console.Error.WriteLine(ex.StackTrace);
                return 1;
            }
        }

        static int HandleRestore(string[] args)
        {
            try
            {
                if (args.Length == 0 || string.IsNullOrEmpty(args[0]))
                {
                    Console.Error.WriteLine("fbinst: error: device not specified");
                    return 1;
                }

                string devicePath = args[0];
                using var diskIo = new DiskIoService();
                if (!diskIo.Open(devicePath))
                {
                    Console.Error.WriteLine($"fbinst: error: failed to open device {devicePath}");
                    return 1;
                }

                var restoreService = new FbRestoreService(diskIo);
                restoreService.RestoreAsync().Wait();

                Console.WriteLine("MBR restored successfully.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"fbinst: error: {ex.Message}");
                return 1;
            }
        }

        static int HandleRemove(string[] args)
        {
            try
            {
                if (args.Length < 1 || string.IsNullOrEmpty(args[0]))
                {
                    Console.Error.WriteLine("fbinst: error: file name not specified");
                    return 1;
                }

                string devicePath = args[0];
                string fileName = args.Length > 1 ? args[1] : null;

                if (string.IsNullOrEmpty(fileName))
                {
                    Console.Error.WriteLine("fbinst: error: file name not specified");
                    return 1;
                }

                using var diskIo = new DiskIoService();
                if (!diskIo.Open(devicePath))
                {
                    Console.Error.WriteLine($"fbinst: error: failed to open device {devicePath}");
                    return 1;
                }

                var fileManager = new FbFileManagerService(diskIo);
                fileManager.LoadFileListAsync().Wait();

                // Проверяем, существует ли файл
                if (!fileManager.FileExists(fileName))
                {
                    Console.Error.WriteLine($"fbinst: error: file '{fileName}' not found");
                    return 1;
                }

                // Удаляем файл
                fileManager.RemoveFile(fileName);
                fileManager.SaveFileListAsync().Wait();

                Console.WriteLine($"File '{fileName}' removed successfully.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"fbinst: error: {ex.Message}");
                return 1;
            }
        }

        static int HandleSave(string[] args)
        {
            Console.WriteLine("Save command - to be implemented");
            return 0;
        }

        static int HandleLoad(string[] args)
        {
            Console.WriteLine("Load command - to be implemented");
            return 0;
        }

        static int HandleCreate(string[] args)
        {
            Console.WriteLine("Create command - to be implemented");
            return 0;
        }

        #endregion

        #region Вспомогательные функции

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