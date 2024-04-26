// using System;
// using System.Collections.Generic;
// using System.Linq;
// using Newtonsoft.Json;
// using Newtonsoft.Json.Serialization;
//
// namespace Benzene.Elements.Core.Patches;
//
// public class PatchContractResolver : DefaultContractResolver
// {
//     private readonly string[] _updatedFields;
//     
//     public PatchContractResolver(string[] updatedFields)
//     {
//         _updatedFields = updatedFields;
//     }
//     
//     protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
//     {
//         var properties = base.CreateProperties(type, memberSerialization);
//         return properties.Where(p => _updatedFields.Contains(p.PropertyName.ToLower())).ToList();
//     }
// }
