using System.Linq;
using Benzene.FluentValidation;
using Xunit;

namespace Benzene.Test.Plugins.FluentValidation;

public class UtilsTest
{
    [Fact]
    public void GetAssemblies_ExplicitAssembliesGiven_ReturnsExactlyThose()
    {
        var thisAssembly = typeof(UtilsTest).Assembly;
        var otherAssembly = typeof(Utils).Assembly;

        var assemblies = Utils.GetAssemblies(thisAssembly, otherAssembly).ToArray();

        Assert.Equal(new[] { thisAssembly, otherAssembly }, assemblies);
    }

    [Fact]
    public void GetAssemblies_NoneGiven_FallsBackToEveryLoadedNonDynamicAssembly()
    {
        var thisAssembly = typeof(UtilsTest).Assembly;

        var assemblies = Utils.GetAssemblies().ToArray();

        Assert.Contains(thisAssembly, assemblies);
        Assert.All(assemblies, a => Assert.False(a.IsDynamic));
    }

    [Fact]
    public void GetAllTypes_ExplicitAssemblyGiven_IncludesATypeKnownToBeInIt()
    {
        var types = Utils.GetAllTypes(typeof(UtilsTest).Assembly).ToArray();

        Assert.Contains(typeof(UtilsTest), types);
    }

    [Fact]
    public void GetAllTypes_ExcludesNestedPrivateTypes()
    {
        var types = Utils.GetAllTypes(typeof(UtilsTest).Assembly).ToArray();

        Assert.DoesNotContain(typeof(NestedPrivateType), types);
    }

    private class NestedPrivateType
    {
    }
}
