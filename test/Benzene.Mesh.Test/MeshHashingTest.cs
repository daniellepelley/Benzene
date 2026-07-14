using Benzene.CodeGen.Core;
using Benzene.Mesh.Contracts;
using Xunit;

namespace Benzene.Mesh.Test;

// Cross-checks MeshHashing.ComputeHash against Benzene.CodeGen.Core.CodeGenHelpers.GenerateHash -
// the two are deliberately separate implementations (see MeshHashing's XML doc comment for why),
// so this test is what keeps them from silently drifting apart.
public class MeshHashingTest
{
    [Theory]
    [InlineData("{}")]
    [InlineData("{\"foo\":\"bar\"}")]
    [InlineData("some non-JSON string is fine too - it's just hashed as text")]
    [InlineData("")]
    public void ComputeHash_MatchesCodeGenHelpers_GenerateHash(string json)
    {
        Assert.Equal(CodeGenHelpers.GenerateHash(json), MeshHashing.ComputeHash(json));
    }

    [Fact]
    public void ComputeHash_DifferentInput_DifferentHash()
    {
        Assert.NotEqual(MeshHashing.ComputeHash("{\"a\":1}"), MeshHashing.ComputeHash("{\"a\":2}"));
    }
}
