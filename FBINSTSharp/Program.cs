using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.IO;
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
                        if (_devicePath == null)
                        {
                            Console.Error.WriteLine("fbinst: error: device not specified for update");
                            return 1;
                        }
                        return HandleUpdate(new[] { _devicePath });

                    case "sync":
                        if (_devicePath == null)
                        {
                            Console.Error.WriteLine("fbinst: error: device not specified for sync");
                            return 1;
                        }
                        return HandleSync(new[] { _devicePath });

                    case "clear":
                        if (_devicePath == null)
                        {
                            Console.Error.WriteLine("fbinst: error: device not specified for clear");
                            return 1;
                        }
                        return HandleClear(new[] { _devicePath });

                    case "add":
                        if (_devicePath == null)
                        {
                            Console.Error.WriteLine("fbinst: error: device not specified for add");
                            return 1;
                        }
                        var addArgs = new List<string> { _devicePath };
                        addArgs.AddRange(commandArgs);
                        return HandleAdd(addArgs.ToArray());

                    case "remove":
                        if (_devicePath == null)
                        {
                            Console.Error.WriteLine("fbinst: error: device not specified for remove");
                            return 1;
                        }
                        var removeArgs = new List<string> { _devicePath };
                        removeArgs.AddRange(commandArgs);
                        return HandleRemove(removeArgs.ToArray());

                    case "add-menu":
                        if (_devicePath == null)
                        {
                            Console.Error.WriteLine("fbinst: error: device not specified for add-menu");
                            return 1;
                        }
                        var addMenuArgs = new List<string> { _devicePath };
                        addMenuArgs.AddRange(commandArgs);
                        return HandleAddMenu(addMenuArgs.ToArray());
                    case "resize":
                        if (_devicePath == null)
                        {
                            Console.Error.WriteLine("fbinst: error: device not specified for resize");
                            return 1;
                        }
                        var resizeArgs = new List<string> { _devicePath };
                        resizeArgs.AddRange(commandArgs);
                        return HandleResize(resizeArgs.ToArray());
                    case "copy":
                        if (_devicePath == null)
                        {
                            Console.Error.WriteLine("fbinst: error: device not specified for copy");
                            return 1;
                        }
                        var copyArgs = new List<string> { _devicePath };
                        copyArgs.AddRange(commandArgs);
                        return HandleCopy(copyArgs.ToArray());
                    case "move":
                        if (_devicePath == null)
                        {
                            Console.Error.WriteLine("fbinst: error: device not specified for move");
                            return 1;
                        }
                        var moveArgs = new List<string> { _devicePath };
                        moveArgs.AddRange(commandArgs);
                        return HandleMove(moveArgs.ToArray());
                    case "export":
                        if (_devicePath == null)
                        {
                            Console.Error.WriteLine("fbinst: error: device not specified for export");
                            return 1;
                        }
                        var exportArgs = new List<string> { _devicePath };
                        exportArgs.AddRange(commandArgs);
                        return HandleExport(exportArgs.ToArray());
                    case "cat":
                        if (_devicePath == null)
                        {
                            Console.Error.WriteLine("fbinst: error: device not specified for cat");
                            return 1;
                        }
                        var catArgs = new List<string> { _devicePath };
                        catArgs.AddRange(commandArgs);
                        return HandleCat(catArgs.ToArray());
                    case "cat-menu":
                        if (_devicePath == null)
                        {
                            Console.Error.WriteLine("fbinst: error: device not specified for cat-menu");
                            return 1;
                        }
                        var catMenuArgs = new List<string> { _devicePath };
                        catMenuArgs.AddRange(commandArgs);
                        return HandleCatMenu(catMenuArgs.ToArray());
                    case "pack":
                        if (_devicePath == null)
                        {
                            Console.Error.WriteLine("fbinst: error: device not specified for pack");
                            return 1;
                        }
                        return HandlePack(new[] { _devicePath });
                    case "check":
                        if (_devicePath == null)
                        {
                            Console.Error.WriteLine("fbinst: error: device not specified for check");
                            return 1;
                        }
                        return HandleCheck(new[] { _devicePath });

                    case "save":
                        if (_devicePath == null)
                        {
                            Console.Error.WriteLine("fbinst: error: device not specified for save");
                            return 1;
                        }
                        var saveArgs = new List<string> { _devicePath };
                        saveArgs.AddRange(commandArgs);
                        return HandleSave(saveArgs.ToArray());
                    case "load":
                        if (_devicePath == null)
                        {
                            Console.Error.WriteLine("fbinst: error: device not specified for load");
                            return 1;
                        }
                        var loadArgs = new List<string> { _devicePath };
                        loadArgs.AddRange(commandArgs);
                        return HandleLoad(loadArgs.ToArray());
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

        static int HandleUpdate(string[] args)
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

                var updateService = new FbUpdateService(diskIo);
                updateService.UpdateAsync().Wait();

                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"fbinst: error: {ex.Message}");
                return 1;
            }
        }

        static int HandleSync(string[] args)
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

                var syncService = new FbSyncService(diskIo);
                syncService.SyncAsync().Wait();

                Console.WriteLine("Sync completed successfully.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"fbinst: error: {ex.Message}");
                return 1;
            }
        }

        static int HandleClear(string[] args)
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

                var fileManager = new FbFileManagerService(diskIo);
                fileManager.LoadFileListAsync().Wait();
                fileManager.ClearFiles();
                fileManager.SaveFileListAsync().Wait();

                Console.WriteLine("All files cleared successfully.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"fbinst: error: {ex.Message}");
                return 1;
            }
        }

        static int HandleAdd(string[] args)
        {
            try
            {
                if (args.Length < 2 || string.IsNullOrEmpty(args[0]) || string.IsNullOrEmpty(args[1]))
                {
                    Console.Error.WriteLine("fbinst: error: not enough parameters for add");
                    Console.Error.WriteLine("Usage: fbinst DEVICE add NAME [FILE]");
                    return 1;
                }

                string devicePath = args[0];
                string fileName = args[1];
                string sourcePath = args.Length > 2 ? args[2] : null;

                // Определяем, нужно ли сохранять в extended область
                bool extended = false;
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i] == "--extended" || args[i] == "-e")
                    {
                        extended = true;
                        break;
                    }
                }

                using var diskIo = new DiskIoService();
                if (!diskIo.Open(devicePath))
                {
                    Console.Error.WriteLine($"fbinst: error: failed to open device {devicePath}");
                    return 1;
                }

                var fileManager = new FbFileManagerService(diskIo);
                fileManager.LoadFileListAsync().Wait();

                if (string.IsNullOrEmpty(sourcePath))
                {
                    Console.Error.WriteLine("fbinst: error: source file not specified");
                    return 1;
                }

                fileManager.AddFileAsync(fileName, sourcePath, extended).Wait();
                Console.WriteLine($"File '{fileName}' added successfully.");

                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"fbinst: error: {ex.Message}");
                return 1;
            }
        }

        static int HandleExport(string[] args)
        {
            try
            {
                if (args.Length < 2 || string.IsNullOrEmpty(args[0]) || string.IsNullOrEmpty(args[1]))
                {
                    Console.Error.WriteLine("fbinst: error: not enough parameters for export");
                    Console.Error.WriteLine("Usage: fbinst DEVICE export NAME [FILE]");
                    return 1;
                }

                string devicePath = args[0];
                string fileName = args[1];
                string outputPath = args.Length > 2 ? args[2] : null;

                using var diskIo = new DiskIoService();
                if (!diskIo.Open(devicePath))
                {
                    Console.Error.WriteLine($"fbinst: error: failed to open device {devicePath}");
                    return 1;
                }

                var fileManager = new FbFileManagerService(diskIo);
                fileManager.LoadFileListAsync().Wait();

                if (!fileManager.FileExists(fileName))
                {
                    Console.Error.WriteLine($"fbinst: error: file '{fileName}' not found");
                    return 1;
                }

                var entry = fileManager.GetFileEntry(fileName);
                byte[] fileData = fileManager.ReadFileData(entry);

                if (string.IsNullOrEmpty(outputPath))
                {
                    // Если выходной файл не указан — выводим в stdout
                    Console.OutputEncoding = System.Text.Encoding.UTF8;
                    Console.Write(System.Text.Encoding.UTF8.GetString(fileData));
                }
                else
                {
                    File.WriteAllBytes(outputPath, fileData);
                    Console.WriteLine($"File '{fileName}' exported to '{outputPath}' successfully.");
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"fbinst: error: {ex.Message}");
                return 1;
            }
        }

        static int HandleCopy(string[] args)
        {
            try
            {
                if (args.Length < 3 || string.IsNullOrEmpty(args[0]) || string.IsNullOrEmpty(args[1]) || string.IsNullOrEmpty(args[2]))
                {
                    Console.Error.WriteLine("fbinst: error: not enough parameters for copy");
                    Console.Error.WriteLine("Usage: fbinst DEVICE copy OLD_NAME NEW_NAME");
                    return 1;
                }

                string devicePath = args[0];
                string oldName = args[1];
                string newName = args[2];

                using var diskIo = new DiskIoService();
                if (!diskIo.Open(devicePath))
                {
                    Console.Error.WriteLine($"fbinst: error: failed to open device {devicePath}");
                    return 1;
                }

                var fileManager = new FbFileManagerService(diskIo);
                fileManager.LoadFileListAsync().Wait();

                if (!fileManager.FileExists(oldName))
                {
                    Console.Error.WriteLine($"fbinst: error: file '{oldName}' not found");
                    return 1;
                }

                if (fileManager.FileExists(newName))
                {
                    Console.Error.WriteLine($"fbinst: error: file '{newName}' already exists");
                    return 1;
                }

                var sourceEntry = fileManager.GetFileEntry(oldName);
                byte[] fileData = fileManager.ReadFileData(sourceEntry);

                // Добавляем новый файл с теми же данными
                fileManager.AddFileFromDataAsync(newName, fileData, sourceEntry.IsExtended).Wait();

                Console.WriteLine($"File '{oldName}' copied to '{newName}' successfully.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"fbinst: error: {ex.Message}");
                return 1;
            }
        }

        static int HandleMove(string[] args)
        {
            try
            {
                if (args.Length < 3 || string.IsNullOrEmpty(args[0]) || string.IsNullOrEmpty(args[1]) || string.IsNullOrEmpty(args[2]))
                {
                    Console.Error.WriteLine("fbinst: error: not enough parameters for move");
                    Console.Error.WriteLine("Usage: fbinst DEVICE move OLD_NAME NEW_NAME");
                    return 1;
                }

                string devicePath = args[0];
                string oldName = args[1];
                string newName = args[2];

                using var diskIo = new DiskIoService();
                if (!diskIo.Open(devicePath))
                {
                    Console.Error.WriteLine($"fbinst: error: failed to open device {devicePath}");
                    return 1;
                }

                var fileManager = new FbFileManagerService(diskIo);
                fileManager.LoadFileListAsync().Wait();

                if (!fileManager.FileExists(oldName))
                {
                    Console.Error.WriteLine($"fbinst: error: file '{oldName}' not found");
                    return 1;
                }

                if (fileManager.FileExists(newName))
                {
                    Console.Error.WriteLine($"fbinst: error: file '{newName}' already exists");
                    return 1;
                }

                var sourceEntry = fileManager.GetFileEntry(oldName);
                byte[] fileData = fileManager.ReadFileData(sourceEntry);

                // Удаляем исходный файл
                fileManager.RemoveFile(oldName);

                // Добавляем новый файл с теми же данными
                fileManager.AddFileFromDataAsync(newName, fileData, sourceEntry.IsExtended).Wait();

                Console.WriteLine($"File '{oldName}' moved to '{newName}' successfully.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"fbinst: error: {ex.Message}");
                return 1;
            }
        }

        static int HandleResize(string[] args)
        {
            try
            {
                if (args.Length < 3 || string.IsNullOrEmpty(args[0]) || string.IsNullOrEmpty(args[1]) || string.IsNullOrEmpty(args[2]))
                {
                    Console.Error.WriteLine("fbinst: error: not enough parameters for resize");
                    Console.Error.WriteLine("Usage: fbinst DEVICE resize NAME SIZE [--fill NUM]");
                    return 1;
                }

                string devicePath = args[0];
                string fileName = args[1];
                uint newSize = 0;
                byte fillByte = 0;
                bool extended = false;

                // Парсим аргументы
                for (int i = 2; i < args.Length; i++)
                {
                    if (args[i] == "--fill" || args[i] == "-f")
                    {
                        if (i + 1 < args.Length)
                        {
                            if (args[i + 1].Length == 1)
                                fillByte = (byte)args[i + 1][0];
                            else
                                fillByte = byte.Parse(args[i + 1]);
                            i++;
                        }
                        continue;
                    }
                    else if (args[i] == "--extended" || args[i] == "-e")
                    {
                        extended = true;
                        continue;
                    }
                    else
                    {
                        // Это размер
                        newSize = uint.Parse(args[i]);
                    }
                }

                if (newSize == 0)
                {
                    Console.Error.WriteLine("fbinst: error: invalid size");
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

                if (!fileManager.FileExists(fileName))
                {
                    Console.Error.WriteLine($"fbinst: error: file '{fileName}' not found");
                    return 1;
                }

                var entry = fileManager.GetFileEntry(fileName);
                byte[] fileData = fileManager.ReadFileData(entry);

                if (newSize < fileData.Length)
                {
                    // Уменьшаем размер
                    Array.Resize(ref fileData, (int)newSize);
                }
                else if (newSize > fileData.Length)
                {
                    // Увеличиваем размер
                    byte[] newData = new byte[newSize];
                    Array.Copy(fileData, newData, fileData.Length);
                    for (int i = fileData.Length; i < newSize; i++)
                        newData[i] = fillByte;
                    fileData = newData;
                }

                // Удаляем старый файл
                fileManager.RemoveFile(fileName);

                // Добавляем новый файл с изменёнными данными
                fileManager.AddFileFromDataAsync(fileName, fileData, extended).Wait();

                Console.WriteLine($"File '{fileName}' resized to {newSize} bytes successfully.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"fbinst: error: {ex.Message}");
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
            try
            {
                if (args.Length < 2 || string.IsNullOrEmpty(args[0]) || string.IsNullOrEmpty(args[1]))
                {
                    Console.Error.WriteLine("fbinst: error: not enough parameters for save");
                    Console.Error.WriteLine("Usage: fbinst DEVICE save ARCHIVE.fba [--list-size NUM]");
                    return 1;
                }

                string devicePath = args[0];
                string archivePath = args[1];
                int listSizeOverride = 0;

                // Парсим дополнительные аргументы
                for (int i = 2; i < args.Length; i++)
                {
                    if (args[i] == "--list-size" || args[i] == "-l")
                    {
                        if (i + 1 < args.Length)
                        {
                            listSizeOverride = int.Parse(args[++i]);
                        }
                    }
                }

                using var diskIo = new DiskIoService();
                if (!diskIo.Open(devicePath))
                {
                    Console.Error.WriteLine($"fbinst: error: failed to open device {devicePath}");
                    return 1;
                }

                var fileManager = new FbFileManagerService(diskIo);
                fileManager.LoadFileListAsync().Wait();

                // Получаем параметры диска
                byte[] mbr = diskIo.ReadSectors(0, 1);
                if (mbr == null || mbr.Length < 512)
                {
                    Console.Error.WriteLine("fbinst: error: failed to read MBR");
                    return 1;
                }

                ushort bootBase = BitConverter.ToUInt16(mbr, 0x1B2);
                byte[] fbData = diskIo.ReadSectors((ulong)(bootBase + 1), 1);
                if (fbData == null || fbData.Length < 16)
                {
                    Console.Error.WriteLine("fbinst: error: failed to read fb_data");
                    return 1;
                }

                ushort priSize = BitConverter.ToUInt16(fbData, 10);
                uint extSize = BitConverter.ToUInt32(fbData, 12);
                ushort listSize = BitConverter.ToUInt16(fbData, 8);
                ushort listUsed = BitConverter.ToUInt16(fbData, 6);

                // Если указан --list-size, используем его
                if (listSizeOverride > 0)
                    listSize = (ushort)(listSizeOverride / 510);

                // Получаем список файлов
                var files = fileManager.GetAllFiles();

                // Создаём архив
                using (var fs = new FileStream(archivePath, FileMode.Create, FileAccess.Write))
                {
                    // 1. Заголовок архива (512 байт)
                    byte[] header = new byte[512];
                    byte[] magicBytes = BitConverter.GetBytes(0x52414246); // "FBAR"
                    Array.Copy(magicBytes, 0, header, 0, 4);
                    header[4] = 1; // ver_major
                    header[5] = 6; // ver_minor
                    byte[] listUsedBytes = BitConverter.GetBytes((ushort)files.Count);
                    Array.Copy(listUsedBytes, 0, header, 6, 2);
                    byte[] listSizeBytes = BitConverter.GetBytes(listSize);
                    Array.Copy(listSizeBytes, 0, header, 8, 2);
                    byte[] priSizeBytes = BitConverter.GetBytes(priSize);
                    Array.Copy(priSizeBytes, 0, header, 10, 2);
                    byte[] extSizeBytes = BitConverter.GetBytes(extSize);
                    Array.Copy(extSizeBytes, 0, header, 12, 4);

                    fs.Write(header, 0, header.Length);

                    // 2. Список файлов (listSize * 510 байт)
                    byte[] fileList = new byte[listSize * 510];
                    int offset = 0;
                    uint currentDataStart = 0;

                    foreach (var file in files)
                    {
                        // Размер записи = 14 + длина имени
                        byte nameLen = (byte)System.Text.Encoding.ASCII.GetBytes(file.Name).Length;
                        byte entrySize = (byte)(14 + nameLen);

                        if (offset + entrySize + 2 > fileList.Length)
                        {
                            Console.Error.WriteLine("fbinst: warning: file list overflow");
                            break;
                        }

                        fileList[offset] = entrySize;
                        fileList[offset + 1] = (byte)(file.IsExtended ? 1 : 0);

                        byte[] startBytes = BitConverter.GetBytes(file.DataStart);
                        Array.Copy(startBytes, 0, fileList, offset + 2, 4);

                        byte[] sizeBytes = BitConverter.GetBytes(file.DataSize);
                        Array.Copy(sizeBytes, 0, fileList, offset + 6, 4);

                        byte[] timeBytes = BitConverter.GetBytes(file.DataTime);
                        Array.Copy(timeBytes, 0, fileList, offset + 10, 4);

                        byte[] nameBytes = System.Text.Encoding.ASCII.GetBytes(file.Name);
                        Array.Copy(nameBytes, 0, fileList, offset + 14, nameLen);

                        offset += entrySize + 2;
                    }

                    fs.Write(fileList, 0, fileList.Length);

                    // 3. Данные файлов
                    foreach (var file in files)
                    {
                        byte[] fileData = fileManager.ReadFileData(file);
                        fs.Write(fileData, 0, fileData.Length);

                        // Выравнивание до сектора
                        uint sectorSize = file.IsExtended ? 512u : 510u;
                        uint fileSizeInSectors = (uint)((file.DataSize + sectorSize - 1) / sectorSize);
                        uint totalSize = fileSizeInSectors * sectorSize;
                        uint padding = totalSize - file.DataSize;
                        if (padding > 0)
                        {
                            byte[] paddingBytes = new byte[padding];
                            fs.Write(paddingBytes, 0, (int)padding);
                        }
                    }
                }

                Console.WriteLine($"Saved {files.Count} files to '{archivePath}' successfully.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"fbinst: error: {ex.Message}");
                return 1;
            }
        }

        static int HandleLoad(string[] args)
        {
            try
            {
                // Формат: fbinst DEVICE load ARCHIVE.fba
                if (args.Length < 2 || string.IsNullOrEmpty(args[0]) || string.IsNullOrEmpty(args[1]))
                {
                    Console.Error.WriteLine("fbinst: error: not enough parameters for load");
                    Console.Error.WriteLine("Usage: fbinst DEVICE load ARCHIVE.fba");
                    return 1;
                }

                string devicePath = args[0];
                string archivePath = args[1];

                if (!File.Exists(archivePath))
                {
                    Console.Error.WriteLine($"fbinst: error: archive file '{archivePath}' not found");
                    return 1;
                }

                using var diskIo = new DiskIoService();
                if (!diskIo.Open(devicePath))
                {
                    Console.Error.WriteLine($"fbinst: error: failed to open device {devicePath}");
                    return 1;
                }

                // Читаем архив
                byte[] archiveData = File.ReadAllBytes(archivePath);
                if (archiveData.Length < 512)
                {
                    Console.Error.WriteLine("fbinst: error: invalid archive file (too small)");
                    return 1;
                }

                // Проверяем магию архива
                uint arMagic = BitConverter.ToUInt32(archiveData, 0);
                if (arMagic != 0x52414246) // "FBAR"
                {
                    Console.Error.WriteLine("fbinst: error: invalid archive file (wrong magic)");
                    return 1;
                }

                // Читаем структуру архива
                byte verMajor = archiveData[4];
                byte verMinor = archiveData[5];
                ushort listUsed = BitConverter.ToUInt16(archiveData, 6);
                ushort listSize = BitConverter.ToUInt16(archiveData, 8);
                ushort priSize = BitConverter.ToUInt16(archiveData, 10);
                uint extSize = BitConverter.ToUInt32(archiveData, 12);

                Console.WriteLine($"Archive version: {verMajor}.{verMinor}");
                Console.WriteLine($"Primary size: {priSize}, Extended size: {extSize}");
                Console.WriteLine($"Files: {listUsed}");

                // Читаем список файлов из архива (начинается с 512 байта)
                int listOffset = 512;
                int listLength = listSize * 510;
                if (archiveData.Length < listOffset + listLength)
                {
                    Console.Error.WriteLine("fbinst: error: archive file truncated");
                    return 1;
                }

                byte[] fileListData = new byte[listLength];
                Array.Copy(archiveData, listOffset, fileListData, 0, listLength);

                // Парсим список файлов из архива
                var fileManager = new FbFileManagerService(diskIo);
                fileManager.LoadFileListAsync().Wait();

                // Очищаем текущие файлы
                fileManager.ClearFiles();

                int offset = 0;
                int filesLoaded = 0;
                while (offset < fileListData.Length)
                {
                    byte size = fileListData[offset];
                    if (size == 0)
                        break;

                    if (offset + 14 >= fileListData.Length)
                        break;

                    uint dataStart = BitConverter.ToUInt32(fileListData, offset + 2);
                    uint dataSize = BitConverter.ToUInt32(fileListData, offset + 6);
                    uint dataTime = BitConverter.ToUInt32(fileListData, offset + 10);

                    int nameLen = size - 12;
                    if (nameLen <= 0 || offset + 14 + nameLen > fileListData.Length)
                        break;

                    string name = System.Text.Encoding.ASCII.GetString(fileListData, offset + 14, nameLen).TrimEnd('\0');
                    bool isExtended = dataStart >= priSize;

                    // Читаем данные файла из архива
                    uint sectorSize = isExtended ? 512u : 510u;
                    uint sectorsNeeded = (uint)((dataSize + sectorSize - 1) / sectorSize);
                    uint fileOffset = (uint)(listOffset + listLength + (dataStart - (isExtended ? priSize : 0)) * sectorSize);

                    if (archiveData.Length < fileOffset + dataSize)
                    {
                        Console.Error.WriteLine($"fbinst: warning: file '{name}' truncated");
                        continue;
                    }

                    byte[] fileData = new byte[dataSize];
                    Array.Copy(archiveData, fileOffset, fileData, 0, dataSize);

                    // Добавляем файл
                    fileManager.AddFileFromDataAsync(name, fileData, isExtended).Wait();
                    filesLoaded++;
                    offset += size + 2;
                }

                fileManager.SaveFileListAsync().Wait();
                Console.WriteLine($"Loaded {filesLoaded} files from archive successfully.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"fbinst: error: {ex.Message}");
                return 1;
            }
        }

        static int HandleCreate(string[] args)
        {
            try
            {
                if (args.Length < 1 || string.IsNullOrEmpty(args[0]))
                {
                    Console.Error.WriteLine("fbinst: error: archive file not specified");
                    Console.Error.WriteLine("Usage: fbinst create ARCHIVE.fba [--primary NUM] [--extended NUM] [--list-size NUM]");
                    return 1;
                }

                string archivePath = args[0];
                ushort priSize = 16128; // Значение по умолчанию из оригинального fbinst
                uint extSize = 0;
                ushort listSize = 896; // Значение по умолчанию

                // Парсим аргументы
                for (int i = 1; i < args.Length; i++)
                {
                    if (args[i] == "--primary" || args[i] == "-p")
                    {
                        if (i + 1 < args.Length)
                        {
                            priSize = ushort.Parse(args[++i]);
                        }
                    }
                    else if (args[i] == "--extended" || args[i] == "-e")
                    {
                        if (i + 1 < args.Length)
                        {
                            extSize = uint.Parse(args[++i]);
                        }
                    }
                    else if (args[i] == "--list-size" || args[i] == "-l")
                    {
                        if (i + 1 < args.Length)
                        {
                            listSize = ushort.Parse(args[++i]);
                        }
                    }
                }

                // Создаём архив
                using (var fs = new FileStream(archivePath, FileMode.Create, FileAccess.Write))
                {
                    // 1. Заголовок архива (512 байт)
                    byte[] header = new byte[512];
                    byte[] magicBytes = BitConverter.GetBytes(0x52414246); // "FBAR"
                    Array.Copy(magicBytes, 0, header, 0, 4);
                    header[4] = 1; // ver_major
                    header[5] = 6; // ver_minor
                    byte[] listUsedBytes = BitConverter.GetBytes((ushort)0); // Нет файлов
                    Array.Copy(listUsedBytes, 0, header, 6, 2);
                    byte[] listSizeBytes = BitConverter.GetBytes(listSize);
                    Array.Copy(listSizeBytes, 0, header, 8, 2);
                    byte[] priSizeBytes = BitConverter.GetBytes(priSize);
                    Array.Copy(priSizeBytes, 0, header, 10, 2);
                    byte[] extSizeBytes = BitConverter.GetBytes(extSize);
                    Array.Copy(extSizeBytes, 0, header, 12, 4);

                    fs.Write(header, 0, header.Length);

                    // 2. Пустой список файлов (listSize * 510 байт)
                    byte[] emptyList = new byte[listSize * 510];
                    fs.Write(emptyList, 0, emptyList.Length);
                }

                Console.WriteLine($"Created empty archive '{archivePath}' successfully.");
                Console.WriteLine($"  Primary size: {priSize} sectors");
                Console.WriteLine($"  Extended size: {extSize} sectors");
                Console.WriteLine($"  List size: {listSize} sectors");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"fbinst: error: {ex.Message}");
                return 1;
            }
        }

        static int HandleCheck(string[] args)
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

                // 1. Читаем MBR
                byte[] mbr = diskIo.ReadSectors(0, 1);
                if (mbr == null || mbr.Length < 512)
                {
                    Console.Error.WriteLine("fbinst: error: failed to read MBR");
                    return 1;
                }

                // 2. Проверяем fbinst
                uint fbMagic = BitConverter.ToUInt32(mbr, 0x1B4);
                if (fbMagic != 0x46424246)
                {
                    Console.Error.WriteLine("fbinst: error: fb mbr not initialized");
                    return 1;
                }

                // 3. Получаем параметры
                ushort bootBase = BitConverter.ToUInt16(mbr, 0x1B2);
                byte[] fbData = diskIo.ReadSectors((ulong)(bootBase + 1), 1);
                if (fbData == null || fbData.Length < 16)
                {
                    Console.Error.WriteLine("fbinst: error: failed to read fb_data");
                    return 1;
                }

                ushort priSize = BitConverter.ToUInt16(fbData, 10);
                uint extSize = BitConverter.ToUInt32(fbData, 12);
                ushort listSize = BitConverter.ToUInt16(fbData, 8);
                ushort listUsed = BitConverter.ToUInt16(fbData, 6);

                Console.WriteLine($"Checking fbinst structure on {devicePath}...");
                Console.WriteLine($"  Boot base: {bootBase}");
                Console.WriteLine($"  Primary size: {priSize}");
                Console.WriteLine($"  Extended size: {extSize}");
                Console.WriteLine($"  List size: {listSize}");
                Console.WriteLine($"  List used: {listUsed}");

                bool hasErrors = false;

                // 4. Проверяем MBR во всех секторах от 0 до bootBase
                Console.WriteLine($"Checking MBR signatures (0..{bootBase})...");
                for (uint i = 0; i <= bootBase; i++)
                {
                    byte[] sector = diskIo.ReadSectors(i, 1);
                    if (sector == null || sector.Length < 512)
                    {
                        Console.Error.WriteLine($"  Error: failed to read sector {i}");
                        hasErrors = true;
                        continue;
                    }

                    ushort signature = BitConverter.ToUInt16(sector, 0x1FE);
                    if (signature != 0xAA55)
                    {
                        Console.Error.WriteLine($"  Error: invalid MBR signature at sector {i}: 0x{signature:X4}");
                        hasErrors = true;
                    }

                    // Проверяем fb_magic в каждом секторе (должен быть только в секторе 0)
                    uint magic = BitConverter.ToUInt32(sector, 0x1B4);
                    if (i == 0 && magic != 0x46424246)
                    {
                        Console.Error.WriteLine($"  Error: fb_magic not found in sector 0");
                        hasErrors = true;
                    }
                    else if (i > 0 && magic == 0x46424246)
                    {
                        Console.Error.WriteLine($"  Error: fb_magic found in sector {i} (should only be in sector 0)");
                        hasErrors = true;
                    }
                }

                // 5. Проверяем fb_data
                Console.WriteLine($"Checking fb_data...");
                if (fbData[4] != 1 && fbData[5] != 6)
                {
                    Console.Error.WriteLine($"  Warning: unexpected version {fbData[4]}.{fbData[5]}");
                }

                // 6. Проверяем список файлов
                Console.WriteLine($"Checking file list...");
                uint listStartSector = (uint)(bootBase + 1 + BitConverter.ToUInt16(fbData, 0));
                byte[] fileList = diskIo.ReadSectors(listStartSector, listSize);
                if (fileList == null || fileList.Length == 0)
                {
                    Console.Error.WriteLine("  Error: failed to read file list");
                    hasErrors = true;
                }
                else
                {
                    int offset = 0;
                    int fileCount = 0;
                    bool prevWasZero = false;

                    while (offset < fileList.Length)
                    {
                        byte size = fileList[offset];
                        if (size == 0)
                        {
                            // Проверяем, что после нуля идут только нули
                            for (int i = offset; i < fileList.Length; i++)
                            {
                                if (fileList[i] != 0)
                                {
                                    Console.Error.WriteLine($"  Error: non-zero data after end marker at offset {i}");
                                    hasErrors = true;
                                    break;
                                }
                            }
                            break;
                        }

                        if (offset + 14 >= fileList.Length)
                        {
                            Console.Error.WriteLine($"  Error: truncated file entry at offset {offset}");
                            hasErrors = true;
                            break;
                        }

                        uint dataStart = BitConverter.ToUInt32(fileList, offset + 2);
                        uint dataSize = BitConverter.ToUInt32(fileList, offset + 6);
                        int nameLen = size - 12;
                        if (nameLen <= 0 || offset + 14 + nameLen > fileList.Length)
                        {
                            Console.Error.WriteLine($"  Error: invalid file entry at offset {offset}");
                            hasErrors = true;
                            break;
                        }

                        string name = System.Text.Encoding.ASCII.GetString(fileList, offset + 14, nameLen).TrimEnd('\0');
                        bool isExtended = dataStart >= priSize;

                        // Проверяем, что файл не выходит за пределы области
                        uint sectorSize = isExtended ? 512u : 510u;
                        uint sectorsNeeded = (uint)((dataSize + sectorSize - 1) / sectorSize);
                        uint fileEnd = dataStart + sectorsNeeded;

                        if (isExtended && fileEnd > priSize + extSize)
                        {
                            Console.Error.WriteLine($"  Error: file '{name}' extends beyond extended area");
                            hasErrors = true;
                        }
                        else if (!isExtended && fileEnd > priSize)
                        {
                            Console.Error.WriteLine($"  Error: file '{name}' extends beyond primary area");
                            hasErrors = true;
                        }

                        fileCount++;
                        offset += size + 2;
                    }

                    Console.WriteLine($"  Found {fileCount} files");
                }

                // 7. Проверяем свободное место
                Console.WriteLine($"Checking free space...");
                var fileManager = new FbFileManagerService(diskIo);
                fileManager.LoadFileListAsync().Wait();

                uint totalPrimaryFree = 0;
                uint totalExtendedFree = 0;
                uint currentPos = listStartSector + listSize;

                foreach (var file in fileManager.GetAllFiles())
                {
                    if (file.DataStart > currentPos)
                    {
                        uint freeSize = file.DataStart - currentPos;
                        if (currentPos < priSize)
                            totalPrimaryFree += freeSize;
                        else
                            totalExtendedFree += freeSize;
                    }
                    uint sectorSize = file.IsExtended ? 512u : 510u;
                    currentPos = file.DataStart + (uint)((file.DataSize + sectorSize - 1) / sectorSize);
                }

                if (currentPos < priSize)
                    totalPrimaryFree += priSize - currentPos;
                else if (currentPos < priSize + extSize)
                    totalExtendedFree += (uint)(priSize + extSize - currentPos);

                Console.WriteLine($"  Primary free: {totalPrimaryFree * 510} bytes");
                Console.WriteLine($"  Extended free: {totalExtendedFree * 512} bytes");

                if (hasErrors)
                {
                    Console.WriteLine("Check completed with errors.");
                    return 1;
                }
                else
                {
                    Console.WriteLine("Check completed successfully.");
                    return 0;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"fbinst: error: {ex.Message}");
                return 1;
            }
        }

        static int HandleAddMenu(string[] args)
        {
            try
            {
                if (args.Length < 2 || string.IsNullOrEmpty(args[0]) || string.IsNullOrEmpty(args[1]))
                {
                    Console.Error.WriteLine("fbinst: error: not enough parameters for add-menu");
                    Console.Error.WriteLine("Usage: fbinst DEVICE add-menu FILE");
                    Console.Error.WriteLine("       fbinst DEVICE add-menu --string \"menu text\"");
                    return 1;
                }

                string devicePath = args[0];
                string source = null;
                bool isString = false;
                bool append = false;

                // Парсим аргументы
                for (int i = 1; i < args.Length; i++)
                {
                    if (args[i] == "--string" || args[i] == "-s")
                    {
                        isString = true;
                        continue;
                    }
                    else if (args[i] == "--append" || args[i] == "-a")
                    {
                        append = true;
                        continue;
                    }
                    else if (source == null)
                    {
                        source = args[i];
                    }
                }

                if (string.IsNullOrEmpty(source))
                {
                    Console.Error.WriteLine("fbinst: error: source file or string not specified");
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

                byte[] menuData;

                if (isString)
                {
                    menuData = System.Text.Encoding.UTF8.GetBytes(source);
                }
                else
                {
                    if (!File.Exists(source))
                    {
                        Console.Error.WriteLine($"fbinst: error: file '{source}' not found");
                        return 1;
                    }
                    menuData = File.ReadAllBytes(source);
                }

                const string targetName = "fb.cfg";

                if (fileManager.FileExists(targetName) && append)
                {
                    var existingEntry = fileManager.GetFileEntry(targetName);
                    byte[] existingData = fileManager.ReadFileData(existingEntry);
                    byte[] newData = new byte[existingData.Length + menuData.Length];
                    Array.Copy(existingData, newData, existingData.Length);
                    Array.Copy(menuData, 0, newData, existingData.Length, menuData.Length);
                    menuData = newData;
                }

                if (fileManager.FileExists(targetName))
                    fileManager.RemoveFile(targetName);

                fileManager.AddFileFromDataAsync(targetName, menuData, false).Wait();
                fileManager.SaveFileListAsync().Wait();

                Console.WriteLine($"Menu '{targetName}' added successfully.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"fbinst: error: {ex.Message}");
                return 1;
            }
        }

        static int HandleCat(string[] args)
        {
            try
            {
                if (args.Length < 2 || string.IsNullOrEmpty(args[0]) || string.IsNullOrEmpty(args[1]))
                {
                    Console.Error.WriteLine("fbinst: error: not enough parameters for cat");
                    Console.Error.WriteLine("Usage: fbinst DEVICE cat NAME");
                    return 1;
                }

                string devicePath = args[0];
                string fileName = args[1];

                using var diskIo = new DiskIoService();
                if (!diskIo.Open(devicePath))
                {
                    Console.Error.WriteLine($"fbinst: error: failed to open device {devicePath}");
                    return 1;
                }

                var fileManager = new FbFileManagerService(diskIo);
                fileManager.LoadFileListAsync().Wait();

                if (!fileManager.FileExists(fileName))
                {
                    Console.Error.WriteLine($"fbinst: error: file '{fileName}' not found");
                    return 1;
                }

                var entry = fileManager.GetFileEntry(fileName);
                byte[] fileData = fileManager.ReadFileData(entry);

                // Выводим содержимое как текст
                Console.OutputEncoding = System.Text.Encoding.UTF8;
                Console.Write(System.Text.Encoding.UTF8.GetString(fileData));

                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"fbinst: error: {ex.Message}");
                return 1;
            }
        }

        static int HandleCatMenu(string[] args)
        {
            try
            {
                if (args.Length < 1 || string.IsNullOrEmpty(args[0]))
                {
                    Console.Error.WriteLine("fbinst: error: not enough parameters for cat-menu");
                    Console.Error.WriteLine("Usage: fbinst DEVICE cat-menu [NAME]");
                    return 1;
                }

                string devicePath = args[0];
                string menuName = args.Length > 1 ? args[1] : "fb.cfg";

                using var diskIo = new DiskIoService();
                if (!diskIo.Open(devicePath))
                {
                    Console.Error.WriteLine($"fbinst: error: failed to open device {devicePath}");
                    return 1;
                }

                var fileManager = new FbFileManagerService(diskIo);
                fileManager.LoadFileListAsync().Wait();

                if (!fileManager.FileExists(menuName))
                {
                    Console.Error.WriteLine($"fbinst: error: menu file '{menuName}' not found");
                    return 1;
                }

                var entry = fileManager.GetFileEntry(menuName);
                byte[] fileData = fileManager.ReadFileData(entry);

                // Парсим меню и выводим в читаемом виде
                ParseAndPrintMenu(fileData);

                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"fbinst: error: {ex.Message}");
                return 1;
            }
        }

        private static void ParseAndPrintMenu(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                Console.WriteLine("(empty menu)");
                return;
            }

            int offset = 0;
            bool hasEntries = false;

            while (offset < data.Length)
            {
                byte size = data[offset];
                if (size == 0)
                    break;

                if (offset + 1 >= data.Length)
                    break;

                byte type = data[offset + 1];

                switch (type)
                {
                    case 1: // FBM_TYPE_MENU
                        if (offset + 4 >= data.Length)
                            break;
                        ushort key = BitConverter.ToUInt16(data, offset + 2);
                        byte sysType = data[offset + 4];
                        string name = System.Text.Encoding.ASCII.GetString(data, offset + 5, size - 4).TrimEnd('\0');
                        string sysTypeName = GetSystemTypeName(sysType);
                        string keyName = GetKeyName(key);
                        Console.WriteLine($"menu {keyName} {sysTypeName} \"{name}\"");
                        hasEntries = true;
                        break;

                    case 2: // FBM_TYPE_TEXT
                        string text = System.Text.Encoding.ASCII.GetString(data, offset + 2, size - 2).TrimEnd('\0');
                        // Проверяем, есть ли в конце \r\n
                        bool hasNewline = text.EndsWith("\r\n");
                        if (hasNewline)
                            text = text.Substring(0, text.Length - 2);
                        Console.WriteLine($"text {(hasNewline ? "" : "-n ")}\"{text}\"");
                        hasEntries = true;
                        break;

                    case 3: // FBM_TYPE_TIMEOUT
                        if (offset + 2 >= data.Length)
                            break;
                        byte timeout = data[offset + 2];
                        Console.WriteLine($"timeout {timeout}");
                        hasEntries = true;
                        break;

                    case 4: // FBM_TYPE_DEFAULT
                        if (offset + 2 >= data.Length)
                            break;
                        byte defaultItem = data[offset + 2];
                        Console.WriteLine($"default {defaultItem}");
                        hasEntries = true;
                        break;

                    case 5: // FBM_TYPE_COLOR
                        if (offset + 2 >= data.Length)
                            break;
                        byte color = data[offset + 2];
                        Console.WriteLine($"color {GetColorName(color)}");
                        hasEntries = true;
                        break;

                    default:
                        Console.WriteLine($"unknown type {type} at offset {offset}");
                        break;
                }

                offset += size + 2;
            }

            if (!hasEntries)
            {
                Console.WriteLine("(no menu entries)");
            }
        }

        private static string GetSystemTypeName(byte type)
        {
            return type switch
            {
                1 => "menu",
                2 => "buldr",
                3 => "syslinux",
                4 => "linux",
                5 => "msdos",
                6 => "freedos",
                7 => "chain",
                8 => "grldr",
                _ => $"unknown({type})"
            };
        }

        private static string GetKeyName(ushort key)
        {
            // Обработка специальных клавиш
            if (key == 0x011b) return "escape";
            if (key >= 0x3b00 && key <= 0x4400)
                return $"F{(key - 0x3b00) + 1}";
            if (key >= 0x4f00 && key <= 0x5200)
                return $"end"; // end, down, pgdn, left
            if (key == 0x5000) return "down";
            if (key == 0x5100) return "pgdn";
            if (key == 0x4b00) return "left";
            if (key == 0x4d00) return "right";
            if (key == 0x4800) return "up";
            if (key == 0x4900) return "pgup";
            if (key == 0x4700) return "home";
            if (key == 0x4f00) return "end";
            if (key == 0x5300) return "del";
            if (key == 0x5200) return "insert";
            if (key >= 0x3b00 && key <= 0x4400)
                return $"F{(key - 0x3b00) + 1}";
            // Числовые клавиши 0-9
            if (key >= 0x30 && key <= 0x39)
                return ((char)key).ToString();
            // Буквы A-Z
            if (key >= 0x41 && key <= 0x5A)
                return ((char)key).ToString();
            return $"0x{key:X4}";
        }

        private static string GetColorName(byte color)
        {
            string[] colors = {
        "black", "blue", "green", "cyan",
        "red", "magenta", "brown", "light-gray",
        "dark-gray", "light-blue", "light-green", "light-cyan",
        "light-red", "light-magenta", "yellow", "white"
    };

            if (color == 7) return "normal";
            int fg = color & 0x0F;
            int bg = (color >> 4) & 0x0F;
            if (bg == 0) return colors[fg];
            return $"{colors[fg]}/{colors[bg]}";
        }

        static int HandlePack(string[] args)
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

                var fileManager = new FbFileManagerService(diskIo);
                fileManager.LoadFileListAsync().Wait();

                // Получаем все файлы
                var files = fileManager.GetAllFiles();
                if (files.Count == 0)
                {
                    Console.WriteLine("No files to pack.");
                    return 0;
                }

                // Сортируем файлы по DataStart
                files.Sort((a, b) => a.DataStart.CompareTo(b.DataStart));

                // Получаем параметры диска
                byte[] mbr = diskIo.ReadSectors(0, 1);
                if (mbr == null || mbr.Length < 512)
                {
                    Console.Error.WriteLine("fbinst: error: failed to read MBR");
                    return 1;
                }

                ushort bootBase = BitConverter.ToUInt16(mbr, 0x1B2);
                byte[] fbData = diskIo.ReadSectors((ulong)(bootBase + 1), 1);
                if (fbData == null || fbData.Length < 16)
                {
                    Console.Error.WriteLine("fbinst: error: failed to read fb_data");
                    return 1;
                }

                ushort priSize = BitConverter.ToUInt16(fbData, 10);
                uint extSize = BitConverter.ToUInt32(fbData, 12);
                ushort bootSize = BitConverter.ToUInt16(fbData, 0);
                uint listStartSector = (uint)(bootBase + 1 + bootSize);

                // Начинаем упаковку с primary области
                uint currentPrimaryPos = listStartSector + BitConverter.ToUInt16(fbData, 8);
                uint currentExtendedPos = priSize;

                bool moved = false;

                foreach (var file in files)
                {
                    uint sectorSize = file.IsExtended ? 512u : 510u;
                    uint sectorsNeeded = (uint)((file.DataSize + sectorSize - 1) / sectorSize);

                    uint targetPos;
                    if (file.IsExtended)
                    {
                        targetPos = currentExtendedPos;
                        currentExtendedPos += sectorsNeeded;
                    }
                    else
                    {
                        targetPos = currentPrimaryPos;
                        currentPrimaryPos += sectorsNeeded;
                    }

                    if (file.DataStart != targetPos)
                    {
                        // Перемещаем файл на новое место
                        Console.WriteLine($"Moving '{file.Name}' from 0x{file.DataStart:X} to 0x{targetPos:X}");

                        // Читаем данные файла
                        byte[] fileData = fileManager.ReadFileData(file);

                        // Удаляем старый файл
                        fileManager.RemoveFile(file.Name);

                        // Создаём новый файл на новом месте
                        fileManager.AddFileFromDataAsync(file.Name, fileData, file.IsExtended).Wait();

                        moved = true;
                    }
                }

                if (moved)
                {
                    // Сохраняем обновлённый список
                    fileManager.SaveFileListAsync().Wait();
                    Console.WriteLine("Pack completed successfully.");
                }
                else
                {
                    Console.WriteLine("No fragmentation found.");
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"fbinst: error: {ex.Message}");
                return 1;
            }
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