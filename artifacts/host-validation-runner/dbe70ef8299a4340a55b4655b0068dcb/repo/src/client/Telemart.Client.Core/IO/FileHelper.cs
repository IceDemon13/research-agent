using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Telemart.Client.Core.Helpers;

namespace Telemart.Client.Core.IO
{
    [SuppressMessage("ReSharper", "ExceptionNotDocumented", Justification = "Not needed")]
    public static class FileHelper
    {
        private const int BufferSize = 4096;

        public static async Task<byte[]> ReadBytesAsync(string filePath)
        {
            using (FileStream fileStream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                BufferSize,
                FileOptions.Asynchronous))
            {
                byte[] buffer = new byte[fileStream.Length];
                await fileStream.ReadAsync(buffer, 0, buffer.Length);
                return buffer;
            }
        }

        public static async Task WriteBytesAsync(string filePath, byte[] data)
        {
            using (FileStream fileStream = File.Create(filePath, BufferSize, FileOptions.Asynchronous))
            {
                await fileStream.WriteAsync(data, 0, data.Length);
            }
        }

        public static async Task OpenAsFileAsync(byte[] data, string ext)
        {
            string directoryPah = Path.Combine(ApplicationFolders.LocalApplicationData, "print");

            if (!Directory.Exists(directoryPah))
            {
                Directory.CreateDirectory(directoryPah);
            }

            string tempFilePath = Path.Combine(directoryPah, $"{Guid.NewGuid():N}.{ext}");

            await WriteBytesAsync(tempFilePath, data);

            ProcessHelper.Start(tempFilePath);
        }

        public static async Task<byte[]> ReadBytesFromUrlAsync(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return null;
            }

            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    return await httpClient.GetByteArrayAsync(url);
                }
            }
            catch
            {
            }

            return null;
        }
    }
}