using System;
using System.IO;
using System.IO.Abstractions;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Telemart.Client.Core.IO
{
    public sealed class FileDownloader : IFileDownloader
    {
        private readonly IFileSystem fileSystem;

        public FileDownloader(IFileSystem fileSystem, ILogger<FileDownloader> logger)
        {
            this.fileSystem = fileSystem;
            Logger = logger;
        }

        private ILogger Logger { get; }

        public async Task DownloadFileAsync(string link, string targetFilePath)
        {
            string downloadFolderPath = fileSystem.Path.GetDirectoryName(targetFilePath);
            string downloadFileName = fileSystem.Path.GetFileName(targetFilePath);

            if (!fileSystem.Directory.Exists(downloadFolderPath))
            {
                fileSystem.Directory.CreateDirectory(downloadFolderPath);
            }

            string tempFilePath = fileSystem.Path.Combine(downloadFolderPath, $"_{downloadFileName}");

            using (HttpClient httpClient = new HttpClient())
            using (FileStream fs = new FileStream(tempFilePath, FileMode.OpenOrCreate))
            {
                Stream stream = await httpClient.GetStreamAsync(link);

                await stream.CopyToAsync(fs);
            }

            fileSystem.File.Move(tempFilePath, targetFilePath);
        }

        public async Task<bool> DownloadExeFileAsync(string link, string exeFileName)
        {
            try
            {
                if (File.Exists(exeFileName))
                {
                    File.Delete(exeFileName);
                }

                using (HttpClient httpClient = new HttpClient())
                {
                    byte[] bytes = await httpClient.GetByteArrayAsync(link);

                    File.WriteAllBytes(exeFileName, bytes);

                    return true;
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Failed download exe file");
            }

            return false;
        }
    }
}