namespace FBINSTSharp.Core.Models
{
    public class FormatOptions
    {
        public bool Force { get; set; }          // --force, -f
        public bool Raw { get; set; }            // --raw, -r
        public bool Zip { get; set; }            // --zip, -z
        public bool Align { get; set; }          // --align, -a
        public bool ChsMode { get; set; }        // --chs
        public bool IsFat32 { get; set; } = true; // --fat32 (default), --fat16
        public bool IsFat16 { get; set; }        // --fat16

        public ulong PartitionSize { get; set; }     // --size, -s (в секторах)
        public ulong PrimarySize { get; set; }       // --primary, -p (в секторах)
        public ulong ExtendedSize { get; set; }      // --extended, -e (в секторах)
        public int BaseSector { get; set; } = 63;    // --base, -b
        public int ListSize { get; set; }            // --list-size, -l (в байтах)
        public int UnitSize { get; set; }            // --unit-size, -u (в секторах)
        public int NandAlign { get; set; } = 255;    // --nalign, -n
        public int MaxSectors { get; set; }          // --max-sectors
        public string ArchiveFile { get; set; }      // --archive

        public string DevicePath { get; set; }       // (hdX) или \\.\PHYSICALDRIVEX

        /// <summary>
        /// Признак того, что пользователь явно указал --fat32 или --fat16.
        /// Если false — определяется автоматически по размеру.
        /// </summary>
        public bool IsFileSystemExplicit { get; set; }

        public FormatOptions()
        {
            // Значения по умолчанию из оригинального fbinst.c
            BaseSector = 63;
            NandAlign = 255;
            IsFat32 = true;
            PrimarySize = 63UL * 256; // MIN_PRI_SIZE
            ListSize = 0; // будет рассчитан позже
        }
    }
}