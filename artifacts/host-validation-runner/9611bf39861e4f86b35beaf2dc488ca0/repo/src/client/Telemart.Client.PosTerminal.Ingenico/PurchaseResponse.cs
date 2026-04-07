using System.Xml.Serialization;

namespace Telemart.Client.PosTerminal.Ingenico
{
    [XmlRoot("Response")]
    public class PurchaseResponse
    {
        [XmlElement("refnum")]
        public string Rn { get; set; }
    }
}