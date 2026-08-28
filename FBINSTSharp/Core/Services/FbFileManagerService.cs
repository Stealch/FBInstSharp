using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FBINSTSharp.Core.Interfaces;

namespace FBINSTSharp.Core.Services
{
    public class FbFileManagerService
    {
        private readonly IDiskIoService _diskIo;
        private readonly Dictionary<string, FbFileEntry> _files = new Dictionary<string, FbFileEntry>();
        private uint _priSize;
        private int _listSize;
        private int _listUsed;
        private int _bootBase;

        public FbFileManagerService(IDiskIoService diskIo)
        {
            _diskIo = diskIo ?? throw new ArgumentNullException(nameof(diskIo));
        }

        public async Task LoadFileListAsync()
        {
            // Читаем MBR
            byte[] mbr = await _diskIo.ReadSectorsAsync(0, 1);
            if (mbr == null || mbr.Length < 512)
                throw new InvalidOperationException("Failed to read MBR");

            // Проверяем fbinst
            uint fbMagic = BitConverter.ToUInt32(mbr, 0x1B4);
            if (fbMagic != 0x46424246)
                throw new InvalidOperationException("fbinst MBR not found");

            // Читаем boot_base
            _bootBase = BitConverter.ToUInt16(mbr, 0x1B2);

            // Читаем fb_data из сектора boot_base + 1
            ulong fbDataSector = (ulong)(_bootBase + 1);
            byte[] fbData = await _diskIo.ReadSectorsAsync(fbDataSector, 1);
            if (fbData == null || fbData.Length < 16)
                throw new InvalidOperationException("Failed to read fb_data");

            // Извлекаем параметры
            _priSize = BitConverter.ToUInt16(fbData, 10);
            _listSize = BitConverter.ToUInt16(fbData, 8) * 510;
            _listUsed = BitConverter.ToUInt16(fbData, 6);

            // Читаем список файлов
            ushort bootSize = BitConverter.ToUInt16(fbData, 0);
            ulong listStart = (ulong)(_bootBase + 1 + bootSize);
            uint listSectors = (uint)(_listSize / 510);

            byte[] fileList = await _diskIo.ReadSectorsAsync(listStart, listSectors);
            if (fileList == null || fileList.Length == 0)
                return;

            // Парсим список
            _files.Clear();
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

                string name = Encoding.ASCII.GetString(fileList, offset + 14, nameLen).TrimEnd('\0');

                _files[name] = new FbFileEntry
                {
                    Name = name,
                    DataStart = dataStart,
                    DataSize = dataSize,
                    DataTime = dataTime,
                    IsExtended = dataStart >= _priSize
                };

                offset += size + 2;
            }
        }

        public async Task AddFileAsync(string name, string sourcePath, bool extended = false)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("File name cannot be empty", nameof(name));

            if (!File.Exists(sourcePath))
                throw new FileNotFoundException($"File not found: {sourcePath}");

            // Читаем файл
            byte[] fileData = File.ReadAllBytes(sourcePath);
            uint fileSize = (uint)fileData.Length;

            // Находим свободное место
            uint startSector = FindFreeSpace(fileSize, extended);

            // Создаём запись в списке
            var entry = new FbFileEntry
            {
                Name = name,
                DataStart = startSector,
                DataSize = fileSize,
                DataTime = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                IsExtended = extended
            };

            // Сохраняем файл на диск
            await WriteFileDataAsync(startSector, fileData, extended);

            // Добавляем в список
            _files[name] = entry;

