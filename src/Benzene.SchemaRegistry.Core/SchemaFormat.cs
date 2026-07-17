namespace Benzene.SchemaRegistry.Core;

/// <summary>The wire format a registered schema describes.</summary>
public enum SchemaFormat
{
    /// <summary>Apache Avro (the Confluent registry default).</summary>
    Avro,

    /// <summary>JSON Schema.</summary>
    Json,

    /// <summary>Protocol Buffers.</summary>
    Protobuf
}
