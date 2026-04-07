using System.Collections.Generic;
using System.Linq;
using Telemart.Client.Data.WebClient;

namespace Telemart.Client.ViewModels.SupplierBill
{
    public class SupplierBillFilteringItem : IFilteringItem
    {
        private const string Separator = ",";

        public SupplierBillFilteringItem(string billIds, string invoiceIds, string number, List<int> suppliers, List<int> states)
        {
            BillIds = billIds;
            Number = number;
            Suppliers = suppliers;
            States = states;
            InvoiceIds = invoiceIds;
        }

        private string BillIds { get; }

        private string InvoiceIds { get; }

        private string Number { get; }

        private List<int> Suppliers { get; }

        private List<int> States { get; }

        public IEnumerable<(string Name, object Value)> BuildParameters()
        {
            if (!string.IsNullOrEmpty(BillIds))
            {
                yield return ("ids", BillIds);
            }

            if (!string.IsNullOrEmpty(InvoiceIds))
            {
                yield return ("invoice_ids", InvoiceIds);
            }

            if (!string.IsNullOrEmpty(Number))
            {
                yield return ("number", Number);
            }

            if (Suppliers != null && Suppliers.Any())
            {
                string suppliers = string.Join(Separator, Suppliers);
                yield return ("suppliers", suppliers);
            }

            if (States != null && States.Any())
            {
                string states = string.Join(Separator, States);
                yield return ("states", states);
            }
        }
    }
}