using System;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;
using Benzene.Core.Filters;

namespace Benzene.Test.Examples;

[Serializable, XmlRoot(ElementName = "root")]
public class ExampleRequestPayload
{
    [XmlElement]
    public int Id { get; set; }

    [Required]
    [MaxLength(10)]
    [XmlElement]
    public string Name { get; set; }
    public string Mapped { get; set; }
}

public class ExamplePayloadRequestFilter : IFilter<ExampleRequestPayload>
{
    public bool Filter(ExampleRequestPayload value)
    {
        return value.Name == "foo";
    }
}
