using System.Collections;
using System.Globalization;
using System.Reflection;
using Avro;
using Avro.Generic;

namespace Benzene.Avro;

/// <summary>
/// Converts between plain CLR objects and the Avro datum shapes (<see cref="GenericRecord"/>,
/// arrays, primitives) that <see cref="GenericDatumWriter{T}"/>/<see cref="GenericDatumReader{T}"/>
/// operate on, driven entirely by the resolved <see cref="Schema"/>.
/// </summary>
internal static class AvroDatumConverter
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    // ---------- CLR object -> Avro datum ----------

    public static object? ToDatum(Schema schema, object? value)
    {
        switch (schema.Tag)
        {
            case Schema.Type.Union:
                return ToUnionDatum((UnionSchema)schema, value);
            case Schema.Type.Record:
                return ToRecord((RecordSchema)schema, value);
            case Schema.Type.Array:
                return ToArray((ArraySchema)schema, value);
            case Schema.Type.String:
                return value == null ? null : ToAvroString(value);
            case Schema.Type.Boolean:
                return value != null && Convert.ToBoolean(value, Inv);
            case Schema.Type.Int:
                return value == null ? 0 : Convert.ToInt32(value, Inv);
            case Schema.Type.Long:
                return value == null ? 0L : Convert.ToInt64(value, Inv);
            case Schema.Type.Float:
                return value == null ? 0f : Convert.ToSingle(value, Inv);
            case Schema.Type.Double:
                return value == null ? 0d : Convert.ToDouble(value, Inv);
            case Schema.Type.Bytes:
                return value as byte[] ?? Array.Empty<byte>();
            case Schema.Type.Null:
                return null;
            default:
                return value;
        }
    }

    private static object? ToUnionDatum(UnionSchema union, object? value)
    {
        if (value == null)
        {
            return null;
        }

        var branch = NonNullBranch(union);
        return ToDatum(branch, value);
    }

    private static GenericRecord ToRecord(RecordSchema schema, object? value)
    {
        var record = new GenericRecord(schema);
        if (value == null)
        {
            return record;
        }

        var type = value.GetType();
        foreach (var field in schema.Fields)
        {
            var property = type.GetProperty(field.Name, BindingFlags.Public | BindingFlags.Instance);
            var propertyValue = property?.GetValue(value);
            record.Add(field.Name, ToDatum(field.Schema, propertyValue));
        }

        return record;
    }

    private static object[] ToArray(ArraySchema schema, object? value)
    {
        if (value is not IEnumerable enumerable)
        {
            return Array.Empty<object>();
        }

        var items = new List<object?>();
        foreach (var item in enumerable)
        {
            items.Add(ToDatum(schema.ItemSchema, item));
        }

        return items.ToArray()!;
    }

    private static string ToAvroString(object value)
    {
        return value switch
        {
            string s => s,
            Guid g => g.ToString(),
            DateTime dt => dt.ToString("O", Inv),
            DateTimeOffset dto => dto.ToString("O", Inv),
            decimal d => d.ToString(Inv),
            Enum e => e.ToString(),
            _ => Convert.ToString(value, Inv) ?? string.Empty
        };
    }

    // ---------- Avro datum -> CLR object ----------

    public static object? FromDatum(Schema schema, object? datum, Type targetType)
    {
        switch (schema.Tag)
        {
            case Schema.Type.Union:
                return FromUnion((UnionSchema)schema, datum, targetType);
            case Schema.Type.Record:
                return FromRecord((RecordSchema)schema, datum, targetType);
            case Schema.Type.Array:
                return FromArray((ArraySchema)schema, datum, targetType);
            case Schema.Type.Null:
                return DefaultValue(targetType);
            case Schema.Type.String:
                return FromAvroString(datum as string, targetType);
            default:
                return datum == null ? DefaultValue(targetType) : ConvertPrimitive(datum, targetType);
        }
    }

    private static object? FromUnion(UnionSchema union, object? datum, Type targetType)
    {
        if (datum == null)
        {
            return DefaultValue(targetType);
        }

        var branch = NonNullBranch(union);
        return FromDatum(branch, datum, Nullable.GetUnderlyingType(targetType) ?? targetType);
    }

    private static object? FromRecord(RecordSchema schema, object? datum, Type targetType)
    {
        if (datum is not GenericRecord record)
        {
            return DefaultValue(targetType);
        }

        var instance = Activator.CreateInstance(targetType)!;
        foreach (var property in AvroSchemaGenerator.GetProperties(targetType))
        {
            if (!record.TryGetValue(property.Name, out var fieldDatum))
            {
                continue;
            }

            var field = schema.Fields.First(f => f.Name == property.Name);
            property.SetValue(instance, FromDatum(field.Schema, fieldDatum, property.PropertyType));
        }

        return instance;
    }

    private static object FromArray(ArraySchema schema, object? datum, Type targetType)
    {
        var elementType = AvroSchemaGenerator.GetEnumerableElementType(targetType) ?? typeof(object);
        var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType))!;

        if (datum is IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                list.Add(FromDatum(schema.ItemSchema, item, elementType));
            }
        }

        if (!targetType.IsArray)
        {
            return list;
        }

        var array = Array.CreateInstance(elementType, list.Count);
        list.CopyTo(array, 0);
        return array;
    }

    private static object? FromAvroString(string? value, Type targetType)
    {
        var type = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (value == null)
        {
            return DefaultValue(targetType);
        }

        if (type == typeof(string)) return value;
        if (type == typeof(Guid)) return Guid.Parse(value);
        if (type == typeof(DateTime)) return DateTime.Parse(value, Inv, DateTimeStyles.RoundtripKind);
        if (type == typeof(DateTimeOffset)) return DateTimeOffset.Parse(value, Inv, DateTimeStyles.RoundtripKind);
        if (type == typeof(decimal)) return decimal.Parse(value, NumberStyles.Any, Inv);
        if (type.IsEnum) return Enum.Parse(type, value);
        return value;
    }

    private static object ConvertPrimitive(object datum, Type targetType)
    {
        var type = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (type == typeof(byte[]) || type.IsInstanceOfType(datum))
        {
            return datum;
        }

        return Convert.ChangeType(datum, type, Inv);
    }

    private static Schema NonNullBranch(UnionSchema union)
    {
        return union.Schemas.FirstOrDefault(s => s.Tag != Schema.Type.Null) ?? union.Schemas[0];
    }

    private static object? DefaultValue(Type type)
    {
        return type.IsValueType && Nullable.GetUnderlyingType(type) == null
            ? Activator.CreateInstance(type)
            : null;
    }
}
