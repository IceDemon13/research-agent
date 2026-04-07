using System.Collections.Generic;
using System.Linq;
using Telemart.Client.Data.WebClient;

namespace Telemart.Client.ViewModels.Service.ServiceRepairs
{
    public sealed class RepairFilteringItem : IFilteringItem
    {
        private const string Separator = ",";

        public List<int> States { get; set; }

        public List<int> ServiceCenters { get; set; }

        public string Name { get; set; }

        public int? Warehouse { get; set; }

        public string Requests { get; set; }

        public string Invoices { get; set; }

        public IEnumerable<(string Name, object Value)> BuildParameters()
        {
            if (!string.IsNullOrWhiteSpace(Requests))
            {
                yield return ("requests", Requests);
            }

            if (!string.IsNullOrWhiteSpace(Invoices))
            {
                yield return ("invoices", Invoices);
            }

            if (States != null && States.Any())
            {
                yield return ("states", string.Join(Separator, States));
            }

            if (ServiceCenters != null && ServiceCenters.Any())
            {
                yield return ("centers", string.Join(Separator, ServiceCenters));
            }

            if (!string.IsNullOrEmpty(Name))
            {
                yield return ("name", Name.Trim());
            }

            if (Warehouse.HasValue)
            {
                yield return ("warehouse", Warehouse.Value.ToString());
            }
        }
    }
}