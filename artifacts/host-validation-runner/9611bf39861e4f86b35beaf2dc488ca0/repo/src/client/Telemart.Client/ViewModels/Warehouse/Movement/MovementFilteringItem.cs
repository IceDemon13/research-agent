using System;
using System.Collections.Generic;
using System.Linq;
using Telemart.Client.Data.WebClient;

namespace Telemart.Client.ViewModels.Warehouse.Movement
{
    public sealed class MovementFilteringItem : IFilteringItem
    {
        private const string Separator = ",";

        public DateTime? DateInAfter { get; set; }

        public DateTime? DateInBefore { get; set; }

        public DateTime? DateOutAfter { get; set; }

        public DateTime? DateOutBefore { get; set; }

        public List<int> FromWarehouses { get; set; }

        public string MovementNumbers { get; set; }

        public List<int> States { get; set; }

        public List<int> ToWarehouses { get; set; }

        public string Ttn { get; set; }

        public IEnumerable<(string Name, object Value)> BuildParameters()
        {
            if (!string.IsNullOrEmpty(MovementNumbers))
            {
                yield return ("ids", MovementNumbers);
            }

            if (DateOutAfter.HasValue)
            {
                yield return ("doutafter", DateOutAfter.Value.ToString("s"));
            }

            if (DateOutBefore.HasValue)
            {
                yield return ("doutbefore", DateOutBefore.Value.ToString("s"));
            }

            if (DateInAfter.HasValue)
            {
                yield return ("dinafter", DateInAfter.Value.ToString("s"));
            }

            if (DateInBefore.HasValue)
            {
                yield return ("dinbefore", DateInBefore.Value.ToString("s"));
            }

            if (FromWarehouses != null && FromWarehouses.Any())
            {
                string warehouses = string.Join(Separator, FromWarehouses);
                yield return ("fromw", warehouses);
            }

            if (ToWarehouses != null && ToWarehouses.Any())
            {
                string couriers = string.Join(Separator, ToWarehouses);
                yield return ("tow", couriers);
            }

            if (States != null && States.Any())
            {
                string orderStatuses = string.Join(Separator, States);
                yield return ("states", orderStatuses);
            }

            if (!string.IsNullOrEmpty(Ttn))
            {
                yield return ("ttn", Ttn);
            }
        }
    }
}