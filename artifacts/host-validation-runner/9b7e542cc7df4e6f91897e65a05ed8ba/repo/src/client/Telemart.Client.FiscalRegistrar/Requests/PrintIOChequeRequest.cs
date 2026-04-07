using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Telemart.Client.FiscalRegistrar.Requests
{
    public class PrintIOChequeRequest
    {
        public PrintIOChequeRequest(params PrintIOChequeItem[] items)
        {
            Items = items;
        }

        public IReadOnlyCollection<PrintIOChequeItem> Items { get; }

        public JObject ToJsonObject()
        {
            JArray jArray = new JArray();

            foreach (PrintIOChequeItem item in Items)
            {
                JObject itemObject = new JObject(
                        new JProperty("sum", item.Sum),
                        new JProperty("no", item.Num));

                jArray.Add(new JObject(new JProperty("IO", itemObject)));
            }

            JObject jObject = new JObject
            {
                { "IO", jArray }
            };

            return jObject;
        }
    }
}
