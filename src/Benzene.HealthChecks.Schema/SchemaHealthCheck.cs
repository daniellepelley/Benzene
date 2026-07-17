using Benzene.Abstractions.MessageHandlers;
using Benzene.CodeGen.Core;
using Benzene.HealthChecks.Core;

namespace Benzene.HealthChecks.Schema;

/// <summary>
/// A provider-side contract health check: it hashes the service's current message contract (every
/// registered handler's topic + request/response schema) and publishes the hash so consumers can
/// tell whether the contract still matches what their generated client was built against.
/// </summary>
/// <remarks>
/// The hash is computed with the same <see cref="CodeGenHelpers.GenerateHash(IMessageHandlerDefinition[])"/>
/// that <c>Benzene.CodeGen.Client</c> bakes into a generated <c>{Service}ServiceClient.HashCode</c>, so
/// a consumer's baked-in hash and this live hash are directly comparable (contract drift = mismatch).
/// The consumer side of the loop is <c>Benzene.Clients.HealthChecks.ClientHealthCheckProcessor</c>.
/// </remarks>
public class SchemaHealthCheck : IHealthCheck
{
    private readonly IMessageHandlerDefinitionLookUp _lookUp;

    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaHealthCheck"/> class.
    /// </summary>
    /// <param name="lookUp">The lookup used to enumerate the service's registered handlers.</param>
    public SchemaHealthCheck(IMessageHandlerDefinitionLookUp lookUp)
    {
        _lookUp = lookUp;
    }

    /// <inheritdoc />
    public string Type => SchemaHealthCheckConstants.Type;

    /// <inheritdoc />
    public Task<IHealthCheckResult> ExecuteAsync()
    {
        var hashCode = CodeGenHelpers.GenerateHash(_lookUp.GetAllHandlers());

        var data = new Dictionary<string, object>
        {
            [SchemaHealthCheckConstants.HashCodeKey] = hashCode,
        };

        return Task.FromResult(HealthCheckResult.CreateInstance(true, SchemaHealthCheckConstants.Type, data));
    }
}
