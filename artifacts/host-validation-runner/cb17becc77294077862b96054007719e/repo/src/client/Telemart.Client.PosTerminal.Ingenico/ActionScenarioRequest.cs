using System.Xml.Serialization;

namespace Telemart.Client.PosTerminal.Ingenico
{
    public class ActionScenarioRequest
    {
        public string Action { get; set; }

        public uint Amount { get; set; }
        
        public uint MerchantId { get; set; }
        
        [XmlElement("refnum")]
        public string Rn { get; set; }
    }
}