            // Сохраняем список файлов
            await SaveFileListAsync();
        }

        public async Task AddFileFromDataAsync(string name, byte[] fileData, bool extended = false)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("File name cannot be empty", nameof(name));

            if (fileData == null || fileData.Length == 0)
                throw new ArgumentException("File data cannot be empty", nameof(fileData));

            uint fileSize = (uint)fileData.Length;

            // Находим свободное место
            uint startSector = FindFreeSpace(fileSize, extended);

            // Создаём запись в списке
            var entry = new FbFileEntry
            {
                Name = name,
                DataStart = startSector,
                DataSize = fileSize,
                DataTime = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                IsExtended = extended
            };

            // Сохраняем файл на диск
            await WriteFileDataAsync(startSector, fileData, extended);

            // Добавляем в список
            _files[name] = entry;

            // Сохраняем список файлов
            await SaveFileListAsync();
        }

        private uint FindFreeSpace(uint fileSize, bool extended)
        {
            uint sectorSize = extended ? 512u : 510u;
            uint sectorsNeeded = (fileSize + sectorSize - 1) / sectorSize;

            // Начинаем поиск с конца primary или начала extended
            uint start = extended ? _priSize : (uint)(_bootBase + 1 + _listSize / 510);

            // Проверяем занятые области
            foreach (var file in _files.Values)
            {
                uint fileEnd = file.DataStart + (file.DataSize + (file.IsExtended ? 511u : 509u)) / (file.IsExtended ? 512u : 510u);
                if (file.IsExtended == extended)
                {
                    if (start >= file.DataStart && start < fileEnd)
                        start = fileEnd;
                }
            }

            return start;
        }

        private async Task WriteFileDataAsync(uint startSector, byte[] data, bool extended)
        {
            uint sectorSize = extended ? 512u : 510u;
            uint sectorsNeeded = (uint)((data.Length + sectorSize - 1) / sectorSize);
            byte[] buffer = new byte[512];

            for (uint i = 0; i < sectorsNeeded; i++)
            {
                Array.Clear(buffer, 0, buffer.Length);

                uint offset = i * sectorSize;
                uint length = Math.Min(sectorSize, (uint)data.Length - offset);

                Array.Copy(data, offset, buffer, 0, length);

                if (!extended)
                {
                    // Записываем маркер сектора (последние 2 байта = номер сектора)
                    byte[] marker = BitConverter.GetBytes((ushort)(startSector + i));
                    Array.Copy(marker, 0, buffer, 510, 2);
                }

                await _diskIo.WriteSectorsAsync(startSector + i, buffer);
            }
        }

        public async Task SaveFileListAsync()
        {
            // Формируем список файлов в буфере
            byte[] fileList = new byte[_listSize];
            int offset = 0;

            foreach (var entry in _files.Values)
            {
                // Размер записи = 14 + длина имени
                byte nameLen = (byte)Encoding.ASCII.GetBytes(entry.Name).Length;
                byte entrySize = (byte)(14 + nameLen);

                if (offset + entrySize + 2 > fileList.Length)
                    throw new InvalidOperationException("File list is full");

                fileList[offset] = entrySize;
                fileList[offset + 1] = (byte)(entry.IsExtended ? 1 : 0);

                byte[] startBytes = BitConverter.GetBytes(entry.DataStart);
                Array.Copy(startBytes, 0, fileList, offset + 2, 4);

                byte[] sizeBytes = BitConverter.GetBytes(entry.DataSize);
                Array.Copy(sizeBytes, 0, fileList, offset + 6, 4);

                byte[] timeBytes = BitConverter.GetBytes(entry.DataTime);
                Array.Copy(timeBytes, 0, fileList, offset + 10, 4);

                byte[] nameBytes = Encoding.ASCII.GetBytes(entry.Name);
                Array.Copy(nameBytes, 0, fileList, offset + 14, nameLen);

                offset += entrySize + 2;
            }

            // Сохраняем список на диск
            ushort bootSize = BitConverter.ToUInt16(await _diskIo.ReadSectorsAsync((ulong)(_bootBase + 1), 1), 0);
            ulong listStart = (ulong)(_bootBase + 1 + bootSize);
            uint listSectors = (uint)(_listSize / 510);

            await _diskIo.WriteSectorsAsync(listStart, fileList);
        }

        public void RemoveFile(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("File name cannot be empty", nameof(name));

            if (!_files.ContainsKey(name))
                throw new InvalidOperationException($"File '{name}' not found");

            _files.Remove(name);
        }

        public bool FileExists(string name)
        {
            return _files.ContainsKey(name);
        }

        public class FbFileEntry
        {
            public string Name { get; set; }
            public uint DataStart { get; set; }
            public uint DataSize { get; set; }
            public uint DataTime { get; set; }
            public bool IsExtended { get; set; }
        }
    }
}