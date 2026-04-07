using System;
using System.Collections.Generic;
using System.Linq;
using Telemart.Client.Data.WebClient;

namespace Telemart.Client.ViewModels.Money.Receive.BankPayment
{
    public class BankPaymentFilteringItem : IFilteringItem
    {
        private const string Separator = ",";

        public DateTime? CreatedFrom { get; set; }

        public DateTime? CreatedTo { get; set; }

        public string OrderIds { get; set; }

        public List<int> Statuses { get; set; }

        public List<int> Cashboxes { get; set; }

        public IEnumerable<(string Name, object Value)> BuildParameters()
        {
            if (CreatedFrom.HasValue)
            {
                yield return ("created_from", CreatedFrom.Value.ToString("s"));
            }

            if (CreatedTo.HasValue)
            {
                yield return ("created_to", CreatedTo.Value.ToString("s"));
            }

            if (!string.IsNullOrEmpty(OrderIds))
            {
                yield return ("order_ids", OrderIds);
            }

            if (Statuses != null && Statuses.Any())
            {
                string statuses = string.Join(Separator, Statuses);
                yield return ("states", statuses);
            }

            if (Cashboxes != null && Cashboxes.Any())
            {
                string createdByIds = string.Join(Separator, Cashboxes);
                yield return ("cashboxes", createdByIds);
            }
        }
    }
}