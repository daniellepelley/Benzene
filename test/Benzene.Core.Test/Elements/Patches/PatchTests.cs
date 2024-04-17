using Benzene.Elements.Core.Patches;
using Benzene.Test.Examples;
using Newtonsoft.Json;
using Xunit;

namespace Benzene.Test.Elements.Patches
{
    public class PatchTests
    {
        [Fact]
        public void PopulatesUpdatedFields()
        {
            var patch = ExamplePatchPayload();

            Assert.Contains("id", patch.UpdatedFields);
            Assert.Contains("name", patch.UpdatedFields);
            Assert.DoesNotContain("address", patch.UpdatedFields);
        }

        [Fact]
        public void HasField()
        {
            var patch = ExamplePatchPayload();

            Assert.True(patch.HasField(x => x.Id));
            Assert.True(patch.HasField(x => x.Name));
            Assert.False(patch.HasField(x => x.Address));
        }

        [Fact]
        public void TryGet()
        {
            var patch = ExamplePatchPayload();

            Assert.Equal(1, patch.TryGet(x => x.Id, 2));
            Assert.Equal("some-name", patch.TryGet(x => x.Name, "foo"));
            Assert.Equal("foo", patch.TryGet(x => x.Address, "foo"));
        }

        [Fact]
        public void Set()
        {
            var patch= new ExamplePatchPayload();

            patch.Set(x => x.Name, "foo");
            
            Assert.Equal("foo", patch.Name);
            Assert.Contains("name", patch.UpdatedFields);
            Assert.DoesNotContain("id", patch.UpdatedFields);
            Assert.DoesNotContain("address", patch.UpdatedFields);
        }
        
        [Fact]
        public void Serialize()
        {
            var patch = new ExamplePatchPayload();

            patch.Set(x => x.Name, "foo");

            var json = new PatchJsonSerializer().Serialize(patch);
            Assert.Equal("{\"Name\":\"foo\"}", json);
        }

        private static ExamplePatchPayload ExamplePatchPayload()
        {
            var raw = new { Id = 1, Name = "some-name" };

            var patch = new PatchJsonSerializer().Deserialize<ExamplePatchPayload>(JsonConvert.SerializeObject(raw));
            return patch;
        }
    }
}
