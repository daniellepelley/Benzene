namespace Benzene.Schema.OpenApi.Abstractions;

/// <summary>
/// Implemented by spec document builders that can advertise the service's
/// BenzeneMessage-over-HTTP endpoint (see <c>Benzene.Http.BenzeneMessage</c>'s
/// <c>UseBenzeneMessage</c>) — e.g. the <c>benzene</c> format's top-level
/// <c>messageEndpoint</c> field.
/// </summary>
public interface IConsumesMessageEndpoint<out TBuilder>
{
    TBuilder AddMessageEndpoint(string path);
}
