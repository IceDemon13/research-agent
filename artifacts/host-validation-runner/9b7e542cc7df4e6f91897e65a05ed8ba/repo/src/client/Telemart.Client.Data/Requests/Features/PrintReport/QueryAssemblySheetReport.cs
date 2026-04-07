using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Assembly;

namespace Telemart.Client.Data.Requests.Features.PrintReport
{
    public sealed class QueryAssemblySheetReport : QueryEntityRequestBase<AssemblySheetReportDataDto>
    {
        public QueryAssemblySheetReport(int assemblyId)
            : base(ApiResources.PrintReport, "assembly_sheet", assemblyId)
        {
        }
    }
}