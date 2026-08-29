using FBINSTSharp.Core.Interfaces;
using System;
using System.Threading.Tasks;

namespace FBINSTSharp.Core.Services
{
    public class FbUpdateService
    {
        private readonly IDiskIoService _diskIo;

        public FbUpdateService(IDiskIoService diskIo)
        {
            _diskIo = diskIo ?? throw new ArgumentNullException(nameof(diskIo));
        }

        public async Task UpdateAsync()
        {
            // 1. Читаем MBR
            byte[] mbr = await _diskIo.ReadSectorsAsync(0, 1);
            if (mbr == null || mbr.Length < 512)
                throw new InvalidOperationException("Failed to read MBR");

            // 2. Проверяем fbinst
            uint fbMagic = BitConverter.ToUInt32(mbr, 0x1B4);
            if (fbMagic != 0x46424246)
                throw new InvalidOperationException("fbinst MBR not found");

            // 3. Получаем boot_base
            ushort bootBase = BitConverter.ToUInt16(mbr, 0x1B2);

            // 4. Получаем загрузочный код из ресурса
            byte[] bootCode = BootCodeProvider.GetBootCode();
            uint sectorsToWrite = (uint)(bootBase + 1);

            if (bootCode.Length < sectorsToWrite * 512)
                throw new InvalidOperationException($"Boot code too short: {bootCode.Length} bytes, need {sectorsToWrite * 512}");

            // 5. Записываем загрузочный код
            for (uint i = 0; i < sectorsToWrite; i++)
            {
                byte[] sectorData = new byte[512];
                int offset = (int)(i * 512);
                Array.Copy(bootCode, offset, sectorData, 0, 512);

                // Для первого сектора сохраняем таблицу разделов
                if (i == 0)
                {
                    Array.Copy(mbr, 0x1BE, sectorData, 0x1BE, 0x40);
                }

                await _diskIo.WriteSectorsAsync(i, sectorData);
            }

            // 6. Обновляем fb_data
            await UpdateFbDataAsync(bootBase);

            Console.WriteLine("Boot code updated successfully.");
        }

        private async Task UpdateFbDataAsync(ushort bootBase)
        {
            // Читаем текущую fb_data
            byte[] fbData = await _diskIo.ReadSectorsAsync((ulong)(bootBase + 1), 1);
            if (fbData == null || fbData.Length < 16)
                throw new InvalidOperationException("Failed to read fb_data");

            // Обновляем версию
            fbData[4] = 1; // ver_major
            fbData[5] = 6; // ver_minor

            // Записываем обратно
            await _diskIo.WriteSectorsAsync((ulong)(bootBase + 1), fbData);
        }
    }
}