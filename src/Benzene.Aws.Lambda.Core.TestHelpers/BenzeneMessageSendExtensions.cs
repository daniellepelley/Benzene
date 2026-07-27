using Benzene.Abstractions;
using Benzene.Aws.Lambda.Core;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Core.MessageHandlers.TestHelpers;

namespace Benzene.Aws.Lambda.Core.TestHelpers;

/// <summary>
/// <c>SendBenzeneMessageAsync</c> overloads that dispatch straight off the <see cref="IAwsLambdaEntryPoint"/>
/// that <c>BuildAwsLambdaHost()</c> returns, so the transport-neutral BenzeneMessage direct-invoke path
/// can be exercised as a one-liner - <c>BenzeneTestHost.Create&lt;StartUp&gt;().BuildAwsLambdaHost()
/// .SendBenzeneMessageAsync(...)</c> - without a manual <c>new AwsLambdaBenzeneTestHost(...)</c> wrap,
/// matching the API Gateway / SQS entry-point overloads.
/// </summary>
/// <remarks>
/// This lives here (in the AWS Lambda test-helpers) rather than next to the existing
/// <see cref="IBenzeneTestHost"/> overload in <c>Benzene.Core.MessageHandlers.TestHelpers</c>, because
/// an extension on <see cref="IAwsLambdaEntryPoint"/> would force that transport-neutral core package
/// to depend on AWS - the wrong direction. The caller owns the entry point's lifetime, so the transient
/// host wrapper does NOT dispose it.
/// </remarks>
public static class BenzeneMessageSendExtensions
{
    public static Task<BenzeneMessageResponse> SendBenzeneMessageAsync(this IAwsLambdaEntryPoint source, BenzeneMessageRequest benzeneMessageRequest)
    {
        return new AwsLambdaBenzeneTestHost(source).SendBenzeneMessageAsync(benzeneMessageRequest);
    }

    public static Task<BenzeneMessageResponse> SendBenzeneMessageAsync<T>(this IAwsLambdaEntryPoint source, IMessageBuilder<T> messageBuilder)
    {
        return new AwsLambdaBenzeneTestHost(source).SendBenzeneMessageAsync(messageBuilder);
    }
}
