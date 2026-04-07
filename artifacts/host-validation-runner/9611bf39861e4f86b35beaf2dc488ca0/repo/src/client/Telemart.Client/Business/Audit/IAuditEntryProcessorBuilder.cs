using System.Threading.Tasks;
using Telemart.Client.Business.Audit.Entry;

namespace Telemart.Client.Business.Audit
{
    public interface IAuditEntryProcessorBuilder
    {
        Task<IAuditEntryProcessor> BuildAsync();
    }
}
