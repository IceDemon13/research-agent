using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Cronicle;

namespace Telemart.Client.Data.Requests.Features.Cronicle
{
    public sealed class GetCronicleSeesion : CallActionRequestResultBase<CronicleSessionDto>
    {
        public GetCronicleSeesion()
            : base(ApiResources.Cronicle, "session")
        {
        }
    }
}