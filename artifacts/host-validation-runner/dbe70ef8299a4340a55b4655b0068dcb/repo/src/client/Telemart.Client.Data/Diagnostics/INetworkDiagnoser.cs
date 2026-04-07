using System;
using System.Threading.Tasks;

namespace Telemart.Client.Data.Diagnostics
{
    public interface INetworkDiagnoser
    {
        event Action StateChanged;

        NetworkState DownloadSpeedNetworkState { get; }

        string ToolTip { get; }

        void SetDownloadTime(long bytes, double milliseconds);
    }
}