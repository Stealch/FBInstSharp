using FBINSTSharp.Core.Interfaces;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace FBINSTSharp.Core.Services
{
    public class Fat32FormatterService
    {
        private readonly IDiskIoService _diskIo;

        public Fat32FormatterService(IDiskIoService diskIo)
        {
            _diskIo = diskIo ?? throw new ArgumentNullException(nameof(diskIo));
        }

        public async Task FormatAsync(ulong startSector, ulong totalSectors, int unitSize = 0, bool align = false)
        {
            if (totalSectors < 65536)
                throw new ArgumentException($"Partition too small for FAT32: {totalSectors} sectors (minimum 65536)");

            if (totalSectors > 0xFFFFFFFFUL)
                throw new ArgumentException($"Partition too large for FAT32: {totalSectors} sectors (max 2^32-1)");

            var (SectorsPerTrack, TracksPerCylinder, BytesPerSector) = _diskIo.GetGeometry();
            uint bytesPerSector = BytesPerSector;

            var (sectorsPerCluster, reservedSectors, fatSize, totalClusters) =
                CalculateFatParameters(totalSectors, bytesPerSector, unitSize, align);

            byte[] bootSector = CreateBootSector(
                startSector, totalSectors, bytesPerSector,
                sectorsPerCluster, reservedSectors, fatSize);

            byte[] fsInfo = CreateFsInfo(totalClusters);
            byte[] firstFatSector = CreateFirstFatSector(bytesPerSector);

            await WriteFat32StructureAsync(startSector, bootSector, fsInfo, firstFatSector,
                bytesPerSector, reservedSectors, fatSize, sectorsPerCluster);
        }

        private (uint sectorsPerCluster, uint reservedSectors, uint fatSize, uint totalClusters)
            CalculateFatParameters(ulong totalSectors, uint bytesPerSector, int unitSize, bool align)
        {
            uint spc;
            if (unitSize > 0)
            {
                spc = (uint)unitSize;
            }
            else
            {
                ulong diskSizeMB = (totalSectors * bytesPerSector) / (1024 * 1024);
                if (diskSizeMB <= 512)
                    spc = 1;
                else if (diskSizeMB <= 8192)
                    spc = 8;
                else if (diskSizeMB <= 16384)
                    spc = 16;
                else if (diskSizeMB <= 32768)
                    spc = 32;
                else
                    spc = 64;
            }

            uint reservedSectors;
            uint calculatedFatSize;

            if (align)
            {
                const uint alignSize = 32;
                calculatedFatSize = GetFatSize(totalSectors, 2 * alignSize, spc, 2, bytesPerSector);
                reservedSectors = alignSize + alignSize - (uint)((alignSize + 2 * calculatedFatSize) % alignSize);
            }
            else
            {
                reservedSectors = 32;
                calculatedFatSize = GetFatSize(totalSectors, reservedSectors, spc, 2, bytesPerSector);
            }

            ulong userArea = totalSectors - reservedSectors - (2UL * calculatedFatSize);
            uint totalClusters = (uint)(userArea / spc);

            if (totalClusters < 65536)
                throw new InvalidOperationException($"FAT32 must have at least 65536 clusters (got {totalClusters})");

            if (totalClusters > 0x0FFFFFFF)
                throw new InvalidOperationException($"Too many clusters ({totalClusters}), try larger cluster size");

            return (spc, reservedSectors, calculatedFatSize, totalClusters);
        }

        private uint GetFatSize(ulong totalSectors, uint reservedSectors, uint sectorsPerCluster,
            uint numFats, uint bytesPerSector)
        {
            ulong fatElementSize = 4;
            ulong numerator = fatElementSize * (totalSectors - reservedSectors);
            ulong denominator = (ulong)sectorsPerCluster * bytesPerSector + fatElementSize * numFats;
            return (uint)(numerator / denominator + 1);
        }

        private byte[] CreateBootSector(ulong startSector, ulong totalSectors, uint bytesPerSector,
            uint sectorsPerCluster, uint reservedSectors, uint fatSize)
        {
            var boot = new Fat32BootSector
            {
                JumpBoot = new byte[] { 0xEB, 0x58, 0x90 },
                OemName = "MSWIN4.1",
                BytesPerSector = (ushort)bytesPerSector,
                SectorsPerCluster = (byte)sectorsPerCluster,
                ReservedSectors = (ushort)reservedSectors,
                NumFATs = 2,
                RootEntries = 0,
                TotalSectors16 = 0,
                Media = 0xF8,
                FATSize16 = 0,
                SectorsPerTrack = 63,
                NumberOfHeads = 255,
                HiddenSectors = (uint)startSector,
                TotalSectors32 = (uint)totalSectors,
                FATSize32 = fatSize,
                ExtFlags = 0,
                FSVersion = 0,
                RootCluster = 2,
                FSInfo = 1,
                BackupBootSector = 6,
                Reserved = new byte[12],
                DriveNumber = 0x80,
                Reserved1 = 0,
                BootSignature = 0x29,
                VolumeID = GetVolumeId(),
                VolumeLabel = "NO NAME    ",
                FileSystemType = "FAT32   ",
                BootSectorSignature = 0xAA55
            };

            int size = Marshal.SizeOf(boot);
            byte[] buffer = new byte[bytesPerSector];
            IntPtr ptr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(boot, ptr, false);
                Marshal.Copy(ptr, buffer, 0, size);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }

            if (bytesPerSector > 512)
            {
                buffer[bytesPerSector - 2] = 0x55;
                buffer[bytesPerSector - 1] = 0xAA;
            }

            return buffer;
        }

        private byte[] CreateFsInfo(uint totalClusters)
        {
            var fsInfo = new Fat32FsInfo
            {
                LeadSignature = 0x41615252,
                Reserved1 = new byte[480],
                StructureSignature = 0x61417272,
                FreeCount = totalClusters - 1,
                NextFree = 3,
                Reserved2 = new byte[12],
                TrailSignature = 0xAA550000
            };

            int size = Marshal.SizeOf(fsInfo);
            byte[] buffer = new byte[512];
            IntPtr ptr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(fsInfo, ptr, false);
                Marshal.Copy(ptr, buffer, 0, size);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }

            return buffer;
        }

        private byte[] CreateFirstFatSector(uint bytesPerSector)
        {
            byte[] buffer = new byte[bytesPerSector];
            buffer[0] = 0xF8;
            buffer[1] = 0xFF;
            buffer[2] = 0xFF;
            buffer[3] = 0x0F;
            buffer[4] = 0xFF;
            buffer[5] = 0xFF;
            buffer[6] = 0xFF;
            buffer[7] = 0x0F;
            buffer[8] = 0xFF;
            buffer[9] = 0xFF;
            buffer[10] = 0xFF;
            buffer[11] = 0x0F;
            return buffer;
        }

        private async Task WriteFat32StructureAsync(ulong startSector, byte[] bootSector, byte[] fsInfo,
            byte[] firstFatSector, uint bytesPerSector, uint reservedSectors, uint fatSize, uint sectorsPerCluster)
        {
            // 1. Обнуляем ВСЮ системную область: резервные сектора + FATы + корневой кластер
            // Это соответствует zero_sectors() в оригинальном fat32format.c
            ulong systemAreaSize = reservedSectors + 2 * fatSize + sectorsPerCluster;
            byte[] zeroSector = new byte[bytesPerSector];

            // Обнуляем большими блоками для производительности (по 128 секторов, как в оригинале)
            const uint burstSize = 128;
            for (ulong i = 0; i < systemAreaSize; i += burstSize)
            {
                ulong count = Math.Min(burstSize, systemAreaSize - i);
                for (ulong j = 0; j < count; j++)
                {
                    await _diskIo.WriteSectorsAsync(startSector + i + j, zeroSector);
                }
            }

            // 2. Записываем структуры (как в оригинале)
            // Sector 0: Boot Sector
            await _diskIo.WriteSectorsAsync(startSector, bootSector);

            // Sector 1: FSInfo
            await _diskIo.WriteSectorsAsync(startSector + 1, fsInfo);

            // Sectors 2-5: нули (уже обнулены)
            // Sector 6: Backup Boot Sector
            await _diskIo.WriteSectorsAsync(startSector + 6, bootSector);

            // Sector 7: Backup FSInfo
            await _diskIo.WriteSectorsAsync(startSector + 7, fsInfo);

            // Sectors 8 до reservedSectors-1: нули (уже обнулены)

            // 3. Записываем первые сектора FAT (остальные уже нули)
            for (uint fatIndex = 0; fatIndex < 2; fatIndex++)
            {
                ulong fatStart = startSector + reservedSectors + (ulong)fatIndex * fatSize;
                await _diskIo.WriteSectorsAsync(fatStart, firstFatSector);
                // Остальные сектора FAT уже нули
            }

            // 4. Корневой кластер (cluster 2) уже нули
        }

        private uint GetVolumeId()
        {
            var now = DateTime.Now;
            uint low = (uint)(now.Day + (now.Month << 8));
            uint tmp = (uint)((now.Millisecond / 10) + (now.Second << 8));
            low += tmp;
            uint high = (uint)(now.Minute + (now.Hour << 8));
            high += (uint)now.Year;
            return low + (high << 16);
        }

        #region Структуры

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        private struct Fat32BootSector
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public byte[] JumpBoot;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 8)]
            public string OemName;
            public ushort BytesPerSector;
            public byte SectorsPerCluster;
            public ushort ReservedSectors;
            public byte NumFATs;
            public ushort RootEntries;
            public ushort TotalSectors16;
            public byte Media;
            public ushort FATSize16;
            public ushort SectorsPerTrack;
            public ushort NumberOfHeads;
            public uint HiddenSectors;
            public uint TotalSectors32;
            public uint FATSize32;
            public ushort ExtFlags;
            public ushort FSVersion;
            public uint RootCluster;
            public ushort FSInfo;
            public ushort BackupBootSector;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
            public byte[] Reserved;
            public byte DriveNumber;
            public byte Reserved1;
            public byte BootSignature;
            public uint VolumeID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 11)]
            public string VolumeLabel;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 8)]
            public string FileSystemType;
            public ushort BootSectorSignature;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct Fat32FsInfo
        {
            public uint LeadSignature;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 480)]
            public byte[] Reserved1;
            public uint StructureSignature;
            public uint FreeCount;
            public uint NextFree;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
            public byte[] Reserved2;
            public uint TrailSignature;
        }

        #endregion
    }
}