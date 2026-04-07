using System.Threading.Tasks;

namespace Telemart.Client.Common
{
    public interface ITelemartClientLogger
    {
        Task SendLogsAsync(string problem, bool automaticSend = false, string link = null);

        Task SendLogsAsync(string problem, string fileSuffix, bool automaticSend = false, string link = null);
    }
}