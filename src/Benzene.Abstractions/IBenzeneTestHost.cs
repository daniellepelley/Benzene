namespace Benzene.Abstractions;

/// <summary>
/// Provides a test host for sending events through the Benzene pipeline in test scenarios.
/// This interface enables integration testing by simulating event processing.
/// </summary>
public interface IBenzeneTestHost
{
    /// <summary>
    /// Sends an event through the pipeline and returns the response.
    /// </summary>
    /// <typeparam name="TResponse">The expected response type.</typeparam>
    /// <param name="awsEvent">The event to send through the pipeline.</param>
    /// <returns>A task representing the asynchronous operation, containing the response.</returns>
    Task<TResponse> SendEventAsync<TResponse>(object awsEvent);
}