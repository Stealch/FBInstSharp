using System;
using System.Reflection;

namespace FBINSTSharp.Core.Services
{
    public static class BootCodeProvider
    {
        private static byte[] _bootCode;

        public static byte[] GetBootCode()
        {
            if (_bootCode != null)
                return _bootCode;

            var assembly = Assembly.GetExecutingAssembly();
            // Имя ресурса: namespace + папка + имя_файла
            string resourceName = "FBINSTSharp.Resources.boot.bsf";

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    throw new InvalidOperationException(
                        $"Boot code resource '{resourceName}' not found. " +
                        "Make sure boot.bsf is embedded as resource in the Resources folder.");

                _bootCode = new byte[stream.Length];
                stream.Read(_bootCode, 0, _bootCode.Length);
            }

            return _bootCode;
        }
    }
}