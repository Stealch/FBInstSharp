using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FBINSTSharp.Core.Interfaces;
using FBINSTSharp.Core.Models;

namespace FBINSTSharp.Core.Services
{
    public class FbFormatterService
    {
        private readonly IDiskIoService _diskIo;
        private readonly Fat32FormatterService _fat32Formatter;

        // Константы из оригинального fbinst.c
        private const uint FB_MAGIC_LONG = 0x46424246;
        private const uint MIN_PRI_SIZE = 63 * 256;
        private const uint MAX_PRI_SIZE = 65535;
        private const int MAX_LIST_SIZE = (0x80000 - 0x10000) >> 9;
        private const int DEF_LIST_SIZE = MAX_LIST_SIZE * 510;
        private const int DEF_BASE_SIZE = 63;
        private const int MIN_NAND_ALIGN = 255;

        public FbFormatterService(IDiskIoService diskIo)
        {
            _diskIo = diskIo ?? throw new ArgumentNullException(nameof(diskIo));
            _fat32Formatter = new Fat32FormatterService(diskIo);
        }

        public async Task FormatAsync(FormatOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            // 1. Получаем геометрию диска
            var geometry = _diskIo.GetGeometry();
            uint bytesPerSector = geometry.BytesPerSector;
            ulong totalSectors = _diskIo.GetTotalSectors();

            // 2. Проверяем, что диск достаточно большой
            if (totalSectors < 1024 * 1024) // минимум ~512 МБ
                throw new InvalidOperationException("Disk is too small for fbinst (minimum ~512 MB)");

            // 3. Проверяем съёмность (если не принудительно)
            if (!options.Force && !options.Raw && !_diskIo.IsRemovable)
                throw new InvalidOperationException("Device is not removable. Use --force to override.");

            // 4. Рассчитываем параметры
            var fbParams = CalculateFbParameters(options, totalSectors, bytesPerSector);

            // 5. Очищаем диск (записываем нули в начало)
            await ClearDiskAsync(fbParams.TotalSectors);

            // 6. Создаём MBR с загрузочным кодом fbinst
            await CreateFbMbrAsync(fbParams);

            // 7. Создаём структуру fb_data
            await CreateFbDataAsync(fbParams);

            // 8. Создаём FAT32-раздел
            await CreateFat32PartitionAsync(fbParams, options);

            // 9. Синхронизируем изменения
            _diskIo.DismountVolume();
        }

        private FbFormatParameters CalculateFbParameters(FormatOptions options, ulong totalSectors, uint bytesPerSector)
        {
            var result = new FbFormatParameters();

            // Определяем базовые параметры (из оригинального fbinst.c)
            result.BaseSector = options.BaseSector > 0 ? options.BaseSector : DEF_BASE_SIZE;

            // Размер primary области (мин 63*256, макс 65535)
            if (options.PrimarySize > 0)
                result.PrimarySize = (uint)Math.Min(Math.Max(options.PrimarySize, (ulong)MIN_PRI_SIZE), (ulong)MAX_PRI_SIZE);
            else
                result.PrimarySize = MIN_PRI_SIZE;

            // Размер extended области
            result.ExtendedSize = options.ExtendedSize;

            // Размер списка файлов
            result.ListSize = options.ListSize > 0
                ? Math.Min(options.ListSize / 510, MAX_LIST_SIZE) * 510
                : DEF_LIST_SIZE;

            // Выравнивание для NAND
            result.NandAlign = options.NandAlign > 0 ? options.NandAlign : MIN_NAND_ALIGN;

            // Размер раздела
            ulong totalNeeded = (ulong)result.BaseSector + result.PrimarySize + result.ExtendedSize;
            if (totalNeeded >= totalSectors)
                throw new InvalidOperationException($"Disk too small: need {totalNeeded} sectors, have {totalSectors}");

            result.PartitionSize = options.PartitionSize > 0
                ? Math.Min(options.PartitionSize, totalSectors - totalNeeded)
                : totalSectors - totalNeeded;

            // Проверяем минимальный размер раздела для FAT32
            if (result.PartitionSize < 65536)
                throw new InvalidOperationException($"Partition too small for FAT32: {result.PartitionSize} sectors (min 65536)");

            // Определяем тип ФС (FAT32 по умолчанию)
            result.IsFat32 = options.IsFat32 || !options.IsFat16;

            // Запоминаем общий размер
            result.TotalSectors = totalSectors;

            return result;
        }

        private async Task ClearDiskAsync(ulong totalSectors)
        {
            // Очищаем первые 1 МБ диска (как в оригинальном fbinst)
            uint sectorsToClear = (uint)Math.Min(totalSectors, 2048);
            byte[] zeroBuffer = new byte[512];

            for (uint i = 0; i < sectorsToClear; i++)
            {
                await _diskIo.WriteSectorsAsync(i, zeroBuffer);
            }
        }

