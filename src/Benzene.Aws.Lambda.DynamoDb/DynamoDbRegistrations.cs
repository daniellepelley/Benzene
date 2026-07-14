using Benzene.Core.DI;

namespace Benzene.Aws.Lambda.DynamoDb;

/// <summary>
/// Declares the dependency injection registrations made by <see cref="DependencyInjectionExtensions.AddDynamoDb"/>,
/// for use by <see cref="RegistrationCheck"/>'s missing-registration diagnostics.
/// </summary>
public class DynamoDbRegistrations : RegistrationsBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DynamoDbRegistrations"/> class.
    /// </summary>
    public DynamoDbRegistrations()
    {
        Add(".AddDynamoDb()", x => x.AddDynamoDb());
    }
}
