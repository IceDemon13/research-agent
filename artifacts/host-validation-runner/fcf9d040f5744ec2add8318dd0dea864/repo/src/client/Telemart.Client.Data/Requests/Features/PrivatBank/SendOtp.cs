using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;

namespace Telemart.Client.Data.Requests.Features.PrivatBank
{
    public sealed class SendOtp : CallActionWithBodyRequestResultBase<PrivatBankCreateSessionResponse, SendOtp.PrivatBankSendOtpRequest>
    {
        public SendOtp(string sessionId, string otpDev)
            : base(new PrivatBankSendOtpRequest(sessionId, otpDev), "pb", "sendOtp")
        {
        }

        public class PrivatBankSendOtpRequest
        {
            public PrivatBankSendOtpRequest(string sessionId, string otpDev)
            {
                SessionId = sessionId;
                OtpDev = otpDev;
            }

            [JsonProperty("session_id")]
            public string SessionId { get; set; }

            [JsonProperty("otp_dev")]
            public string OtpDev { get; set; }
        }
    }
}