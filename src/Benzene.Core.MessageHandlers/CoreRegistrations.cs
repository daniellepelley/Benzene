using Benzene.Core.DI;
using Benzene.Core.MessageHandlers.DI;

namespace Benzene.Core.MessageHandlers;

/// <summary>
/// Describes this package's DI registration extension methods (<see cref="DI.Extensions.AddBenzene"/>,
/// <see cref="DI.Extensions.AddBenzeneMessage"/>, <see cref="DI.Extensions.SetApplicationInfo"/>) for
/// tooling/diagnostics that enumerate a service container's registrations via <see cref="RegistrationsBase"/>.
/// </summary>
public class CoreRegistrations : RegistrationsBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CoreRegistrations"/> class, recording each
    /// registration method this package exposes.
    /// </summary>
    public CoreRegistrations()
    {
        Add(".AddBenzene()", DI.Extensions.AddBenzene);
        Add(".AddBenzeneMessage()", DI.Extensions.AddBenzeneMessage);
        Add(".SetApplicationInfo(<name>, <version>, <description>)", x => x.SetApplicationInfo("", "", ""));
    }
}
