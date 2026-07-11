using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Benzene.Core.DI;

/// <summary>
/// Provides dependency injection registration validation and diagnostic capabilities.
/// </summary>
public class RegistrationCheck : IRegistrationCheck
{
    private readonly IDictionary<string, IDictionary<string, string[]>> _registrations;

    private RegistrationCheck(IDictionary<string, IDictionary<string, Type[]>> registrations)
    {
        _registrations = registrations.ToDictionary(x => x.Key,
            x => Convert(x.Value));
    }

    /// <summary>
    /// Creates a new registration check instance from an array of types.
    /// </summary>
    /// <param name="types">The types to scan for <see cref="IRegistrations"/> implementations.</param>
    /// <returns>A new <see cref="RegistrationCheck"/> instance configured with discovered registrations.</returns>
    public static RegistrationCheck Create(params Type[] types)
    {
        var registrationTypes =  types.Where(x => x.IsClass && !x.IsAbstract && typeof(IRegistrations)
                .IsAssignableFrom(x))
            .ToArray();

        var registrationDictionary = registrationTypes
            .Select(registrationType => (IRegistrations)Activator.CreateInstance(registrationType))
            .Where(registrations => registrations != null)
            .GroupBy(x => x.PackageName)
            .ToDictionary(registrations => registrations.First().PackageName, registrations => DictionaryCombine(registrations.Select(x => x.GetRegistrations())));

        return new RegistrationCheck(registrationDictionary);
    }

    private static IDictionary<string, string[]> Convert(IDictionary<string, Type[]> source)
    {
        return source.ToDictionary(
            x => x.Key,
            x => x.Value.Select(type => GetSimpleTypeName(type.FullName)).ToArray());
    }

    /// <summary>
    /// Checks if a type is registered in the dependency injection container and provides registration guidance if missing.
    /// </summary>
    /// <param name="typeName">The full name of the type to check.</param>
    /// <returns>A diagnostic message indicating registration status and guidance for missing registrations.</returns>
    public string CheckType(string typeName)
    {
        typeName = GetSimpleTypeName(typeName);

        var matches = _registrations
            .SelectMany(x => x.Value.SelectMany(x1 => GetMatches(x.Key, typeName, x1)))  
            .GroupBy(x => new { x.Type, x.Package, x.Method })
            .Select(x => x.First())
            .ToArray();

        return FormatResponse(matches);
    }

    private static RegistrationMatch[] GetMatches(string type, string typeNameToMatch, KeyValuePair<string, string[]> typeRegistrations)
    {
        return typeRegistrations.Value
            .Where(registeredType => IsMatch(typeNameToMatch, registeredType))
            .Select(package => new RegistrationMatch(type, typeRegistrations.Key, package))
            .ToArray();
    }

    private static bool IsMatch(string inputType, string registeredType)
    {
        if (registeredType.Contains("<>"))
        {
            return inputType.StartsWith(registeredType.Split(">").First());
        }

        return inputType == registeredType;
    }

    private static string GetSimpleTypeName(string typeName)
    {
        var parts = typeName.Split(new[] { '[', ',', ']' }, StringSplitOptions.RemoveEmptyEntries);

        return parts.Length == 1
            ? parts[0].Replace("`1", "<>")
            : $"{parts[0].Replace("`1", "")}<{parts[1]}>";
    }

    private static string FormatResponse(RegistrationMatch[] matches)
    {
        var stringBuilder = new StringBuilder();
        foreach (var item in matches)
        {
            stringBuilder.AppendLine();
            stringBuilder.AppendLine($"{item.Type} is registered in {item.Method} from {item.Package}");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("You might be missing this in your dependency registration");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine($"    .UsingBenzene(x => x{item.Method})");
            stringBuilder.AppendLine();
        }

        return stringBuilder.ToString();
    }
    
    /// <summary>
    /// Combines multiple dictionaries into a single dictionary, using the first occurrence of each key.
    /// </summary>
    /// <typeparam name="TKey">The type of dictionary keys.</typeparam>
    /// <typeparam name="TValue">The type of dictionary values.</typeparam>
    /// <param name="source">The collection of dictionaries to combine.</param>
    /// <returns>A combined dictionary containing all unique keys from the source dictionaries.</returns>
    public static IDictionary<TKey, TValue> DictionaryCombine<TKey, TValue>(IEnumerable<IDictionary<TKey, TValue>> source)
    {
        var output = new Dictionary<TKey, TValue>();

        foreach (var dictionary in source)
        {
            foreach (var keyValue in dictionary)
            {
                output.TryAdd(keyValue.Key, keyValue.Value); 
            }
        }

        return output;
    }
}
