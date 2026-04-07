using System.Xml.Serialization;

namespace Telemart.Client.ViewModels.SupplierBill.Requests
{
    public class BillProductRequest
    {
        public BillProductRequest(int id, int quantity, decimal price, int? taxRate, long? tnved)
        {
            Id = id;
            Quantity = quantity;
            Price = price;
            TaxRate = taxRate;
            Tnved = tnved;
        }

        public BillProductRequest()
        {
        }

        [XmlElement("id_product")]
        public int Id { get; set; }

        [XmlElement("quantity")]
        public int Quantity { get; set; }

        [XmlElement("price")]
        public decimal Price { get; set; }

        [XmlElement("tax_rate")]
        public int? TaxRate { get; set; }

        [XmlElement("tnved")]
        public long? Tnved { get; set; }
    }
}
