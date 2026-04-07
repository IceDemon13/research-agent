using System;
using System.Collections.Generic;
using System.Linq;

namespace Telemart.Client.ReportDesigner1
{
    public sealed class OrderProductWarrantyCardReportData
    {
        private readonly HashSet<string> serialNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public OrderProductWarrantyCardReportData(
            int position,
            string nameUa,
            int quantity,
            IReadOnlyCollection<string> serials,
            string warrantyUa)
        {
            Position = position;
            NameUa = nameUa;
            Quantity = quantity;

            if(serials?.Any() == true)
            {
                foreach(string serial in serials)
                {
                    serialNumbers.Add(serial);
                }
            }

            WarrantyUa = warrantyUa;
        }

        public string NameUa { get; }

        public int Position { get; }

        public int Quantity { get; }

        public IReadOnlyCollection<string> SerialNumbers => serialNumbers;

        public string Serials => serialNumbers.Any()
            ? string.Join(Environment.NewLine, serialNumbers)
            : "-----";

        public string WarrantyUa { get; }

        public IReadOnlyCollection<OrderProductWarrantyCardReportData> AdditionalServices { get; set; }
    }
}