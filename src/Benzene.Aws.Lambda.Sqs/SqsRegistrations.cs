using Benzene.Core.DI;

namespace Benzene.Aws.Lambda.Sqs;

/// <summary>
/// Declares the dependency injection registrations made by <see cref="DependencyInjectionExtensions.AddSqs"/>,
/// for use by <see cref="RegistrationCheck"/>'s missing-registration diagnostics.
/// </summary>
public class SqsRegistrations : RegistrationsBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqsRegistrations"/> class.
    /// </summary>
    public SqsRegistrations()
    {
        Add(".AddSqs()", x => x.AddSqs());
    }
}
