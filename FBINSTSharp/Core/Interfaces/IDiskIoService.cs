using System;
using System.Threading.Tasks;

namespace FBINSTSharp.Core.Interfaces
{
    public interface IDiskIoService : IDisposable
    {
        /// <summary>
        /// Открывает устройство для работы.
        /// </summary>
        /// <param name="devicePath">Путь вида \\.\PHYSICALDRIVE1 или (hd1)</param>
        /// <returns>true если успешно</returns>
        bool Open(string devicePath);

        /// <summary>
        /// Закрывает устройство.
        /// </summary>
        void Close();

        /// <summary>
        /// Получает размер диска в секторах.
        /// </summary>
        ulong GetTotalSectors();

        /// <summary>
        /// Получает геометрию диска (сектора на дорожку, головки).
        /// </summary>
        (uint SectorsPerTrack, uint TracksPerCylinder, uint BytesPerSector) GetGeometry();

        /// <summary>
        /// Читает один или несколько секторов.
        /// </summary>
        byte[] ReadSectors(ulong startSector, uint sectorCount);

        /// <summary>
        /// Асинхронно читает секторы.
        /// </summary>
        Task<byte[]> ReadSectorsAsync(ulong startSector, uint sectorCount);

        /// <summary>
        /// Записывает данные в секторы.
        /// </summary>
        void WriteSectors(ulong startSector, byte[] data);

        /// <summary>
        /// Асинхронно записывает данные в секторы.
        /// </summary>
        Task WriteSectorsAsync(ulong startSector, byte[] data);

        /// <summary>
        /// Проверяет, является ли устройство съёмным.
        /// </summary>
        bool IsRemovable { get; }

        /// <summary>
        /// Блокирует том для эксклюзивного доступа.
        /// </summary>
        bool LockVolume();

        /// <summary>
        /// Разблокирует том.
        /// </summary>
        bool UnlockVolume();

        /// <summary>
        /// Размонтирует том (для применения изменений).
        /// </summary>
        bool DismountVolume();
    }
}