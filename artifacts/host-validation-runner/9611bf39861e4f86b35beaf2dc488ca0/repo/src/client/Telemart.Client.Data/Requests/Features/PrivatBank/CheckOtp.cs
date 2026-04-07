using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;

namespace Telemart.Client.Data.Requests.Features.PrivatBank
{
    public sealed class CheckOtp : CallActionWithBodyRequestResultBase<PrivatBankCreateSessionResponse, CheckOtp.PrivatBankCheckOtpRequest>
    {
        public CheckOtp(string sessionId, string otp)
            : base(new PrivatBankCheckOtpRequest(sessionId, otp), "pb", "checkOtp")
        {
        }

        public class PrivatBankCheckOtpRequest
        {
            public PrivatBankCheckOtpRequest(string sessionId, string otp)
            {
                SessionId = sessionId;
                Otp = otp;
            }

            [JsonProperty("session_id")]
            public string SessionId { get; set; }

            [JsonProperty("otp")]
            public string Otp { get; set; }
        }
    }
}