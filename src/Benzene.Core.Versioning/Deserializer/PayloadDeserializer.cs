using System.Reflection;
using System.Text.Json;
using Benzene.Core.Versioning.Casters;
using Benzene.Core.Versioning.Schemas;

namespace Benzene.Core.Versioning.Deserializer;

public class PayloadDeserializer : IPayloadDeserializer
{
    private readonly SchemaCasters _jsonElementSchemaCasters;
    private readonly IPayloadSchemaVersionLookUp _PayloadSchemaVersionLookUp;
    private readonly IPayloadFields _PayloadFields;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public PayloadDeserializer(ISchemaCasters schemaCasters, IPayloadSchemaVersionLookUp PayloadSchemaVersionLookUp, IPayloadFields PayloadFields, JsonSerializerOptions jsonSerializerOptions)
    {
        _jsonSerializerOptions = jsonSerializerOptions;
        _PayloadFields = PayloadFields;
        _PayloadSchemaVersionLookUp = PayloadSchemaVersionLookUp;
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
        var fromSchemaVersion = _PayloadFields.GetSchemaVersion(json);
        var topic = _PayloadFields.GetTopic(json);

        var toSchemaVersion = _PayloadSchemaVersionLookUp.GetSchemaVersion(topic);

        if (fromSchemaVersion == toSchemaVersion)
        {
            return JsonSerializer.Deserialize<T>(json.GetRawText(), _jsonSerializerOptions);
        }

        var schemaCaster = (ISchemaCaster<JsonElement, T>)_jsonElementSchemaCasters.GetSchemaCaster(fromSchemaVersion, toSchemaVersion, topic);

        return schemaCaster.Caster.Cast(json);
    }
}
