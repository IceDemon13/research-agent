using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Telemart.Client.FiscalRegistrar.Requests
{
    public class EditPaymentTableRequest
    {
        private readonly IEnumerable<PaymentItem> items;

        public EditPaymentTableRequest(IEnumerable<PaymentItem> items)
        {
            this.items = items;
        }

        public JArray ToRequestObject()
        {
            JArray jArray = new JArray();

            foreach (PaymentItem item in items)
            {
                jArray.Add(new JObject(
                    new JProperty("id", item.Id),
                    new JProperty("Name", item.Name)));
            }

            return jArray;
        }
    }
}