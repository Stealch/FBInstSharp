using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using FBINSTSharp.Core.Interfaces;
using Microsoft.Win32.SafeHandles;

namespace FBINSTSharp.Core.Services
{
    public class DiskIoService : IDiskIoService
    {
        private SafeFileHandle _handle;
        private string _devicePath;
        private bool _disposed;
        private ulong _totalSectors;
        private uint _bytesPerSector;
        private uint _sectorsPerTrack;
        private uint _tracksPerCylinder;

        // Константы Windows API
        private const uint GENERIC_READ = 0x80000000;
        private const uint GENERIC_WRITE = 0x40000000;
        private const uint FILE_SHARE_READ = 0x00000001;
        private const uint FILE_SHARE_WRITE = 0x00000002;
        private const uint OPEN_EXISTING = 3;
        private const uint FILE_FLAG_NO_BUFFERING = 0x20000000;
        private const uint FILE_ATTRIBUTE_NORMAL = 0x80;
        private const uint FSCTL_LOCK_VOLUME = 0x00090018;
        private const uint FSCTL_UNLOCK_VOLUME = 0x0009001C;
        private const uint FSCTL_DISMOUNT_VOLUME = 0x00090020;
        private const uint IOCTL_DISK_GET_DRIVE_GEOMETRY = 0x00070040;
        private const uint IOCTL_DISK_GET_LENGTH_INFO = 0x0007405C;
        private const uint IOCTL_STORAGE_GET_DEVICE_NUMBER = 0x002D1080;

