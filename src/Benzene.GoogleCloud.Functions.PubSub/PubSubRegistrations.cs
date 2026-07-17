using Benzene.Core.DI;

namespace Benzene.GoogleCloud.Functions.PubSub;

/// <summary>
/// Declares the dependency injection registrations made by
/// <see cref="DependencyInjectionExtensions.AddGooglePubSub"/>, for use by <see cref="RegistrationCheck"/>'s
/// missing-registration diagnostics.
/// </summary>
public class PubSubRegistrations : RegistrationsBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PubSubRegistrations"/> class.
    /// </summary>
    public PubSubRegistrations()
    {
        Add(".AddGooglePubSub()", x => x.AddGooglePubSub());
    }
}
