using Benzene.Core.MessageHandlers;
using Xunit;

namespace Benzene.Test.Core.MessageHandlers;

public class VersionSelectorTest
{
    private readonly VersionSelector _versionSelector = new();

    [Fact]
    public void Select_RequestedVersionAvailable_ReturnsIt()
    {
        Assert.Equal("V1", _versionSelector.Select("V1", new[] { "V1", "V2" }));
    }

    [Fact]
    public void Select_RequestedVersionMissing_FallsBackToOrdinalMax()
    {
        // Ordinal max of ["a","B"] is "a" (0x61 > 0x42). The default culture-sensitive comparer would
        // pick "B" under most cultures - the doc promises ordinal, and it must not vary by machine culture.
        Assert.Equal("a", _versionSelector.Select("missing", new[] { "a", "B" }));
    }
}
