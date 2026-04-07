using System.Collections.Generic;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Business.Audit.Entry
{
    public interface IAuditEntryProcessor
    {
        IEnumerable<AuditEntry> Process(IEnumerable<AuditEntryDto> entries);
    }
}
