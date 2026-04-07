using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Complaint;

namespace Telemart.Client.Data.Requests.Features.Complaint
{
    public class QueryComplaint : QueryEntityRequestBase<ComplaintDto>
    {
        public QueryComplaint(int id)
            : base(ApiResources.Complaints, id)
        {
        }
    }
}