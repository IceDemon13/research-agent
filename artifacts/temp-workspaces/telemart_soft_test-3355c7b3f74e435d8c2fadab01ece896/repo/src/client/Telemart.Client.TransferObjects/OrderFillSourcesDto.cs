using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class OrderFillSourcesDto
    {
        public OrderFillSourcesDto(int id, int[] warehouses, bool useInvoices, bool useMovements)
        {
            Id = id;
            Warehouses = warehouses;
            UseInvoices = useInvoices;
            UseMovements = useMovements;
        }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("warehouses")]
        public int[] Warehouses { get; set; }

        [JsonProperty("use_invoices")]
        public bool UseInvoices { get; set; }

        [JsonProperty("use_movements")]
        public bool UseMovements { get; set; }
    }
}