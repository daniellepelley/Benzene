namespace Benzene.Mesh.Tracing.Tempo;

/// <summary>One timeseries sample returned by a Prometheus instant query - a label set and its current value.</summary>
public class PrometheusSample
{
    /// <summary>Initializes a new instance of the <see cref="PrometheusSample"/> class.</summary>
    /// <param name="labels">The sample's label set (e.g. <c>client</c>, <c>server</c>).</param>
    /// <param name="value">The sample's current value.</param>
    public PrometheusSample(IReadOnlyDictionary<string, string> labels, double value)
    {
        Labels = labels;
        Value = value;
    }

    /// <summary>The sample's label set (e.g. <c>client</c>, <c>server</c>).</summary>
    public IReadOnlyDictionary<string, string> Labels { get; }

    /// <summary>The sample's current value.</summary>
    public double Value { get; }
}
