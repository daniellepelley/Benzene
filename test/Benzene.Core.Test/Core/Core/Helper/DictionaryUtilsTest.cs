using System.Collections.Generic;
using Benzene.Core.Helper;
using Xunit;

namespace Benzene.Test.Core.Core.Helper;

public class DictionaryUtilsTest
{
    private class Enrichable
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }

    [Fact]
    public void MapOnto_ExistingKeyWithDefaultValue_IsReplaced()
    {
        var source = new Dictionary<string, object>
        {
            { "foo", null }
        };
        var overlay = new Dictionary<string, object>
        {
            { "foo", "bar" }
        };

        var result = DictionaryUtils.MapOnto(source, overlay);

        Assert.Equal("bar", result["foo"]);
    }

    [Fact]
    public void MapOnto_ExistingKeyWithNonDefaultValue_IsNotReplaced()
    {
        var source = new Dictionary<string, object>
        {
            { "foo", "original" }
        };
        var overlay = new Dictionary<string, object>
        {
            { "foo", "bar" }
        };

        var result = DictionaryUtils.MapOnto(source, overlay);

        Assert.Equal("original", result["foo"]);
    }

    [Fact]
    public void MapOnto_StringOverload_MapsOntoSource()
    {
        var source = new Dictionary<string, object>();
        var overlay = new Dictionary<string, string>
        {
            { "foo", "bar" }
        };

        DictionaryUtils.MapOnto(source, overlay);

        Assert.Equal("bar", source["foo"]);
    }

    [Fact]
    public void Combine_UsesFirstOccurrenceOfEachKey()
    {
        var first = new Dictionary<string, string> { { "a", "1" }, { "b", "2" } };
        var second = new Dictionary<string, string> { { "b", "should-not-win" }, { "c", "3" } };

        var result = DictionaryUtils.Combine(new[] { first, second });

        Assert.Equal("1", result["a"]);
        Assert.Equal("2", result["b"]);
        Assert.Equal("3", result["c"]);
    }

    [Fact]
    public void FilterAndReplace_RenamesKeysAndDropsUnfiltered()
    {
        var source = new Dictionary<string, string>
        {
            { "oldKey", "value1" },
            { "unfiltered", "value2" }
        };
        var filter = new Dictionary<string, string>
        {
            { "oldkey", "newKey" }
        };

        var result = DictionaryUtils.FilterAndReplace(source, filter);

        Assert.Single(result);
        Assert.Equal("value1", result["newKey"]);
    }

    [Fact]
    public void KeyEquals_MatchingValue_ReturnsTrue()
    {
        var dictionary = new Dictionary<string, string> { { "key", "value" } };

        Assert.True(DictionaryUtils.KeyEquals(dictionary, "key", "value"));
    }

    [Fact]
    public void KeyEquals_NonMatchingValue_ReturnsFalse()
    {
        var dictionary = new Dictionary<string, string> { { "key", "other" } };

        Assert.False(DictionaryUtils.KeyEquals(dictionary, "key", "value"));
    }

    [Fact]
    public void KeyEquals_NullDictionary_ReturnsFalse()
    {
        Assert.False(DictionaryUtils.KeyEquals(null, "key", "value"));
    }

    [Fact]
    public void KeyEquals_MissingKey_ReturnsFalse()
    {
        var dictionary = new Dictionary<string, string>();

        Assert.False(DictionaryUtils.KeyEquals(dictionary, "key", "value"));
    }

    [Fact]
    public void Enrich_SetsMatchingPropertiesCaseInsensitively()
    {
        var source = new Enrichable();
        var dictionary = new Dictionary<string, object>
        {
            { "NAME", "foo" },
            { "age", 42 },
            { "unmatched", "ignored" }
        };

        var result = DictionaryUtils.Enrich(source, dictionary);

        Assert.Equal("foo", result.Name);
        Assert.Equal(42, result.Age);
    }

    [Fact]
    public void Enrich_NullSource_CreatesNewInstance()
    {
        var dictionary = new Dictionary<string, object>
        {
            { "Name", "foo" }
        };

        var result = DictionaryUtils.Enrich<Enrichable>(null, dictionary);

        Assert.NotNull(result);
        Assert.Equal("foo", result.Name);
    }
}
