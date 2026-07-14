using System.Collections.Generic;
using Benzene.Core.MessageHandlers.Helper;
using Xunit;

namespace Benzene.Test.Core.Core.MessageHandling;

public class DictionaryUtilsTest
{
    private class Enrichable
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public string Untouched { get; set; } = "default";
    }

    [Fact]
    public void MapOnto_ExistingKeyWithDefaultValue_IsReplaced()
    {
        var source = new Dictionary<string, object> { { "foo", null } };
        var overlay = new Dictionary<string, object> { { "foo", "bar" } };

        var result = DictionaryUtils.MapOnto(source, overlay);

        Assert.Equal("bar", result["foo"]);
    }

    [Fact]
    public void MapOnto_ExistingKeyWithNonDefaultValue_IsNotReplaced()
    {
        var source = new Dictionary<string, object> { { "foo", "original" } };
        var overlay = new Dictionary<string, object> { { "foo", "bar" } };

        var result = DictionaryUtils.MapOnto(source, overlay);

        Assert.Equal("original", result["foo"]);
    }

    [Fact]
    public void MapOnto_LowercasesKeys()
    {
        var source = new Dictionary<string, object>();
        var overlay = new Dictionary<string, object> { { "FOO", "bar" } };

        var result = DictionaryUtils.MapOnto(source, overlay);

        Assert.True(result.ContainsKey("foo"));
        Assert.False(result.ContainsKey("FOO"));
    }

    [Fact]
    public void MapOnto_NullOverlay_ReturnsSourceUnchanged()
    {
        var source = new Dictionary<string, object> { { "foo", "bar" } };

        var result = DictionaryUtils.MapOnto(source, (IDictionary<string, object>)null);

        Assert.Equal("bar", result["foo"]);
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
        Assert.Equal("default", result.Untouched);
    }

    [Fact]
    public void Enrich_ConvertsValueTypeWhenItDiffersFromPropertyType()
    {
        var source = new Enrichable();
        // "age" arrives as a string here (e.g. from a route parameter), but the property is an int.
        var dictionary = new Dictionary<string, object> { { "age", "42" } };

        var result = DictionaryUtils.Enrich(source, dictionary);

        Assert.Equal(42, result.Age);
    }

    [Fact]
    public void Enrich_NullSource_CreatesNewInstance()
    {
        var dictionary = new Dictionary<string, object> { { "Name", "foo" } };

        var result = DictionaryUtils.Enrich<Enrichable>(null, dictionary);

        Assert.NotNull(result);
        Assert.Equal("foo", result.Name);
    }

    [Fact]
    public void Enrich_EmptyDictionary_ReturnsSourceUntouched()
    {
        var source = new Enrichable { Name = "original" };

        var result = DictionaryUtils.Enrich(source, new Dictionary<string, object>());

        Assert.Same(source, result);
        Assert.Equal("original", result.Name);
    }

    [Fact]
    public void Enrich_DuplicateCaseInsensitiveKeys_FirstOccurrenceWins()
    {
        // Preserves the pre-refactor .First() semantics: if a caller's dictionary somehow has two
        // differently-cased keys for the same property, the first-enumerated one wins rather than
        // throwing.
        var source = new Enrichable();
        var dictionary = new Dictionary<string, object> { { "Name", "first" }, { "NAME", "second" } };

        var result = DictionaryUtils.Enrich(source, dictionary);

        Assert.Equal("first", result.Name);
    }

    [Fact]
    public void Enrich_RepeatedCallsForSameType_ProduceIndependentCorrectResults()
    {
        // Exercises the compiled-setter cache across multiple distinct instances of the same T.
        var first = DictionaryUtils.Enrich(new Enrichable(), new Dictionary<string, object> { { "Name", "alice" } });
        var second = DictionaryUtils.Enrich(new Enrichable(), new Dictionary<string, object> { { "Name", "bob" } });

        Assert.Equal("alice", first.Name);
        Assert.Equal("bob", second.Name);
    }
}
