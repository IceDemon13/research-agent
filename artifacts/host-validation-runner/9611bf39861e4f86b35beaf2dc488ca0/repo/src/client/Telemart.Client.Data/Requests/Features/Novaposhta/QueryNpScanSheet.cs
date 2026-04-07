using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Novaposhta;

namespace Telemart.Client.Data.Requests.Features.Novaposhta
{
    public sealed class QueryNpScanSheet : QueryEntityRequestBase<NpScanSheetDto>
    {
        public QueryNpScanSheet(int id)
            : base(ApiResources.ScanSheets, "np", id)
        {
        }
    }
}