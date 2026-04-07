using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Invoice.Actions
{
    public class SetInvoiceAutoReserve : CallEntityActionWithBodyRequestResultBase<InvoiceDto, SetInvoiceAutoReserve.SetAutoReserveRequest>
    {
        public SetInvoiceAutoReserve(int id, bool autoReserve)
            : base(id, new SetAutoReserveRequest(id, autoReserve), ApiResources.Invoices, "set_auto_reserve")
        {
        }

        public class SetAutoReserveRequest
        {
            public SetAutoReserveRequest(int id, bool autoReserve)
            {
                Id = id;
                AutoReserve = autoReserve;
            }

            [JsonProperty("id")]
            public int Id { get; set; }

            [JsonProperty("auto_reserve")]
            public bool AutoReserve { get; set; }
        }
    }
}
