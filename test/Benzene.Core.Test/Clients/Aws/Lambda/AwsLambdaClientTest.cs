// using System.Collections.Generic;
// using System.IO;
// using System.Text;
// using System.Threading;
// using System.Threading.Tasks;
// using Amazon.Lambda;
// using Amazon.Lambda.Model;
// using Benzene.Clients;
// using Benzene.Clients.Aws.Lambda;
// using Benzene.Test.Clients.Aws.Samples;
// using Benzene.Test.Examples;
// using Moq;
// using Newtonsoft.Json;
// using Xunit;
//
// namespace Benzene.Test.Clients.Aws.Lambda;
//
// public class AwsLambdaClientTest
// {
//     [Fact]
//     public async Task SendMessageAsync()
//     {
//         var mockAmazonLambda = new Mock<IAmazonLambda>();
//         mockAmazonLambda.Setup(x => x.InvokeAsync(It.IsAny<InvokeRequest>(), It.IsAny<CancellationToken>()))
//             .ReturnsAsync(new InvokeResponse
//             {
//                 Payload = ObjectToMemoryStream(new ExamplePayload())
//             });
//
//         await client.SendMessageAsync<BenzeneMessageClientRequest, BenzeneMessageClientResponse>(new BenzeneMessageClientRequest(Defaults.Topic, new Dictionary<string, string>(), Defaults.Message), Defaults.LambdaName,
//             InvocationType.RequestResponse);
//
//         mockAmazonLambda.Verify(x => x.InvokeAsync(It.IsAny<InvokeRequest>(), It.IsAny<CancellationToken>()));
//     }
//
//     public static MemoryStream StringToStream(string src)
//     {
//         var byteArray = Encoding.UTF8.GetBytes(src);
//         return new MemoryStream(byteArray);
//     }
//
//     public static MemoryStream ObjectToMemoryStream(object obj)
//     {
//         return StringToStream(JsonConvert.SerializeObject(obj));
//     }
// }
