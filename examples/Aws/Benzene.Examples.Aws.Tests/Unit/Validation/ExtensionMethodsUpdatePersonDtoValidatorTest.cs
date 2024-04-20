// using Benzene.Core.Helper;
// using Benzene.Examples.Aws.Model.Messages;
// using Newtonsoft.Json;
//
// namespace Benzene.Examples.Aws.Tests.Unit.Validation;
//
// public static class ExtensionMethodsUpdatePersonDtoValidatorTest
// {
//     public static PatchOrderMessage UpdateField(this PatchOrderMessage order)
//     {
//         string json = JsonConvert.SerializeObject(order, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
//         var jsonDeserializer = new JsonDeserializer();
//         return jsonDeserializer.Deserialize<PatchOrderMessage>(json);
//     }
// }