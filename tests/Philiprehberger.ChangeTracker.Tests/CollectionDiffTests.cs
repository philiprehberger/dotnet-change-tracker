using Xunit;
using Philiprehberger.ChangeTracker;

namespace Philiprehberger.ChangeTracker.Tests;

public class CollectionDiffTests
{
    [Fact]
    public void CollectionChange_Constructor_SetsAllProperties()
    {
        var change = new CollectionChange(2, CollectionChangeKind.Modified, "old", "new");

        Assert.Equal(2, change.Index);
        Assert.Equal(CollectionChangeKind.Modified, change.Kind);
        Assert.Equal("old", change.OldValue);
        Assert.Equal("new", change.NewValue);
    }

    [Fact]
    public void CollectionChange_Added_HasNullOldValue()
    {
        var change = new CollectionChange(0, CollectionChangeKind.Added, null, "value");

        Assert.Null(change.OldValue);
        Assert.Equal("value", change.NewValue);
    }

    [Fact]
    public void CollectionChange_Removed_HasNullNewValue()
    {
        var change = new CollectionChange(0, CollectionChangeKind.Removed, "value", null);

        Assert.Equal("value", change.OldValue);
        Assert.Null(change.NewValue);
    }

    [Fact]
    public void CollectionDiff_Constructor_SetsChanges()
    {
        var changes = new List<CollectionChange>
        {
            new(0, CollectionChangeKind.Added, null, "item"),
            new(1, CollectionChangeKind.Removed, "old", null)
        };

        var diff = new CollectionDiff(changes);

        Assert.Equal(2, diff.Changes.Count);
    }

    [Fact]
    public void CollectionChange_Equality_SameValues_AreEqual()
    {
        var a = new CollectionChange(1, CollectionChangeKind.Modified, "old", "new");
        var b = new CollectionChange(1, CollectionChangeKind.Modified, "old", "new");

        Assert.Equal(a, b);
    }

    [Fact]
    public void CollectionChangeKind_HasExpectedValues()
    {
        Assert.Equal(0, (int)CollectionChangeKind.Added);
        Assert.Equal(1, (int)CollectionChangeKind.Removed);
        Assert.Equal(2, (int)CollectionChangeKind.Modified);
    }
}
