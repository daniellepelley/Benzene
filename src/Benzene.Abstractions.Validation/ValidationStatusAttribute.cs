namespace Benzene.Abstractions.Validation;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ValidationStatusAttribute : Attribute
{
    public string Status { get; }

    public ValidationStatusAttribute(string status)
    {
        Status = status;
    }
}
