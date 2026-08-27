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

        static int Main(string[] args)
        {
            try
            {
                if (args.Length == 0)
                {
                    PrintHelp();
                    return 1;
                }

                // Глобальные ключи
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
                    }
                    else if (arg == "--debug" || arg == "-d")
                    {
                        // Использовать debug-версию MBR (опционально)
                    }
                }

                // Первый аргумент не ключ — это команда
                string command = args[0].ToLowerInvariant();
                string[] commandArgs = args.Skip(1).ToArray();

                switch (command)
                {
                    case "format":
                        return HandleFormat(commandArgs);
                    case "restore":
                        return HandleRestore(commandArgs);
                    case "update":
                        return HandleUpdate(commandArgs);
                    case "sync":
                        return HandleSync(commandArgs);
                    case "info":
                        return HandleInfo(commandArgs);
                    case "clear":
                        return HandleClear(commandArgs);
                    case "add":
                        return HandleAdd(commandArgs);
                    case "add-menu":
                        return HandleAddMenu(commandArgs);
                    case "resize":
                        return HandleResize(commandArgs);
                    case "copy":
                        return HandleCopy(commandArgs);
                    case "move":
                        return HandleMove(commandArgs);
                    case "export":
                        return HandleExport(commandArgs);
                    case "remove":
                        return HandleRemove(commandArgs);
                    case "cat":
                        return HandleCat(commandArgs);
                    case "cat-menu":
                        return HandleCatMenu(commandArgs);
                    case "pack":
                        return HandlePack(commandArgs);
                    case "check":
                        return HandleCheck(commandArgs);
                    case "save":
                        return HandleSave(commandArgs);
                    case "load":
                        return HandleLoad(commandArgs);
                    case "create":
                        return HandleCreate(commandArgs);
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

        #region Обработчики команд

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

                if (options.IsFat32)
                {
                    var formatter = new Fat32FormatterService(diskIo);
                    ulong startSector = 0;
                    ulong totalSectors = diskIo.GetTotalSectors();

                    Console.WriteLine($"Total sectors: {totalSectors}");
                    Console.WriteLine($"Cluster size: {(options.UnitSize > 0 ? (object)options.UnitSize : (object)"auto")}");
                    Console.WriteLine($"Align: {options.Align}");

                    formatter.FormatAsync(startSector, totalSectors, options.UnitSize, options.Align).Wait();
                    Console.WriteLine("FAT32 formatting completed successfully.");
                }
                else if (options.IsFat16)
                {
                    Console.WriteLine("FAT16 support not yet implemented.");
                    return 1;
                }
                else
                {
                    Console.Error.WriteLine("fbinst: error: no file system specified (use --fat32 or --fat16)");
                    return 1;
                }

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
            Console.WriteLine("Restore command - to be implemented");
            return 0;
        }

        static int HandleUpdate(string[] args)
        {
            Console.WriteLine("Update command - to be implemented");
            return 0;
        }

        static int HandleSync(string[] args)
        {
            Console.WriteLine("Sync command - to be implemented");
            return 0;
        }

        static int HandleInfo(string[] args)
        {
            try
            {
                // Парсим путь к устройству
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

                // Получаем информацию
                ulong totalSectors = diskIo.GetTotalSectors();
                ulong totalBytes = totalSectors * 512;

                // Форматируем размер
                string sizeStr;
                if (totalBytes >= 1024UL * 1024 * 1024 * 1024) // TB
                    sizeStr = $"{totalBytes / (1024UL * 1024 * 1024 * 1024)} TB";
                else if (totalBytes >= 1024UL * 1024 * 1024) // GB
                    sizeStr = $"{totalBytes / (1024UL * 1024 * 1024)} GB";
                else if (totalBytes >= 1024UL * 1024) // MB
                    sizeStr = $"{totalBytes / (1024UL * 1024)} MB";
                else
                    sizeStr = $"{totalBytes / 1024} KB";

                var geometry = diskIo.GetGeometry();

                Console.WriteLine($"Device: {devicePath}");
                Console.WriteLine($"Total sectors: {totalSectors}");
                Console.WriteLine($"Total size: {sizeStr}");
                Console.WriteLine($"Bytes per sector: {geometry.BytesPerSector}");
                Console.WriteLine($"Sectors per track: {geometry.SectorsPerTrack}");
                Console.WriteLine($"Tracks per cylinder: {geometry.TracksPerCylinder}");
                Console.WriteLine($"Removable: {diskIo.IsRemovable}");

                // Читаем MBR (сектор 0)
                try
                {
                    byte[] mbr = diskIo.ReadSectors(0, 1);

                    // Проверяем сигнатуру MBR
                    ushort signature = BitConverter.ToUInt16(mbr, 510);
                    Console.WriteLine($"MBR signature: 0x{signature:X4} {(signature == 0xAA55 ? "(valid)" : "(invalid)")}");

                    // Проверяем наличие fbinst-метки
                    uint fbMagic = BitConverter.ToUInt32(mbr, 0x1B4);
                    Console.WriteLine($"fbinst magic: 0x{fbMagic:X8} {(fbMagic == 0x46424246 ? "(fbinst detected)" : "")}");

                    // Если fbinst найден, читаем дополнительные данные
                    if (fbMagic == 0x46424246)
                    {
                        // Читаем основную структуру fbinst (сектор, следующий за MBR)
                        byte[] fbData = diskIo.ReadSectors(1, 1);
                        ushort bootSize = BitConverter.ToUInt16(fbData, 0);
                        ushort flags = BitConverter.ToUInt16(fbData, 2);
                        byte verMajor = fbData[4];
                        byte verMinor = fbData[5];
                        ushort listUsed = BitConverter.ToUInt16(fbData, 6);
                        ushort listSize = BitConverter.ToUInt16(fbData, 8);
                        ushort priSize = BitConverter.ToUInt16(fbData, 10);
                        uint extSize = BitConverter.ToUInt32(fbData, 12);

                        Console.WriteLine($"  fbinst version: {verMajor}.{verMinor}");
                        Console.WriteLine($"  Boot size: {bootSize} sectors");
                        Console.WriteLine($"  Flags: 0x{flags:X4}");
                        Console.WriteLine($"  List used: {listUsed} sectors");
                        Console.WriteLine($"  List size: {listSize} sectors");
                        Console.WriteLine($"  Primary data size: {priSize} sectors");
                        Console.WriteLine($"  Extended data size: {extSize} sectors");
                    }

                    // Выводим информацию о разделах
                    Console.WriteLine("\nPartition table:");
                    bool hasPartitions = false;
                    for (int i = 0; i < 4; i++)
                    {
                        int offset = 0x1BE + i * 16;
                        byte status = mbr[offset];
                        if (status == 0x00 && mbr[offset + 4] == 0x00)
                            continue;

                        hasPartitions = true;
                        uint startLba = BitConverter.ToUInt32(mbr, offset + 8);
                        uint sizeInSectors = BitConverter.ToUInt32(mbr, offset + 12);
                        byte type = mbr[offset + 4];
                        string typeName = GetPartitionTypeName(type);

                        Console.WriteLine($"  Partition {i + 1}:");
                        Console.WriteLine($"    Bootable: {(status == 0x80 ? "Yes" : "No")}");
                        Console.WriteLine($"    Type: 0x{type:X2} ({typeName})");
                        Console.WriteLine($"    Start: {startLba} sectors");
                        Console.WriteLine($"    Size: {sizeInSectors} sectors");
                    }

                    if (!hasPartitions)
                        Console.WriteLine("  No partitions found (the whole disk may be used as a superfloppy)");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to read MBR: {ex.Message}");
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"fbinst: error: {ex.Message}");
                return 1;
            }
        }

        private static string GetPartitionTypeName(byte type)
        {
            // Наиболее распространённые типы разделов
            return type switch
            {
                0x01 => "FAT12",
                0x04 => "FAT16 (DOS 3.0+)",
                0x06 => "FAT16 (DOS 3.31+)",
                0x07 => "NTFS/exFAT",
                0x0B => "FAT32 (CHS)",
                0x0C => "FAT32 (LBA)",
                0x0E => "FAT16 (LBA)",
                0x0F => "Extended (LBA)",
                0x11 => "Hidden FAT12",
                0x12 => "Compaq diagnostics",
                0x14 => "Hidden FAT16",
                0x16 => "Hidden FAT16",
                0x17 => "Hidden NTFS",
                0x1B => "Hidden FAT32 (CHS)",
                0x1C => "Hidden FAT32 (LBA)",
                0x1E => "Hidden FAT16 (LBA)",
                0x27 => "System Recovery",
                0x42 => "Windows Dynamic Disk",
                0x82 => "Linux swap",
                0x83 => "Linux",
                0x85 => "Linux extended",
                0x86 => "NTFS volume set",
                0x87 => "NTFS volume set",
                0x8E => "Linux LVM",
                0x9F => "BIOS Boot",
                0xEE => "GPT Protective MBR",
                0xEF => "EFI System Partition",
                0xFB => "VMware VMFS",
                0xFC => "VMware VMKCORE",
                _ => "Unknown",
            };
        }

        static int HandleClear(string[] args)
        {
            Console.WriteLine("Clear command - to be implemented");
            return 0;
        }

        static int HandleAdd(string[] args)
        {
            Console.WriteLine("Add command - to be implemented");
            return 0;
        }

        static int HandleAddMenu(string[] args)
        {
            Console.WriteLine("Add-menu command - to be implemented");
            return 0;
        }

        static int HandleResize(string[] args)
        {
            Console.WriteLine("Resize command - to be implemented");
            return 0;
        }

        static int HandleCopy(string[] args)
        {
            Console.WriteLine("Copy command - to be implemented");
            return 0;
        }

        static int HandleMove(string[] args)
        {
            Console.WriteLine("Move command - to be implemented");
            return 0;
        }

        static int HandleExport(string[] args)
        {
            Console.WriteLine("Export command - to be implemented");
            return 0;
        }

        static int HandleRemove(string[] args)
        {
            Console.WriteLine("Remove command - to be implemented");
            return 0;
        }

        static int HandleCat(string[] args)
        {
            Console.WriteLine("Cat command - to be implemented");
            return 0;
        }

        static int HandleCatMenu(string[] args)
        {
            Console.WriteLine("Cat-menu command - to be implemented");
            return 0;
        }

        static int HandlePack(string[] args)
        {
            Console.WriteLine("Pack command - to be implemented");
            return 0;
        }

        static int HandleCheck(string[] args)
        {
            Console.WriteLine("Check command - to be implemented");
            return 0;
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
            Console.WriteLine("Listing available disks...");
            Console.WriteLine("");

            // Перебираем возможные физические диски (0-20, как в MAX_DISKS)
            bool foundAny = false;
            for (int diskNumber = 0; diskNumber < 20; diskNumber++)
            {
                string devicePath = $@"\\.\PHYSICALDRIVE{diskNumber}";
                string displayName = $"(hd{diskNumber})";

                try
                {
                    // Пытаемся открыть устройство только для чтения, без блокировки
                    using var diskIo = new DiskIoService();
                    // Временно: используем Open, но для получения инфы без записи
                    // Можно модифицировать Open, чтобы позволять открывать в режиме ReadOnly
                    // Но пока используем существующий Open (он открывает с GENERIC_READ|GENERIC_WRITE)
                    // Для безопасности: если открыть не удаётся из-за прав, пробуем открыть для чтения
                    bool opened = false;
                    try
                    {
                        opened = diskIo.Open(devicePath);
                    }
                    catch
                    {
                        // Если не удалось открыть с записью, игнорируем
                        continue;
                    }

                    if (!opened)
                        continue;

                    foundAny = true;

                    // Получаем информацию
                    ulong totalSectors = diskIo.GetTotalSectors();
                    ulong totalBytes = totalSectors * 512;

                    // Форматируем размер
                    string sizeStr;
                    if (totalBytes >= 1024UL * 1024 * 1024 * 1024) // TB
                        sizeStr = $"{totalBytes / (1024UL * 1024 * 1024 * 1024)} TB";
                    else if (totalBytes >= 1024UL * 1024 * 1024) // GB
                        sizeStr = $"{totalBytes / (1024UL * 1024 * 1024)} GB";
                    else if (totalBytes >= 1024UL * 1024) // MB
                        sizeStr = $"{totalBytes / (1024UL * 1024)} MB";
                    else
                        sizeStr = $"{totalBytes / 1024} KB";

                    bool isRemovable = diskIo.IsRemovable;

                    // Пытаемся прочитать MBR для проверки на fbinst
                    string marker = "";
                    try
                    {
                        byte[] mbr = diskIo.ReadSectors(0, 1);
                        uint fbMagic = BitConverter.ToUInt32(mbr, 0x1B4);
                        if (fbMagic == 0x46424246)
                        {
                            marker = " [fbinst]";
                        }
                    }
                    catch
                    {
                        // Если не удалось прочитать MBR, просто игнорируем
                    }

                    Console.WriteLine($"{displayName}: {totalSectors} sectors ({sizeStr}){(isRemovable ? " [removable]" : "")}{marker}");
                }
                catch
                {
                    // Устройство не существует или недоступно
                    continue;
                }
            }

            if (!foundAny)
            {
                Console.WriteLine("No disks found.");
            }
        }

        #endregion
    }
}