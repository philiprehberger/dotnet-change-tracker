using Xunit;
using Philiprehberger.ChangeTracker;

namespace Philiprehberger.ChangeTracker.Tests;

public class ChangeSetTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        var changes = new List<PropertyChange>
        {
            new("Name", "Alice", "Bob", DateTimeOffset.UtcNow)
        };
        var trackedAt = DateTimeOffset.UtcNow;

        var changeSet = new ChangeSet("MyType", changes, trackedAt);

        Assert.Equal("MyType", changeSet.TypeName);
        Assert.Single(changeSet.Changes);
        Assert.Equal(trackedAt, changeSet.TrackedAt);
    }

    [Fact]
    public void ToJson_ReturnsValidJson()
    {
        var changes = new List<PropertyChange>
        {
            new("Name", "Alice", "Bob", DateTimeOffset.UtcNow)
        };
        var changeSet = new ChangeSet("MyType", changes, DateTimeOffset.UtcNow);

        var json = changeSet.ToJson();

        Assert.Contains("typeName", json);
        Assert.Contains("MyType", json);
        Assert.Contains("changes", json);
    }

    [Fact]
    public void FromJson_RoundTrips()
    {
        var changes = new List<PropertyChange>
        {
            new("Age", 25, 30, DateTimeOffset.UtcNow)
        };
        var original = new ChangeSet("TestType", changes, DateTimeOffset.UtcNow);

        var json = original.ToJson();
        var deserialized = ChangeSet.FromJson(json);

        Assert.Equal(original.TypeName, deserialized.TypeName);
        Assert.Equal(original.Changes.Count, deserialized.Changes.Count);
    }
}
