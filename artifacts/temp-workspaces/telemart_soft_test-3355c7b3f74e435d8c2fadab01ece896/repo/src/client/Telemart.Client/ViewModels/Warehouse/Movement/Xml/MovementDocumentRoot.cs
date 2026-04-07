using System.Xml.Serialization;

namespace Telemart.Client.ViewModels.Warehouse.Movement.Xml
{
    [XmlRoot("documents")]
    public class MovementDocumentRoot
    {
        public MovementDocumentRoot(MovementDocument document)
        {
            Document = document;
        }

        public MovementDocumentRoot()
        {
        }

        [XmlElement("document")]
        public MovementDocument Document { get; set; }
    }
}
