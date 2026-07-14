namespace Benzene.Core.MessageHandlers;

/// <summary>
/// Marks a message handler class with the topic (and optionally version) it handles, so
/// <see cref="ReflectionMessageHandlersFinder"/> can discover it and route messages to it.
/// </summary>
/// <remarks>
/// Apply this to a class implementing <c>IMessageHandler&lt;TRequest&gt;</c> or
/// <c>IMessageHandler&lt;TRequest, TResponse&gt;</c>. Only one <see cref="MessageAttribute"/> per
/// class is used; a class without this attribute is not discovered by reflection-based handler
/// discovery. Two handlers may not share the same topic and version - <see cref="ReflectionMessageHandlersFinder"/>
/// throws if it detects that at discovery time.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class MessageAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessageAttribute"/> class.
    /// </summary>
    /// <param name="topic">The topic id this handler processes messages for.</param>
    /// <param name="version">
    /// The optional version of this handler, used by <see cref="Benzene.Abstractions.MessageHandlers.IVersionSelector"/> when multiple
    /// handlers are registered for the same topic. Defaults to an empty string (unversioned).
    /// </param>
    public MessageAttribute(string topic, string version = "")
    {
        Version = version;
        Topic = topic;
    }

    /// <summary>
    /// Gets the version of this handler, as supplied to the constructor.
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// Gets the topic id this handler processes messages for.
    /// </summary>
    public string Topic { get; }
}
