using System.Runtime.Serialization;

namespace Telemart.Client.TransferObjects
{
    [DataContract(Name = "InvoiceProduct")]
    public class InvoiceProductSimple
    {
        [DataMember]
        public string ProductName { get; set; }

        [DataMember]
        public int OrderQuantity { get; set; }

        [DataMember]
        public int Quantity { get; set; }

        [DataMember]
        public decimal Price { get; set; }

        [DataMember]
        public string Currency { get; set; }

        [DataMember]
        public string Employee { get; set; }
    }
}