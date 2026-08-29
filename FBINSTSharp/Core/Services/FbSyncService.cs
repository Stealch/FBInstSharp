using FBINSTSharp.Core.Interfaces;
using System;
using System.Threading.Tasks;

namespace FBINSTSharp.Core.Services
{
    public class FbSyncService
    {
        private readonly IDiskIoService _diskIo;

        public FbSyncService(IDiskIoService diskIo)
        {
            _diskIo = diskIo ?? throw new ArgumentNullException(nameof(diskIo));
        }

        public async Task SyncAsync(int maxSectors = 0, bool chsMode = false, bool zip = false,
            int copyBpb = -1, int bpbSize = 0)
        {
            // 1. Читаем MBR (сектор 0)
            byte[] mbr = await _diskIo.ReadSectorsAsync(0, 1);
            if (mbr == null || mbr.Length < 512)
                throw new InvalidOperationException("Failed to read MBR");

            // 2. Проверяем fbinst
            uint fbMagic = BitConverter.ToUInt32(mbr, 0x1B4);
            if (fbMagic != 0x46424246)
                throw new InvalidOperationException("fbinst MBR not found");

            // 3. Находим первый раздел
            uint partitionStart = 0;
            for (int i = 0x1BE; i < 0x1FE; i += 16)
            {
                if (mbr[i + 4] != 0)
                {
                    partitionStart = BitConverter.ToUInt32(mbr, i + 8);
                    break;
                }
            }

            if (partitionStart == 0)
                throw new InvalidOperationException("No partition found");

            // 4. Читаем BPB из первого раздела
            byte[] bpb = await _diskIo.ReadSectorsAsync(partitionStart, 1);
            if (bpb == null || bpb.Length < 512)
                throw new InvalidOperationException("Failed to read BPB from partition");

            // 5. Получаем jmp_ofs из MBR
            byte jmpOfs = mbr[1];

            // 6. Копируем/сбрасываем/очищаем BPB
            if (copyBpb == 2) // --copy-bpb
            {
                // Копируем BPB из раздела в MBR
                Array.Copy(bpb, 2, mbr, 2, jmpOfs);

                // Корректируем параметры
                ushort bytesPerSector = BitConverter.ToUInt16(bpb, 11);
                if (bytesPerSector == 0)
                    bytesPerSector = 512;

                // Обновляем ReservedSectors
                ushort reservedSectors = BitConverter.ToUInt16(bpb, 14);
                if (reservedSectors > 0)
                {
                    reservedSectors = (ushort)((uint)reservedSectors + partitionStart);
                    byte[] rsBytes = BitConverter.GetBytes(reservedSectors);
                    Array.Copy(rsBytes, 0, mbr, 14, 2);
                }

                // Обновляем TotalSectors
                uint totalSectors = 0;
                try
                {
                    totalSectors = BitConverter.ToUInt32(bpb, 32);
                }
                catch
                {
                    totalSectors = BitConverter.ToUInt16(bpb, 19);
                }

                if (totalSectors == 0)
                    totalSectors = BitConverter.ToUInt16(bpb, 19);

                if (totalSectors > 0)
                {
                    totalSectors = (uint)(totalSectors + partitionStart);
                    if (totalSectors < 65536)
                    {
                        byte[] ts16Bytes = BitConverter.GetBytes((ushort)totalSectors);
                        Array.Copy(ts16Bytes, 0, mbr, 19, 2);
                        Array.Clear(mbr, 32, 4);
                    }
                    else
                    {
                        byte[] ts32Bytes = BitConverter.GetBytes(totalSectors);
                        Array.Copy(ts32Bytes, 0, mbr, 32, 4);
                        Array.Clear(mbr, 19, 2);
                    }
                }

                Array.Clear(mbr, 28, 4);
            }
            else if (copyBpb == 1) // --reset-bpb
            {
                Array.Clear(mbr, 2, jmpOfs);
                mbr[0x10] = 2;
                mbr[0x18] = 0x3F;
                mbr[0x1A] = 0xFF;
                mbr[0x24] = 0x80;
            }
            else if (copyBpb == 0) // --clear-bpb
            {
                Array.Clear(mbr, 2, jmpOfs);
            }

            // 7. Обновляем параметры CHS/zip
            if (maxSectors > 0 && maxSectors <= 127)
                mbr[0x1AD] = (byte)maxSectors;

            if (chsMode)
                mbr[0x1AD] |= 0x80;

            if (zip)
            {
                mbr[0x26] = 0x29;
                byte[] oemBytes = System.Text.Encoding.ASCII.GetBytes("MSWIN4.1");
                Array.Copy(oemBytes, 0, mbr, 3, 8);
            }

            // 8. Записываем обновлённый MBR на все сектора от 0 до boot_base
            ushort bootBase = BitConverter.ToUInt16(mbr, 0x1B2);
            for (uint i = 0; i <= bootBase; i++)
            {
                byte[] lbaBytes = BitConverter.GetBytes((ushort)i);
                Array.Copy(lbaBytes, 0, mbr, 0x1AE, 2);

                if (i > 0)
                {
                    for (int j = 0x1BE; j < 0x1FE; j += 16)
                    {
                        if (mbr[j + 4] != 0)
                        {
                            uint start = BitConverter.ToUInt32(mbr, j + 8);
                            if (start > 0)
                            {
                                start--;
                                byte[] startBytes = BitConverter.GetBytes(start);
                                Array.Copy(startBytes, 0, mbr, j + 8, 4);
                            }
                        }
                    }

                    UpdateChsAddresses(mbr);
                }

                await _diskIo.WriteSectorsAsync(i, mbr);
            }
        }

        private void UpdateChsAddresses(byte[] mbr)
        {
            for (int i = 0x1BE; i < 0x1FE; i += 16)
            {
                if (mbr[i + 4] != 0)
                {
                    uint start = BitConverter.ToUInt32(mbr, i + 8);
                    uint size = BitConverter.ToUInt32(mbr, i + 12);

                    byte[] chsStart = LbaToChs(start);
                    Array.Copy(chsStart, 0, mbr, i + 1, 3);

                    byte[] chsEnd = LbaToChs(start + size - 1);
                    Array.Copy(chsEnd, 0, mbr, i + 5, 3);
                }
            }
        }

        private byte[] LbaToChs(uint lba)
        {
            const byte spt = 63;
            const byte heads = 255;

            byte[] chs = new byte[3];
            uint tmp = lba / (spt * heads);
            chs[2] = (byte)(tmp & 0xFF);
            chs[1] = (byte)((lba % (spt * heads)) / spt);
            chs[0] = (byte)((lba % spt) + 1);

            return chs;
        }
    }
}