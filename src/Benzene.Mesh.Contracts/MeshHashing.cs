using System.Security.Cryptography;
using System.Text;

namespace Benzene.Mesh.Contracts;

/// <summary>
/// Computes the hash used to detect a service's contract drift between mesh aggregator runs.
/// </summary>
/// <remarks>
/// This is deliberately the same algorithm as
/// <c>Benzene.CodeGen.Core.CodeGenHelpers.GenerateHash(string)</c> - the hash already baked into
/// generated <c>Benzene.CodeGen.Client</c> SDKs and compared against a live service's current spec by
/// <c>Benzene.Clients.HealthChecks.ClientHealthCheckProcessor</c>/<c>ClientHashMatch</c>. Keeping the
/// two in sync means the mesh's <see cref="MeshManifestEntry.ContractDrift"/> flag means the same
/// thing as that existing drift check, rather than a second, subtly different notion of "changed".
/// Reimplemented here (rather than referencing <c>Benzene.CodeGen.Core</c> directly) to keep this
/// package's dependency graph limited to what a runtime aggregator actually needs - see
/// <c>test/Benzene.Core.Test/Mesh/MeshHashingTest.cs</c> for the cross-check that keeps the two
/// implementations from silently drifting apart.
/// </remarks>
public static class MeshHashing
{
    /// <summary>Computes the contract-drift hash of a raw spec document JSON string.</summary>
    /// <param name="json">The raw spec document JSON, verbatim as fetched from a service's spec endpoint.</param>
    /// <returns>A lowercase hex digest, with no algorithm prefix.</returns>
    public static string ComputeHash(string json)
    {
        var hash = new HMACSHA256(Array.Empty<byte>()).ComputeHash(Encoding.UTF8.GetBytes(json));
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }
}
