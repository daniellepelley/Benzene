using System.Reflection;
using System.Text.Json;
using Benzene.Core.Versioning.Casters;
using Benzene.Core.Versioning.Schemas;

namespace Benzene.Core.Versioning.Deserializer;

public class PayloadDeserializer : IPayloadDeserializer
{
    private readonly SchemaCasters _jsonElementSchemaCasters;
    private readonly IPayloadSchemaVersionLookUp _payloadSchemaVersionLookUp;
    private readonly IPayloadFields _payloadFields;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public PayloadDeserializer(ISchemaCasters schemaCasters, IPayloadSchemaVersionLookUp payloadSchemaVersionLookUp, IPayloadFields payloadFields, JsonSerializerOptions jsonSerializerOptions)
    {
        _jsonSerializerOptions = jsonSerializerOptions;
        _payloadFields = payloadFields;
        _payloadSchemaVersionLookUp = payloadSchemaVersionLookUp;
        _jsonElementSchemaCasters = new SchemaCasters(schemaCasters.GetAll()
            .Select(CreateJsonElementSchemaCaster)
            .ToArray());
    }

    private ISchemaCaster CreateJsonElementSchemaCaster(ISchemaCaster schemaCaster)
    {
        MethodInfo methodInfo = typeof(PayloadDeserializer).GetMethod(nameof(CreateJsonCaster), BindingFlags.NonPublic | BindingFlags.Instance)!;
        MethodInfo genericMethod = methodInfo.MakeGenericMethod(schemaCaster.FromType, schemaCaster.ToType);
        var newCaster = (ISchemaCaster)genericMethod.Invoke(this, [schemaCaster])!;
        return newCaster;
    }

    private SchemaCaster<JsonElement, TTo> CreateJsonCaster<TFrom, TTo>(ISchemaCaster<TFrom, TTo> schemaCaster)
    {
        var func = (Func<JsonElement, TTo>)(x => schemaCaster.Caster.Cast(x.Deserialize<TFrom>(_jsonSerializerOptions)!));

        return new SchemaCaster<JsonElement, TTo>
        {
            Definition = schemaCaster.Definition,
            Caster = new FuncCaster<JsonElement, TTo>(func)
        };
    }

    public T? Deserialize<T>(JsonElement json)
    {
        var fromSchemaVersion = _payloadFields.GetSchemaVersion(json);
        var topic = _payloadFields.GetTopic(json);

        var toSchemaVersion = _payloadSchemaVersionLookUp.GetSchemaVersion(topic);

        if (fromSchemaVersion == toSchemaVersion)
        {
            return json.Deserialize<T>(_jsonSerializerOptions);
        }

        var schemaCaster = (ISchemaCaster<JsonElement, T>)_jsonElementSchemaCasters.GetSchemaCaster(fromSchemaVersion, toSchemaVersion, topic);

        return schemaCaster.Caster.Cast(json);
    }
}
