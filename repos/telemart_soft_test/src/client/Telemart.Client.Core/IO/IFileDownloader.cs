using System;
using System.Threading.Tasks;

namespace Telemart.Client.Core.IO
{
    public interface IFileDownloader
    {
        Task DownloadFileAsync(string link, string targetFilePath);

        Task<bool> DownloadExeFileAsync(string link, string exeFileName);
    }
}