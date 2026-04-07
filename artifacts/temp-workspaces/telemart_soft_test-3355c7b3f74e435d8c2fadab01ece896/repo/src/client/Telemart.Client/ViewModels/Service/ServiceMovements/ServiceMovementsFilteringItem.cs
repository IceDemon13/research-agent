using System.Collections.Generic;
using System.Linq;
using Telemart.Client.Data.WebClient;

namespace Telemart.Client.ViewModels.Service.ServiceMovements
{
    public class ServiceMovementsFilteringItem : IFilteringItem
    {
        private const string Separator = ",";

        public string ServiceMovementsNumbers { get; set; }

        public int? WarehouseFromId { get; set; }

        public int? WarehouseToId { get; set; }

        public List<int> Statuses { get; set; }

        public List<int> Carries { get; set; }

        public string Ttn { get; set; }

        public IEnumerable<(string Name, object Value)> BuildParameters()
        {
            if (!string.IsNullOrWhiteSpace(ServiceMovementsNumbers))
            {
                yield return ("ids", ServiceMovementsNumbers);
            }

            if (WarehouseFromId.HasValue)
            {
                yield return ("warehouse_from_id", WarehouseFromId);
            }

            if (WarehouseToId.HasValue)
            {
                yield return ("warehouse_to_id", WarehouseToId);
            }

            if (Statuses?.Any() == true)
            {
                string statuses = string.Join(Separator, Statuses);
                yield return ("states", statuses);
            }

            if (Carries?.Any() == true)
            {
                string carries = string.Join(Separator, Carries);
                yield return ("carries", carries);
            }

            if (!string.IsNullOrEmpty(Ttn))
            {
                yield return ("ttn", Ttn);
            }
        }
    }
}