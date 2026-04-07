using System.Diagnostics;

namespace Telemart.Client.Core.Helpers
{
    public static class ProcessHelper
    {
        public static void Start(string uri)
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo(uri);

            processStartInfo.UseShellExecute = true;

            Process.Start(processStartInfo);
        }
    }
}