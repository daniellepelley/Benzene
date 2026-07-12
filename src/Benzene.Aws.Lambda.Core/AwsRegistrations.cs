using Benzene.Core.DI;
using Benzene.Core.MessageHandlers.DI;

namespace Benzene.Aws.Lambda.Core;

/// <summary>
/// Declares the dependency injection registrations made by this package's extension methods, for use
/// by <see cref="RegistrationCheck"/>'s missing-registration diagnostics.
/// </summary>
/// <remarks>
/// This class doesn't perform any registration itself. It records, against a
/// <see cref="Benzene.Core.DI.RegistrationRecorder"/>, which types <c>.AddMessageHandlers(...)</c> would
/// register, so that if a consumer forgets to call it, the resulting DI resolution failure can name the
/// missing registration call in its error message.
/// </remarks>
public class AwsRegistrations : RegistrationsBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AwsRegistrations"/> class.
    /// </summary>
    public AwsRegistrations()
    {
        Add(".AddMessageHandlers(<assemblies>)", x => x.AddMessageHandlers());
    }
}
