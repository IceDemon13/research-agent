using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Call;

namespace Telemart.Client.Data.Requests.Features.Call.Actions
{
    public class MakeCall : CallEntityActionWithBodyRequestResultBase<CallDto, MakeCall.MakeCallDto>
    {
        public MakeCall(int id, string phone)
            : base(id, new MakeCallDto(id, phone), ApiResources.Calls, "make")
        {
        }

        public class MakeCallDto
        {
            public MakeCallDto(int id, string phone)
            {
                Id = id;
                Phone = phone;
            }

            [JsonProperty("id")]
            public int Id { get; set; }

            [JsonProperty("phone")]
            public string Phone { get; set; }
        }
    }
}