using System.Xml.Serialization;

namespace Telemart.Client.ViewModels.Warehouse.Movement.Xml
{
    public class MovementDocumentProduct
    {
        public MovementDocumentProduct(int id, int quantity)
        {
            Id = id;
            Quantity = quantity;
        }

        public MovementDocumentProduct()
        {
        }

        [XmlElement("id")]
        public int Id { get; set; }

        [XmlElement("quantity")]
        public int Quantity { get; set; }
    }
}