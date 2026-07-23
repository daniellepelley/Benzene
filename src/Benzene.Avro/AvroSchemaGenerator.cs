using System.Reflection;
using System.Text;

namespace Benzene.Avro;

/// <summary>
/// Generates an Avro schema (<c>.avsc</c> JSON) from a CLR type by reflecting over its public
/// read/write properties, so callers can serialize plain POCOs without hand-authoring a schema.
/// </summary>
/// <remarks>
/// Type mapping: <c>bool→boolean</c>, signed integral and <c>ushort</c> (<=32 bit)<c>→int</c>,
/// <c>uint/long/ulong→long</c> (<c>uint</c> maps to long, not int, because its upper half overflows int32),
/// <c>float→float</c>, <c>double→double</c>, <c>byte[]→bytes</c>, and
/// <c>string/Guid/DateTime/DateTimeOffset/decimal/enum→string</c> (stringified so precision and
/// round-tripping are preserved for money/timestamps). Nested classes map to Avro records, and
/// <c>IEnumerable&lt;T&gt;</c>/arrays to Avro arrays. Reference-typed and <see cref="Nullable{T}"/>
/// fields become a <c>["null", X]</c> union so nulls round-trip; non-nullable value types are emitted
/// bare. A named record type is emitted once and referenced by name on subsequent occurrences, so
/// recursive/shared types produce a valid single schema.
/// </remarks>
internal static class AvroSchemaGenerator
{
    public static string Generate(Type type)
    {
        var sb = new StringBuilder();
        EmitType(sb, type, allowNull: false, new HashSet<string>());
        return sb.ToString();
    }

    private static void EmitType(StringBuilder sb, Type type, bool allowNull, HashSet<string> definedRecords)
    {
        var underlying = Nullable.GetUnderlyingType(type);
        var nullable = allowNull && (underlying != null || !type.IsValueType);
        var effective = underlying ?? type;

        if (nullable)
        {
            sb.Append("[\"null\",");
            EmitBare(sb, effective, definedRecords);
            sb.Append(']');
        }
        else
        {
            EmitBare(sb, effective, definedRecords);
        }
    }

    private static void EmitBare(StringBuilder sb, Type type, HashSet<string> definedRecords)
    {
        if (type == typeof(bool)) { sb.Append("\"boolean\""); return; }
        if (type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) ||
            type == typeof(ushort) || type == typeof(int)) { sb.Append("\"int\""); return; }
        // uint maps to Avro long, not int: its upper half (> int.MaxValue) does not fit int32, so a
        // legal uint value like 3_000_000_000 threw OverflowException in Convert.ToInt32 on serialize.
        // long comfortably holds the full uint range; the reverse (long -> uint) goes through
        // Convert.ChangeType in FromDatum, which is fine for in-range values.
        if (type == typeof(long) || type == typeof(ulong) || type == typeof(uint)) { sb.Append("\"long\""); return; }
        if (type == typeof(float)) { sb.Append("\"float\""); return; }
        if (type == typeof(double)) { sb.Append("\"double\""); return; }
        if (type == typeof(byte[])) { sb.Append("\"bytes\""); return; }

        if (type == typeof(string) || type == typeof(Guid) || type == typeof(DateTime) ||
            type == typeof(DateTimeOffset) || type == typeof(decimal) || type.IsEnum)
        {
            sb.Append("\"string\"");
            return;
        }

        var elementType = GetEnumerableElementType(type);
        if (elementType != null)
        {
            sb.Append("{\"type\":\"array\",\"items\":");
            EmitType(sb, elementType, allowNull: true, definedRecords);
            sb.Append('}');
            return;
        }

        EmitRecord(sb, type, definedRecords);
    }

    private static void EmitRecord(StringBuilder sb, Type type, HashSet<string> definedRecords)
    {
        var recordName = SanitizeName(type.FullName ?? type.Name);

        // Avro requires each named type be defined once; later occurrences reference it by name.
        if (!definedRecords.Add(recordName))
        {
            sb.Append('"').Append(recordName).Append('"');
            return;
        }

        sb.Append("{\"type\":\"record\",\"name\":\"").Append(recordName).Append("\",\"fields\":[");

        var first = true;
        foreach (var property in GetProperties(type))
        {
            if (!first)
            {
                sb.Append(',');
            }

            first = false;
            sb.Append("{\"name\":\"").Append(property.Name).Append("\",\"type\":");
            EmitType(sb, property.PropertyType, allowNull: true, definedRecords);
            sb.Append('}');
        }

        sb.Append("]}");
    }

    internal static IEnumerable<PropertyInfo> GetProperties(Type type)
    {
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p is { CanRead: true, CanWrite: true } && p.GetIndexParameters().Length == 0);
    }

    internal static Type? GetEnumerableElementType(Type type)
    {
        if (type == typeof(string) || type == typeof(byte[]))
        {
            return null;
        }

        if (type.IsArray)
        {
            return type.GetElementType();
        }

        var enumerable = new[] { type }.Concat(type.GetInterfaces())
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        return enumerable?.GetGenericArguments()[0];
    }

    private static string SanitizeName(string name)
    {
        var sb = new StringBuilder(name.Length);
        foreach (var c in name)
        {
            sb.Append(char.IsLetterOrDigit(c) || c == '_' ? c : '_');
        }

        // Avro names must start with a letter or underscore.
        if (sb.Length == 0 || (!char.IsLetter(sb[0]) && sb[0] != '_'))
        {
            sb.Insert(0, '_');
        }

        return sb.ToString();
    }
}