        // Константы для SetupAPI
        private const uint DIGCF_ALLCLASSES = 0x00000004;
        private const uint DIGCF_PRESENT = 0x00000002;
        private const uint SPDRP_REMOVABLE = 0x0000001B;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern SafeFileHandle CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool DeviceIoControl(
            SafeFileHandle hDevice,
            uint dwIoControlCode,
            IntPtr lpInBuffer,
            uint nInBufferSize,
            IntPtr lpOutBuffer,
            uint nOutBufferSize,
            out uint lpBytesReturned,
            IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadFile(
            SafeFileHandle hFile,
            byte[] lpBuffer,
            uint nNumberOfBytesToRead,
            out uint lpNumberOfBytesRead,
            IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteFile(
            SafeFileHandle hFile,
            byte[] lpBuffer,
            uint nNumberOfBytesToWrite,
            out uint lpNumberOfBytesWritten,
            IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetFilePointerEx(
            SafeFileHandle hFile,
            long liDistanceToMove,
            out long lpNewFilePointer,
            uint dwMoveMethod);

        // P/Invoke для SetupAPI
        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern IntPtr SetupDiGetClassDevs(
            IntPtr ClassGuid,
            string Enumerator,
            IntPtr hwndParent,
            uint Flags);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiEnumDeviceInfo(
            IntPtr DeviceInfoSet,
            uint MemberIndex,
            ref SP_DEVINFO_DATA DeviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiGetDeviceInstanceId(
            IntPtr DeviceInfoSet,
            ref SP_DEVINFO_DATA DeviceInfoData,
            byte[] DeviceInstanceId,
            uint DeviceInstanceIdSize,
            out uint RequiredSize);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiGetDeviceRegistryProperty(
            IntPtr DeviceInfoSet,
            ref SP_DEVINFO_DATA DeviceInfoData,
            uint Property,
            out uint PropertyRegDataType,
            byte[] PropertyBuffer,
            uint PropertyBufferSize,
            out uint RequiredSize);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiDestroyDeviceInfoList(
            IntPtr DeviceInfoSet);

        [StructLayout(LayoutKind.Sequential)]
        private struct SP_DEVINFO_DATA
        {
            public int cbSize;
            public Guid ClassGuid;
            public uint DevInst;
            public IntPtr Reserved;
        }

        public bool Open(string devicePath)
        {
            if (string.IsNullOrEmpty(devicePath))
                throw new ArgumentException("Device path cannot be null or empty", nameof(devicePath));

            string windowsPath = devicePath;

            if (devicePath.StartsWith("(hd", StringComparison.OrdinalIgnoreCase) && devicePath.EndsWith(")"))
            {
                string numPart = devicePath.Substring(3, devicePath.Length - 4);
                if (int.TryParse(numPart, out int diskNumber))
                {
                    windowsPath = $@"\\.\PHYSICALDRIVE{diskNumber}";
                }
                else
                {
                    throw new ArgumentException($"Invalid device format: {devicePath}");
                }
            }
            else if (devicePath.StartsWith("\\\\.\\"))
            {
                windowsPath = devicePath;
            }
            else
            {
                throw new ArgumentException($"Unsupported device format: {devicePath}");
            }

            _devicePath = windowsPath;

            _handle = CreateFile(
                windowsPath,
                GENERIC_READ | GENERIC_WRITE,
                FILE_SHARE_READ | FILE_SHARE_WRITE,
                IntPtr.Zero,
                OPEN_EXISTING,
                FILE_FLAG_NO_BUFFERING | FILE_ATTRIBUTE_NORMAL,
                IntPtr.Zero);

            if (_handle == null || _handle.IsInvalid)
            {
                int error = Marshal.GetLastWin32Error();
                throw new Win32Exception(error, $"Failed to open device {windowsPath} (error {error})");
            }

            if (!GetDriveGeometry())
            {
                Close();
                throw new InvalidOperationException($"Failed to get geometry for {windowsPath}");
            }

            return true;
        }

        private bool GetDriveGeometry()
        {
            byte[] lenBuffer = new byte[8];
            uint bytesReturned;

            bool result = DeviceIoControl(
                _handle,
                IOCTL_DISK_GET_LENGTH_INFO,
                IntPtr.Zero,
                0,
                Marshal.UnsafeAddrOfPinnedArrayElement(lenBuffer, 0),
                (uint)lenBuffer.Length,
                out bytesReturned,
                IntPtr.Zero);

            if (!result)
                return false;

            long lengthInBytes = BitConverter.ToInt64(lenBuffer, 0);
            if (lengthInBytes <= 0)
                return false;

            byte[] geomBuffer = new byte[24];
            result = DeviceIoControl(
                _handle,
                IOCTL_DISK_GET_DRIVE_GEOMETRY,
                IntPtr.Zero,
                0,
                Marshal.UnsafeAddrOfPinnedArrayElement(geomBuffer, 0),
                (uint)geomBuffer.Length,
                out bytesReturned,
                IntPtr.Zero);

            if (result)
            {
                _tracksPerCylinder = BitConverter.ToUInt32(geomBuffer, 12);
                _sectorsPerTrack = BitConverter.ToUInt32(geomBuffer, 16);
                _bytesPerSector = BitConverter.ToUInt32(geomBuffer, 20);
            }
            else
            {
                _bytesPerSector = 512;
                _sectorsPerTrack = 63;
                _tracksPerCylinder = 255;
            }

            if (_bytesPerSector == 0)
                _bytesPerSector = 512;

            _totalSectors = (ulong)(lengthInBytes / _bytesPerSector);
            return true;
        }

        public void Close()
        {
            if (_handle != null && !_handle.IsInvalid)
            {
                _handle.Close();
                _handle = null;
            }
        }

        public ulong GetTotalSectors() => _totalSectors;

        public (uint SectorsPerTrack, uint TracksPerCylinder, uint BytesPerSector) GetGeometry()
        {
            return (_sectorsPerTrack, _tracksPerCylinder, _bytesPerSector);
        }

        public byte[] ReadSectors(ulong startSector, uint sectorCount)
        {
            if (_handle == null || _handle.IsInvalid)
                throw new InvalidOperationException("Device not open");

            if (startSector + sectorCount > _totalSectors)
                throw new ArgumentOutOfRangeException(nameof(sectorCount), "Sector range exceeds disk size");

            uint bytesToRead = sectorCount * _bytesPerSector;
            byte[] buffer = new byte[bytesToRead];

            long offset = (long)(startSector * _bytesPerSector);
            if (!SetFilePointerEx(_handle, offset, out _, 0))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to seek");

            if (!ReadFile(_handle, buffer, bytesToRead, out uint bytesRead, IntPtr.Zero))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to read sector(s)");

            if (bytesRead != bytesToRead)
                throw new InvalidOperationException($"Read {bytesRead} bytes, expected {bytesToRead}");

            return buffer;
        }

        public async Task<byte[]> ReadSectorsAsync(ulong startSector, uint sectorCount)
        {
            return await Task.Run(() => ReadSectors(startSector, sectorCount));
        }

        public void WriteSectors(ulong startSector, byte[] data)
        {
            if (_handle == null || _handle.IsInvalid)
                throw new InvalidOperationException("Device not open");

            if (data.Length % _bytesPerSector != 0)
                throw new ArgumentException($"Data length ({data.Length}) must be multiple of sector size ({_bytesPerSector})");

            uint sectorCount = (uint)(data.Length / _bytesPerSector);
            if (startSector + sectorCount > _totalSectors)
                throw new ArgumentOutOfRangeException(nameof(startSector), "Data range exceeds disk size");

            long offset = (long)(startSector * _bytesPerSector);
            if (!SetFilePointerEx(_handle, offset, out _, 0))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to seek");

            if (!WriteFile(_handle, data, (uint)data.Length, out uint bytesWritten, IntPtr.Zero))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to write sector(s)");

            if (bytesWritten != data.Length)
                throw new InvalidOperationException($"Wrote {bytesWritten} bytes, expected {data.Length}");
        }

        public async Task WriteSectorsAsync(ulong startSector, byte[] data)
        {
            await Task.Run(() => WriteSectors(startSector, data));
        }

        public bool LockVolume()
        {
            if (_handle == null || _handle.IsInvalid)
                return false;

            uint bytesReturned;
            return DeviceIoControl(_handle, FSCTL_LOCK_VOLUME, IntPtr.Zero, 0, IntPtr.Zero, 0, out bytesReturned, IntPtr.Zero);
        }

        public bool UnlockVolume()
        {
            if (_handle == null || _handle.IsInvalid)
                return false;

            uint bytesReturned;
            return DeviceIoControl(_handle, FSCTL_UNLOCK_VOLUME, IntPtr.Zero, 0, IntPtr.Zero, 0, out bytesReturned, IntPtr.Zero);
        }

        public bool DismountVolume()
        {
            if (_handle == null || _handle.IsInvalid)
                return false;

            uint bytesReturned;
            return DeviceIoControl(_handle, FSCTL_DISMOUNT_VOLUME, IntPtr.Zero, 0, IntPtr.Zero, 0, out bytesReturned, IntPtr.Zero);
        }

        public bool IsRemovable
        {
            get
            {
                if (_handle == null || _handle.IsInvalid)
                    return false;

                try
                {
                    var deviceNumber = GetDeviceNumber(_handle);
                    if (deviceNumber == null)
                        return false;

                    string devicePath = GetDevicePath(deviceNumber.Value.DeviceType, deviceNumber.Value.DeviceNumber);
                    if (string.IsNullOrEmpty(devicePath))
                        return false;

                    return IsDeviceRemovable(devicePath);
                }
                catch
                {
                    return false;
                }
            }
        }

        private struct StorageDeviceNumber
        {
            public uint DeviceType;
            public uint DeviceNumber;
            public uint PartitionNumber;
        }

        private StorageDeviceNumber? GetDeviceNumber(SafeFileHandle handle)
        {
            byte[] buffer = new byte[12];
            uint bytesReturned;

            bool result = DeviceIoControl(
                handle,
                IOCTL_STORAGE_GET_DEVICE_NUMBER,
                IntPtr.Zero,
                0,
                Marshal.UnsafeAddrOfPinnedArrayElement(buffer, 0),
                (uint)buffer.Length,
                out bytesReturned,
                IntPtr.Zero);

            if (!result || bytesReturned < 12)
                return null;

            return new StorageDeviceNumber
            {
                DeviceType = BitConverter.ToUInt32(buffer, 0),
                DeviceNumber = BitConverter.ToUInt32(buffer, 4),
                PartitionNumber = BitConverter.ToUInt32(buffer, 8)
            };
        }

        private string GetDevicePath(uint deviceType, uint deviceNumber)
        {
            IntPtr deviceInfoSet = SetupDiGetClassDevs(
                IntPtr.Zero,
                null,
                IntPtr.Zero,
                DIGCF_ALLCLASSES | DIGCF_PRESENT);

            if (deviceInfoSet == IntPtr.Zero)
                return null;

            try
            {
                SP_DEVINFO_DATA deviceInfoData = new SP_DEVINFO_DATA();
                deviceInfoData.cbSize = Marshal.SizeOf(deviceInfoData);
                uint index = 0;

                while (SetupDiEnumDeviceInfo(deviceInfoSet, index, ref deviceInfoData))
                {
                    index++;

                    string path = GetDevicePathFromDevInfo(deviceInfoSet, ref deviceInfoData);
                    if (string.IsNullOrEmpty(path))
                        continue;

                    if (IsMatchingDevice(path, deviceType, deviceNumber))
                        return path;
                }
            }
            finally
            {
                SetupDiDestroyDeviceInfoList(deviceInfoSet);
            }

            return null;
        }

        private bool IsMatchingDevice(string devicePath, uint deviceType, uint deviceNumber)
        {
            using (SafeFileHandle tempHandle = CreateFile(
                devicePath,
                0,
                FILE_SHARE_READ | FILE_SHARE_WRITE,
                IntPtr.Zero,
                OPEN_EXISTING,
                0,
                IntPtr.Zero))
            {
                if (tempHandle == null || tempHandle.IsInvalid)
                    return false;

                var number = GetDeviceNumber(tempHandle);
                if (number == null)
                    return false;

                return number.Value.DeviceType == deviceType &&
                       number.Value.DeviceNumber == deviceNumber;
            }
        }

        private bool IsDeviceRemovable(string devicePath)
        {
            IntPtr deviceInfoSet = SetupDiGetClassDevs(
                IntPtr.Zero,
                null,
                IntPtr.Zero,
                DIGCF_ALLCLASSES | DIGCF_PRESENT);

            if (deviceInfoSet == IntPtr.Zero)
                return false;

            try
            {
                SP_DEVINFO_DATA deviceInfoData = new SP_DEVINFO_DATA();
                deviceInfoData.cbSize = Marshal.SizeOf(deviceInfoData);
                uint index = 0;

                while (SetupDiEnumDeviceInfo(deviceInfoSet, index, ref deviceInfoData))
                {
                    index++;

                    string path = GetDevicePathFromDevInfo(deviceInfoSet, ref deviceInfoData);
                    if (string.IsNullOrEmpty(path) || path != devicePath)
                        continue;

                    byte[] buffer = new byte[256];
                    uint propertyRegDataType;
                    uint requiredSize;

                    if (SetupDiGetDeviceRegistryProperty(
                        deviceInfoSet,
                        ref deviceInfoData,
                        SPDRP_REMOVABLE,
                        out propertyRegDataType,
                        buffer,
                        (uint)buffer.Length,
                        out requiredSize))
                    {
                        return BitConverter.ToBoolean(buffer, 0);
                    }

                    break;
                }
            }
            finally
            {
                SetupDiDestroyDeviceInfoList(deviceInfoSet);
            }

            return false;
        }

        private string GetDevicePathFromDevInfo(IntPtr deviceInfoSet, ref SP_DEVINFO_DATA deviceInfoData)
        {
            byte[] buffer = new byte[512];
            uint requiredSize;

            if (!SetupDiGetDeviceInstanceId(
                deviceInfoSet,
                ref deviceInfoData,
                buffer,
                (uint)buffer.Length,
                out requiredSize))
            {
                return null;
            }

            string instanceId = System.Text.Encoding.Unicode.GetString(buffer).TrimEnd('\0');
            return $@"\\.\{instanceId}";
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                Close();
            }

            _disposed = true;
        }
    }
}