using Benzene.Abstractions.MessageHandlers.Info;

namespace Benzene.Schema.OpenApi.Abstractions;

/// <summary>
/// Implemented by spec document builders that can advertise every transport the host is wired to
/// receive messages over — e.g. the <c>benzene</c> format's top-level <c>transports</c> field.
/// </summary>
public interface IConsumesTransportsInfo<out TBuilder>
{
    TBuilder AddTransportsInfo(ITransportsInfo transportsInfo);
}
