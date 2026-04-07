using System;
using System.Threading.Tasks;

namespace Telemart.Client.Core.Update
{
    public interface IUpdateManager
    {
        Task<WinCheckForUpdateResult?> CheckIfUpdateAvailableAsync();

        Task UpdateAsync(IProgress<int> progress);

        Task RunLatestVersionAsync();

        Version GetCurrentVersion();

        Version GetCurrentAssemblyVersion();
    }
}