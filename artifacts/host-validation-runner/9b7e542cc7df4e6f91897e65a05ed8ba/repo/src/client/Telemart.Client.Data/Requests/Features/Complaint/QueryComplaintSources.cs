using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Complaint;

namespace Telemart.Client.Data.Requests.Features.Complaint
{
    public sealed class QueryComplaintSources : QueryEntitiesRequestBase<ComplaintSourceDto>
    {
        public QueryComplaintSources()
            : base($"{ApiResources.Complaints}/sources")
        {
        }
    }
}