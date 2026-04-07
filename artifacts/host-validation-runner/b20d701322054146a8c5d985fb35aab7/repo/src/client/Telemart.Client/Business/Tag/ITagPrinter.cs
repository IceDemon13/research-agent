using System.Collections.Generic;
using System.Threading.Tasks;

namespace Telemart.Client.Business.Tag
{
    public interface ITagPrinter
    {
        Task PrintAsync(IReadOnlyCollection<TagPrintInfo> tagPrintInfos);
    }
}