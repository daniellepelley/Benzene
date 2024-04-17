namespace Benzene.Core.DI;

public class RegistrationMatch
{
    public RegistrationMatch(string type, string method, string package)
    {
        Type = type;
        Method = method;
        Package = package;
    }

    public string Type { get; }
    public string Method { get; }
    public string Package { get; }
}
