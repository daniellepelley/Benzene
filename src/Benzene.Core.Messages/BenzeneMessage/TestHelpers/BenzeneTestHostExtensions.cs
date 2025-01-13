// using Benzene.Abstractions;
// using Benzene.Core.BenzeneMessage;
//
// namespace Benzene.Core.Messages.BenzeneMessage.TestHelpers;
//
// public static class BenzeneTestHostExtensions
// {
//     public static Task<BenzeneMessageResponse> SendBenzeneMessageAsync(this IBenzeneTestHost source, BenzeneMessageRequest benzeneMessageRequest)
//     {
//         return source.SendEventAsync<BenzeneMessageResponse>(benzeneMessageRequest);
//     }
//
//     public static Task<BenzeneMessageResponse> SendBenzeneMessageAsync<T>(this IBenzeneTestHost source, IMessageBuilder<T> messageBuilder)
//     {
//         return source.SendBenzeneMessageAsync(messageBuilder.AsBenzeneMessage());
//     }
// }
