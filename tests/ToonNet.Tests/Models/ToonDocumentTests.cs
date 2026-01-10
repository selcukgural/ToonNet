using ToonNet.Core.Models;

namespace ToonNet.Tests.Models;

public class ToonDocumentTests
{
    [Fact]
    public void Constructor_WithToonValue_SetsRoot()
    {
        var obj = new ToonObject();
        var doc = new ToonDocument(obj);
        Assert.NotNull(doc.Root);
        Assert.Same(obj, doc.Root);
    }

    [Fact]
    public void Constructor_Default_CreatesEmptyObject()
    {
        var doc = new ToonDocument();
        Assert.NotNull(doc.Root);
        Assert.IsType<ToonObject>(doc.Root);
    }

    [Fact]
    public void AsObject_WhenRootIsObject_ReturnsObject()
    {
        var obj = new ToonObject();
        obj["key"] = new ToonString("value");
        var doc = new ToonDocument(obj);
        var result = doc.AsObject();
        Assert.NotNull(result);
        Assert.Same(obj, result);
    }

    [Fact]
    public void AsObject_WhenRootIsNotObject_ThrowsException()
    {
        var arr = new ToonArray();
        var doc = new ToonDocument(arr);
        var ex = Assert.Throws<InvalidOperationException>(() => doc.AsObject());
        Assert.Contains("not an object", ex.Message);
    }

    [Fact]
    public void AsArray_WhenRootIsArray_ReturnsArray()
    {
        var arr = new ToonArray();
        arr.Items.Add(new ToonNumber(123));
        var doc = new ToonDocument(arr);
        var result = doc.AsArray();
        Assert.NotNull(result);
        Assert.Same(arr, result);
    }

    [Fact]
    public void AsArray_WhenRootIsNotArray_ThrowsException()
    {
        var obj = new ToonObject();
        var doc = new ToonDocument(obj);
        var ex = Assert.Throws<InvalidOperationException>(() => doc.AsArray());
        Assert.Contains("not an array", ex.Message);
    }

    [Fact]
    public void Document_RootCanBeNull()
    {
        var doc = new ToonDocument(ToonNull.Instance);
        Assert.NotNull(doc.Root);
        Assert.IsType<ToonNull>(doc.Root);
    }

    [Fact]
    public void Document_RootCanBeString()
    {
        var doc = new ToonDocument(new ToonString("Hello"));
        Assert.IsType<ToonString>(doc.Root);
        Assert.Equal("Hello", ((ToonString)doc.Root).Value);
    }

    [Fact]
    public void Document_RootCanBeNumber()
    {
        var doc = new ToonDocument(new ToonNumber(42.5));
        Assert.IsType<ToonNumber>(doc.Root);
        Assert.Equal(42.5, ((ToonNumber)doc.Root).Value);
    }

    [Fact]
    public void Document_RootCanBeBoolean()
    {
        var doc = new ToonDocument(new ToonBoolean(true));
        Assert.IsType<ToonBoolean>(doc.Root);
        Assert.True(((ToonBoolean)doc.Root).Value);
    }
}