namespace Benzene.HealthChecks;

public class HealthCheckNamer
{
    private readonly Dictionary<string, int> _existingNames = new() {{ "HealthCheck", 0 }};
    
    public string GetName(string name)
    {
        if (!string.IsNullOrEmpty(name))
        {
            return ReturnName(name);
        }
        return ReturnName("HealthCheck");
    }
    
    public string ReturnName(string name)
    {
        if (_existingNames.TryAdd(name, 1))
        {
            return name;
        }

        _existingNames[name]++;
        return $"{name}-{_existingNames[name]}";
    }
}
