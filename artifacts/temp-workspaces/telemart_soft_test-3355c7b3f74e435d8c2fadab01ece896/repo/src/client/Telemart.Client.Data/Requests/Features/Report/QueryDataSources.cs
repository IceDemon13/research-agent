using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.Requests.Features.Report.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Report
{
    public sealed class QueryDataSources : QueryEntitiesRequestBase<DataSourceDto>
    {
        public QueryDataSources()
            : base("datasources")
        {
        }
    }
}