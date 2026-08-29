using FBINSTSharp.Core.Interfaces;
using System;
using System.Threading.Tasks;

namespace FBINSTSharp.Core.Services
{
    public class FbRestoreService
    {
        private readonly IDiskIoService _diskIo;

        public FbRestoreService(IDiskIoService diskIo)
        {
            _diskIo = diskIo ?? throw new ArgumentNullException(nameof(diskIo));
        }

        public async Task RestoreAsync()
        {
            // 1. Ищем резервную копию MBR (оригинальный fbinst ищет до DEF_BASE_SIZE = 63)
            const int MAX_BASE_SECTOR = 63;
            byte[] backupMbr = null;
            int foundBase = -1;

            for (int i = 0; i <= MAX_BASE_SECTOR; i++)
            {
                byte[] mbr = await _diskIo.ReadSectorsAsync((ulong)i, 1);
                if (mbr == null || mbr.Length < 512)
                    continue;

                // Проверяем сигнатуру MBR
                ushort signature = BitConverter.ToUInt16(mbr, 0x1FE);
                if (signature != 0xAA55)
                    continue;

                // Проверяем fb_magic
                uint fbMagic = BitConverter.ToUInt32(mbr, 0x1B4);
                if (fbMagic == 0x46424246)
                {
                    // Проверяем LBA (должен быть равен i)
                    ushort lba = BitConverter.ToUInt16(mbr, 0x1AE);
                    if (lba == i)
                    {
                        backupMbr = mbr;
                        foundBase = i;
                        break;
                    }
                }
            }

            if (backupMbr == null || foundBase == -1)
                throw new InvalidOperationException("No valid fbinst MBR backup found");

            // 2. Читаем текущий MBR (сектор 0)
            byte[] currentMbr = await _diskIo.ReadSectorsAsync(0, 1);
            if (currentMbr == null || currentMbr.Length < 512)
                throw new InvalidOperationException("Failed to read current MBR");

            // 3. Копируем таблицу разделов из резервной копии в текущий MBR
            // (смещение 0x1BE - 0x1FD)
            Array.Copy(backupMbr, 0x1BE, currentMbr, 0x1BE, 0x40);

            // 4. Копируем fb_magic и boot_base из резервной копии
            Array.Copy(backupMbr, 0x1B4, currentMbr, 0x1B4, 4);
            Array.Copy(backupMbr, 0x1B2, currentMbr, 0x1B2, 2);

            // 5. Обновляем LBA в MBR (устанавливаем 0)
            byte[] lbaBytes = BitConverter.GetBytes((ushort)0);
            Array.Copy(lbaBytes, 0, currentMbr, 0x1AE, 2);

            // 6. Обновляем CHS адреса в таблице разделов
            UpdateChsAddresses(currentMbr);

            // 7. Записываем восстановленный MBR
            await _diskIo.WriteSectorsAsync(0, currentMbr);

            // 8. Синхронизируем изменения
            _diskIo.DismountVolume();
        }

        private void UpdateChsAddresses(byte[] mbr)
        {
            for (int i = 0x1BE; i < 0x1FE; i += 16)
            {
                if (mbr[i + 4] != 0)
                {
                    uint start = BitConverter.ToUInt32(mbr, i + 8);
                    uint size = BitConverter.ToUInt32(mbr, i + 12);

                    // CHS для начала раздела
                    byte[] chsStart = LbaToChs(start);
                    Array.Copy(chsStart, 0, mbr, i + 1, 3);

                    // CHS для конца раздела
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