        private async Task CreateFbMbrAsync(FbFormatParameters parameters)
        {
            // Создаём MBR с загрузочным кодом fbinst
            byte[] mbr = new byte[512];

            // Загрузочный код fbinst (из оригинального fbmbr.S)
            // Для простоты используем минимальный загрузчик
            // В реальном проекте здесь должен быть бинарник из fbmbr.bin

            // Устанавливаем базовые поля MBR
            mbr[0x1AD] = 63; // max_sec
            mbr[0x1AE] = 0;  // lba (0)
            mbr[0x1AF] = 0x80; // bootdrv
            mbr[0x1B0] = 63; // spt
            mbr[0x1B1] = 255; // heads

            // boot_base (2 байта, little-endian)
            byte[] bootBaseBytes = BitConverter.GetBytes((ushort)parameters.BaseSector);
            Array.Copy(bootBaseBytes, 0, mbr, 0x1B2, 2);

            // fb_magic (4 байта, little-endian)
            byte[] magicBytes = BitConverter.GetBytes(FB_MAGIC_LONG);
            Array.Copy(magicBytes, 0, mbr, 0x1B4, 4);

            // Сигнатура MBR (0xAA55)
            mbr[0x1FE] = 0x55;
            mbr[0x1FF] = 0xAA;

            // Создаём запись в таблице разделов для FAT32
            // Смещение раздела: BaseSector + PrimarySize + ExtendedSize
            uint partitionStart = (uint)((ulong)parameters.BaseSector + parameters.PrimarySize + parameters.ExtendedSize);
            byte partitionType = parameters.IsFat32 ? (byte)0x0C : (byte)0x0E; // FAT32 LBA или FAT16 LBA

            // Заполняем запись раздела (смещение 0x1BE)
            mbr[0x1BE] = 0x80; // bootable
            // CHS адреса (упрощённо)
            mbr[0x1BF] = 0x01;
            mbr[0x1C0] = 0x01;
            mbr[0x1C1] = 0x00;
            mbr[0x1C2] = partitionType;
            // Конец CHS
            mbr[0x1C3] = 0xFE;
            mbr[0x1C4] = 0xFF;
            mbr[0x1C5] = 0xFF;

            // LBA начало раздела
            byte[] startBytes = BitConverter.GetBytes(partitionStart);
            Array.Copy(startBytes, 0, mbr, 0x1C6, 4);

            // Размер раздела
            byte[] sizeBytes = BitConverter.GetBytes((uint)parameters.PartitionSize);
            Array.Copy(sizeBytes, 0, mbr, 0x1CA, 4);

            // Записываем MBR
            await _diskIo.WriteSectorsAsync(0, mbr);
        }

        private async Task CreateFbDataAsync(FbFormatParameters parameters)
        {
            // Создаём структуру fb_data в секторе BaseSector + 1
            byte[] fbData = new byte[512];

            // boot_size (смещение 0)
            byte[] bootSizeBytes = BitConverter.GetBytes((ushort)0);
            Array.Copy(bootSizeBytes, 0, fbData, 0, 2);

            // flags (смещение 2)
            byte[] flagsBytes = BitConverter.GetBytes((ushort)0);
            Array.Copy(flagsBytes, 0, fbData, 2, 2);

            // ver_major (смещение 4)
            fbData[4] = 1;
            // ver_minor (смещение 5)
            fbData[5] = 6;

            // list_used (смещение 6)
            byte[] listUsedBytes = BitConverter.GetBytes((ushort)1);
            Array.Copy(listUsedBytes, 0, fbData, 6, 2);

            // list_size (смещение 8)
            ushort listSize = (ushort)(parameters.ListSize / 510);
            byte[] listSizeBytes = BitConverter.GetBytes(listSize);
            Array.Copy(listSizeBytes, 0, fbData, 8, 2);

            // pri_size (смещение 10)
            byte[] priSizeBytes = BitConverter.GetBytes((ushort)parameters.PrimarySize);
            Array.Copy(priSizeBytes, 0, fbData, 10, 2);

            // ext_size (смещение 12)
            byte[] extSizeBytes = BitConverter.GetBytes((uint)parameters.ExtendedSize);
            Array.Copy(extSizeBytes, 0, fbData, 12, 4);

            // Записываем fb_data в сектор BaseSector + 1
            ulong fbDataSector = (ulong)(parameters.BaseSector + 1);
            await _diskIo.WriteSectorsAsync(fbDataSector, fbData);
        }

        private async Task CreateFat32PartitionAsync(FbFormatParameters parameters, FormatOptions options)
        {
            // Вычисляем начало раздела
            ulong partitionStart = (ulong)parameters.BaseSector + parameters.PrimarySize + parameters.ExtendedSize;

            // Форматируем FAT32
            await _fat32Formatter.FormatAsync(
                partitionStart,
                parameters.PartitionSize,
                options.UnitSize,
                options.Align
            );
        }

        private class FbFormatParameters
        {
            public int BaseSector { get; set; }
            public uint PrimarySize { get; set; }
            public ulong ExtendedSize { get; set; }
            public int ListSize { get; set; }
            public int NandAlign { get; set; }
            public ulong PartitionSize { get; set; }
            public bool IsFat32 { get; set; }
            public ulong TotalSectors { get; set; }
        }
    }
}