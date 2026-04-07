using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.AssembledComputer
{
    public sealed class QueryAssembledComputer : QueryEntityRequestBase<AssembledComputerDto>
    {
        public QueryAssembledComputer(string nomenclatureSeries, int? serviceRequestId = null)
            : base(ApiResources.AssembledComputers, nomenclatureSeries)
        {
            if (serviceRequestId.HasValue)
            {
                UrlParameters = new[] { (nameof(serviceRequestId), (object)serviceRequestId) };
            }
        }
    }
}