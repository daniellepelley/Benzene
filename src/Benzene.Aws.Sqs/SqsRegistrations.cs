using Benzene.Core.DI;

namespace Benzene.Aws.Sqs;

/// <summary>
/// Declares the dependency injection registrations made by <see cref="DependencyInjectionExtensions.AddSqsConsumer"/>,
/// for use by <see cref="RegistrationCheck"/>'s missing-registration diagnostics.
/// </summary>
public class SqsRegistrations : RegistrationsBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqsRegistrations"/> class.
    /// </summary>
    public SqsRegistrations()
    {
        Add(".AddSqsConsumer()", x => x.AddSqsConsumer());
    }
}
