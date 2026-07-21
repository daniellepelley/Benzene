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

            // Match System.Text.Json's JsonNamingPolicy.CamelCase (the wire policy the runtime
            // serializer uses), so property keys shown in generated markdown docs are the exact
            // wire shape: a capital that precedes a lowercase letter is kept ("IPAddress" ->
            // "ipAddress", not "ipaddress"). See Benzene.Schema.OpenApi ExamplePayloadBuilder.
            return new FormatString(System.Text.Json.JsonNamingPolicy.CamelCase.ConvertName(source.Value));
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
            var document = new EventServiceDocumentBuilder(new SchemaBuilder())
                .AddMessageHandlerDefinitions(handlers)
                .Build();

            return GenerateHash(document);
        }

        /// <summary>
        /// Computes the contract hash of a spec document: the hash of its serialized form with the
        /// non-contract decoration stripped — generated <c>example</c> payloads and the
        /// <c>messageEndpoint</c> advertisement. Examples are derived from the schemas and the
        /// endpoint is transport plumbing, so neither changes what the service's message contract
        /// *is* — and excluding them keeps this hash identical to the hashes baked into client SDKs
        /// generated before examples existed, so upgrading a service doesn't trip the
        /// client-vs-service contract-drift check (<c>Benzene.Clients.HealthChecks</c>) falsely.
        /// </summary>
        public static string GenerateHash(EventServiceDocument document)
        {
            var normalized = new EventServiceDocument(
                document.Info,
                document.Tags,
                document.Requests.Select(x => new RequestResponse
                {
                    Topic = x.Topic,
                    Version = x.Version,
                    HttpMappings = x.HttpMappings,
                    Request = x.Request,
                    Response = x.Response
                }).ToArray(),
                document.Events.Select(x => new Event(x.Topic, x.Message)).ToArray(),
                document.Components);

            return GenerateHash(normalized.SerializeAsJson(OpenApiSpecVersion.OpenApi3_0));
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
