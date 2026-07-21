namespace Benzene.Aws.Lambda.Kinesis;

/// <summary>
/// Configures how <see cref="KinesisStreamApplication"/> checkpoints a Kinesis batch.
/// </summary>
public class KinesisStreamOptions
{
    /// <summary>
    /// When <c>true</c> (the default), a batch whose pipeline completes without throwing and whose
    /// handler never checkpointed anything itself is checkpointed to the end - so a fully-processed
    /// batch advances its resume point instead of being redelivered by Kinesis forever (the
    /// <c>UseStream((records, ct) =&gt; ...)</c> callback overload never checkpoints on its own). Set
    /// <c>false</c> to leave the resume point at exactly what the handler explicitly checkpointed even
    /// on success (full manual control). Auto-checkpoint never runs when the pipeline throws - the
    /// resume point then stays at the handler's last explicit checkpoint, the correct Kinesis
    /// shard-ordered retry signal. Mirrors Cosmos's <c>AutoCheckpointOnSuccess</c>.
    /// </summary>
    public bool AutoCheckpointOnSuccess { get; set; } = true;
}
