using System.Xml.Serialization;

namespace Telemart.Client.PosTerminal.Ingenico
{
    [XmlRoot("ActionScenarioResponse")]
    public class ActionScenarioResponse
    {
        [XmlElement("ResultCode")]
        public string ResultCode { get; set; }
        
        [XmlElement("Result")]
        public string Result { get; set; }
    }
}