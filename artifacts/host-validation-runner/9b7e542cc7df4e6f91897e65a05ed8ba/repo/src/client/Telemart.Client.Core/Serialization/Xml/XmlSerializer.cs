using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace Telemart.Client.Core.Serialization.Xml
{
    public sealed class XmlSerializer<T> : ISerializer<T>
        where T : class, new()
    {
        public T Deserialize(string str)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));

            using (StringReader stringReader = new StringReader(str))
            {
                using (XmlReader xmlReader = new XmlTextReader(stringReader))
                {
                    return (T)xmlSerializer.Deserialize(xmlReader);
                }
            }
        }

        public string Serialize(T obj)
        {
            XmlSerializerNamespaces serializerNamespaces = new XmlSerializerNamespaces();
            serializerNamespaces.Add(string.Empty, string.Empty);

            XmlWriterSettings settings = new XmlWriterSettings
            {
                Indent = true,
                OmitXmlDeclaration = true
            };

            XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));

            using (StringWriter stringWriter = new StringWriter())
            {
                using (XmlWriter writer = XmlWriter.Create(stringWriter, settings))
                {
                    xmlSerializer.Serialize(writer, obj, serializerNamespaces);
                    return stringWriter.ToString();
                }
            }
        }
    }
}