using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Cronicle;

namespace Telemart.Client.Data.Requests.Features.Cronicle
{
    public sealed class AliveSessionCronicle : CallActionWithBodyRequestResultBase<AliveSessionCronicleDto, CronicleSessionDto>
    {
        public AliveSessionCronicle(string sessionId)
            : base(new CronicleSessionDto { SessionId = sessionId }, ApiResources.Cronicle, "alive")
        {
        }
    }
}