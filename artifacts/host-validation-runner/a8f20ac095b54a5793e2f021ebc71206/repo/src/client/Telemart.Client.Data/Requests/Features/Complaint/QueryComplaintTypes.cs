using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Complaint;

namespace Telemart.Client.Data.Requests.Features.Complaint
{
    public class QueryComplaintTypes : QueryEntitiesRequestBase<ComplaintTypeDto>
    {
        public QueryComplaintTypes()
            : base($"{ApiResources.Complaints}/types")
        {
        }
    }
}
