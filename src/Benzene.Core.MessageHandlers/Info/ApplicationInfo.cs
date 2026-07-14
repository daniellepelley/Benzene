using Benzene.Abstractions.MessageHandlers.Info;

namespace Benzene.Core.MessageHandlers.Info;

/// <summary>
/// Default <see cref="IApplicationInfo"/> implementation, populated via <c>SetApplicationInfo</c> with
/// the values the application supplies at startup.
/// </summary>
public class ApplicationInfo : IApplicationInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationInfo"/> class.
    /// </summary>
    /// <param name="name">The application's name.</param>
    /// <param name="version">The application's version.</param>
    /// <param name="description">The application's description.</param>
    public ApplicationInfo(string name, string version, string description)
    {
        Name = name;
        Version = version;
        Description = description;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string Version { get; }

    /// <inheritdoc />
    public string Description { get; }
}
