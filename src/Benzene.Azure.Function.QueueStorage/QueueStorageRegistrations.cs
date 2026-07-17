using Benzene.Core.DI;

namespace Benzene.Azure.Function.QueueStorage;

/// <summary>
/// Declares the dependency injection registrations made by
/// <see cref="DependencyInjectionExtensions.AddAzureQueueStorage"/>, for use by <see cref="RegistrationCheck"/>'s
/// missing-registration diagnostics.
/// </summary>
public class QueueStorageRegistrations : RegistrationsBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QueueStorageRegistrations"/> class.
    /// </summary>
    public QueueStorageRegistrations()
    {
        Add(".AddAzureQueueStorage()", x => x.AddAzureQueueStorage());
    }
}
