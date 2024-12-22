using System.Security.Cryptography;
using System.Text;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Schema.OpenApi;
using Benzene.Schema.OpenApi.EventService;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Extensions;

namespace Benzene.CodeGen.Core
{
    public static class CodeGenHelpers
    {
        public static FormatString Camelcase(this string source)
        {
            return new FormatString(source).Camelcase();
        }

        public static FormatString Camelcase(this FormatString source)
        {
            if (string.IsNullOrEmpty(source.Value))
                return source;

            var numberUpper = source.Value.ToCharArray().TakeWhile(char.IsUpper).Count();

            return new FormatString(source.Value.Substring(0, numberUpper).ToLowerInvariant() +
                   source.Value.Substring(numberUpper, source.Value.Length - numberUpper));
        }

        public static FormatString Pascalcase(this FormatString source)
        {
            if (string.IsNullOrEmpty(source.Value))
                return source;
            return new FormatString(char.ToUpperInvariant(source.Value[0]) + source.Value.Substring(1));
        }

        public static FormatString EnsureStartsWithLetterOrUnderScore(this FormatString source)
        {
            if (string.IsNullOrEmpty(source.Value))
                return source;

            if (!char.IsLetter(source.Value[0]) && source.Value[0] != '_')
            {
                return new FormatString("_" + source);
            }

            return source;
        }

        public static FormatString RemoveSpaces(this FormatString source)
        {
            if (string.IsNullOrEmpty(source.Value))
                return source;

            return new FormatString(source.Value.Replace(" ", ""));
        }

        public static FormatString RemoveNonIdentifierCharacters(this FormatString source)
        {
            if (string.IsNullOrEmpty(source.Value))
                return source;

            return new FormatString(string.Concat(source.Value.Where(ch => char.IsLetterOrDigit(ch) || ch == '_')));
        }

        public static string TitleCase(string value)
        {
            return System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(value);
        }

        public static string GenerateHash(IMessageHandlerDefinition[] handlers)
        {
            var json = new EventServiceDocumentBuilder(new SchemaBuilder())
                .AddMessageHandlerDefinitions(handlers)
                .Build()
                .SerializeAsJson(OpenApiSpecVersion.OpenApi3_0);

            return GenerateHash(json);
        }

        private static string GeneratorBase64(string json)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(json);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public static string GenerateHash(string json)
        {
            var hash = new HMACSHA256(Array.Empty<byte>()).ComputeHash(Encoding.UTF8.GetBytes(json));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
}
