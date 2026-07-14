using Benzene.Core.DI;

namespace Benzene.Aws.Lambda.Kinesis;

/// <summary>
/// Declares the dependency injection registrations made by <see cref="DependencyInjectionExtensions.AddKinesis"/>,
/// for use by <see cref="RegistrationCheck"/>'s missing-registration diagnostics.
/// </summary>
public class KinesisRegistrations : RegistrationsBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KinesisRegistrations"/> class.
    /// </summary>
    public KinesisRegistrations()
    {
        Add(".AddKinesis()", x => x.AddKinesis());
    }
}
