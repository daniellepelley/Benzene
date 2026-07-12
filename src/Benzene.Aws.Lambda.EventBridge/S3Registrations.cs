using Benzene.Core.DI;

namespace Benzene.Aws.EventBridge;

/// <summary>
/// Declares the dependency injection registrations made by <see cref="DependencyInjectionExtensions.AddS3"/>,
/// for use by <see cref="RegistrationCheck"/>'s missing-registration diagnostics.
/// </summary>
public class S3Registrations : RegistrationsBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="S3Registrations"/> class.
    /// </summary>
    public S3Registrations()
    {
        Add(".AddS3()", x => x.AddS3());
    }
}
