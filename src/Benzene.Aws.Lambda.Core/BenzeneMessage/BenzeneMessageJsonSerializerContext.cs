using System.Text.Json.Serialization;
using Benzene.Core.Messages.BenzeneMessage;

namespace Benzene.Aws.Lambda.Core.BenzeneMessage;

/// <summary>
/// System.Text.Json source-generation context for the BenzeneMessage direct-invoke request/response
/// types, so <see cref="BenzeneMessageLambdaHandler"/> reads the request and writes the response without
/// System.Text.Json building that metadata by reflection on the first (cold) invocation. This is the
/// Lambda-to-Lambda direct-invoke path the mesh uses. Public so an app can reuse it (e.g. toward
/// trimming/Native AOT).
/// </summary>
/// <remarks>
/// The router deserializes the concrete <see cref="BenzeneMessageRequest"/> but the pipeline hands the
/// response back as the <see cref="IBenzeneMessageResponse"/> interface (its declared property set is
/// identical to the concrete's), so the response is registered by its interface - the static type the
/// serializer is invoked with - not the concrete type.
/// </remarks>
[JsonSerializable(typeof(BenzeneMessageRequest))]
[JsonSerializable(typeof(IBenzeneMessageResponse))]
public partial class BenzeneMessageJsonSerializerContext : JsonSerializerContext
{
}
