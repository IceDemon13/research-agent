using System.Xml.Serialization;

namespace Telemart.Client.ViewModels.Warehouse.Movement.Xml
{
    [XmlRoot("document")]
    public class MovementDocument
    {
        public MovementDocument(
            int id,
            int warehouseFrom,
            int warehouseTo,
            MovementDocumentProduct[] products,
            string type = "ПТ",
            string organization = "1")
        {
            Id = id;
            WarehouseFrom = warehouseFrom;
            WarehouseTo = warehouseTo;
            Products = products;
            Type = type;
            Organization = organization;
        }

        public MovementDocument()
        {
        }

        [XmlElement("type")]
        public string Type { get; set; }

        [XmlElement("id")]
        public int Id { get; set; }

        [XmlElement("organization")]
        public string Organization { get; set; }

        [XmlElement("id_warehouse_from")]
        public int WarehouseFrom { get; set; }

        [XmlElement("id_warehouse_to")]
        public int WarehouseTo { get; set; }

        [XmlArray("products")]
        [XmlArrayItem("product")]
        public MovementDocumentProduct[] Products { get; set; }
    }
}