using Benzene.Abstractions.Results;

namespace Benzene.Extras.Results
{
    public class Base64JsonMessage : IBase64JsonMessage
    {
        private Base64JsonMessage(string base64Json)
        {
            Base64Json = base64Json;
        }

        public static Base64JsonMessage CreateInstance(string base64Json)
        {
            return new Base64JsonMessage(base64Json);
        }

        public string Base64Json { get; }
    }
}
