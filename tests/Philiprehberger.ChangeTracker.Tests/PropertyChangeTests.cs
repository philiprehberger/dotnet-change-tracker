using Xunit;
using Philiprehberger.ChangeTracker;

namespace Philiprehberger.ChangeTracker.Tests;

public class PropertyChangeTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var timestamp = DateTimeOffset.UtcNow;

        var change = new PropertyChange("Name", "Old", "New", timestamp);

        Assert.Equal("Name", change.PropertyName);
        Assert.Equal("Old", change.OldValue);
        Assert.Equal("New", change.NewValue);
        Assert.Equal(timestamp, change.Timestamp);
    }

    [Fact]
    public void Constructor_AllowsNullValues()
    {
        var change = new PropertyChange("Field", null, "value", DateTimeOffset.UtcNow);

        Assert.Null(change.OldValue);
        Assert.Equal("value", change.NewValue);
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var timestamp = DateTimeOffset.UtcNow;
        var a = new PropertyChange("Name", "Old", "New", timestamp);
        var b = new PropertyChange("Name", "Old", "New", timestamp);

        Assert.Equal(a, b);
    }
}
