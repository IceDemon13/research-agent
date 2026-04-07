using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;

namespace Telemart.Client.Data.Requests.Features.PrivatBank
{
    public sealed class RemoveSession : CallActionWithBodyRequestBase<object, RemoveSession.PrivatBankRemoveSessionRequest>
    {
        public RemoveSession(string sessionId)
            : base(new PrivatBankRemoveSessionRequest(sessionId), "pb", "removeSession")
        {
        }

        public class PrivatBankRemoveSessionRequest
        {
            public PrivatBankRemoveSessionRequest(string sessionId)
            {
                SessionId = sessionId;
            }

            [JsonProperty("session_id")]
            public string SessionId { get; set; }
        }
    }
}