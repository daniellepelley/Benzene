namespace Benzene.Idempotency;

/// <summary>Shared default values for idempotency handling.</summary>
public static class IdempotencyDefaults
{
    /// <summary>The default header name a caller-supplied idempotency key is read from.</summary>
    public const string HeaderName = "idempotency-key";
}
