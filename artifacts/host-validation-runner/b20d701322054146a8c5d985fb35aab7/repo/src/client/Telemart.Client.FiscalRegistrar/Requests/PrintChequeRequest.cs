using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Telemart.Client.FiscalRegistrar.Entities;

namespace Telemart.Client.FiscalRegistrar.Requests
{
    public sealed class PrintChequeRequest
    {
        private string requestString;
        private JObject requestObject;

        public PrintChequeRequest(
            string prefixComment,
            string postfixComment,
            int orderId,
            ChequeType type,
            IReadOnlyCollection<PrintChequeProductItem> products,
            IReadOnlyCollection<PrintChequePaymentItem> payments)
        {
            PrefixComment = prefixComment;
            PostfixComment = postfixComment;
            OrderId = orderId;
            Type = type;
            Products = products ?? throw new ArgumentNullException(nameof(products));
            Payments = payments ?? throw new ArgumentNullException(nameof(payments));
        }

        public string PrefixComment { get; }

        public string PostfixComment { get; }

        public int OrderId { get; }

        public ChequeType Type { get; }

        public IReadOnlyCollection<PrintChequeProductItem> Products { get; }

        public IReadOnlyCollection<PrintChequePaymentItem> Payments { get; }

        public object RequestObject => requestObject ?? (requestObject = ToJsonObject());

        private string RequestString => requestString ?? (requestString = RequestObject.ToString());

        public override string ToString()
        {
            return RequestString;
        }

        private JObject ToJsonObject()
        {
            JArray jArray = new JArray();

            if (!string.IsNullOrWhiteSpace(PrefixComment))
            {
                jArray.Add(new JObject(new JProperty(
                    "N",
                    new JObject(new JProperty("cm", PrefixComment)))));
            }

            foreach (PrintChequeProductItem product in Products)
            {
                JObject productObject = new JObject(
                    new JProperty("code", product.Id),
                    new JProperty("name", product.Name),
                    new JProperty("price", product.Price),
                    new JProperty("qty", product.Quantity));

                jArray.Add(new JObject(new JProperty("S", productObject)));
            }

            foreach (PrintChequePaymentItem payment in Payments)
            {
                JObject paymentObject = new JObject(
                    new JProperty("no", payment.No),
                    new JProperty("sum", payment.Sum));

                jArray.Add(new JObject(new JProperty("P", paymentObject)));
            }

            if (!string.IsNullOrWhiteSpace(PostfixComment))
            {
                jArray.Add(new JObject(new JProperty(
                    "N",
                    new JObject(new JProperty("cm", PostfixComment)))));
            }

            jArray.Add(new JObject(new JProperty("BC", new JObject(new JProperty("code", OrderId.ToString()), new JProperty("type", 3)))));

            string rootObjectName;

            switch (Type)
            {
                case ChequeType.Receive:
                    rootObjectName = "F";
                    break;
                case ChequeType.Refund:
                    rootObjectName = "R";
                    break;
                default:
                    throw new NotSupportedException("Cheque type not supported");
            }

            JObject jObject = new JObject
            {
                { rootObjectName, jArray }
            };

            return jObject;
        }
    }
}
