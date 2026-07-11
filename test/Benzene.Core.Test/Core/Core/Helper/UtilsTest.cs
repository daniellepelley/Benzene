using System.Collections.Generic;
using System.Linq;
using Benzene.Core.Helper;
using Xunit;

namespace Benzene.Test.Core.Core.Helper;

public class UtilsTest
{
    [Fact]
    public void GetValue_ExistingKey_ReturnsValue()
    {
        var dictionary = new Dictionary<string, string> { { "key", "value" } };

        Assert.Equal("value", dictionary.GetValue("key"));
    }

    [Fact]
    public void GetValue_MissingKey_ReturnsNull()
    {
        var dictionary = new Dictionary<string, string>();

        Assert.Null(dictionary.GetValue("key"));
    }

    [Fact]
    public void GetValue_NullDictionary_ReturnsNull()
    {
        IDictionary<string, string> dictionary = null;

        Assert.Null(dictionary.GetValue("key"));
    }

    [Fact]
    public void GetAssemblies_WithExplicitAssembly_ReturnsIt()
    {
        var assembly = typeof(UtilsTest).Assembly;

        var result = Utils.GetAssemblies(assembly).ToArray();

        Assert.Contains(assembly, result);
    }

    [Fact]
    public void GetAssemblies_NoArguments_ReturnsNonDynamicAppDomainAssemblies()
    {
        var result = Utils.GetAssemblies().ToArray();

        Assert.Contains(typeof(UtilsTest).Assembly, result);
        Assert.All(result, x => Assert.False(x.IsDynamic));
    }

    [Fact]
    public void GetAllTypes_WithExplicitAssembly_ReturnsTypesFromIt()
    {
        var assembly = typeof(UtilsTest).Assembly;

        var result = Utils.GetAllTypes(assembly).ToArray();

        Assert.Contains(typeof(UtilsTest), result);
    }
}
