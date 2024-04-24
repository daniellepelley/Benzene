using System;
using System.Xml.Serialization;

namespace Benzene.Examples.App.Model.Messages;

[Serializable, XmlRoot(ElementName = "OrderDto")]
public class CreateOrderMessage
{
    [XmlElement]
    public string Status { get; set; }
    [XmlElement]
    public string Name { get; set; }
}