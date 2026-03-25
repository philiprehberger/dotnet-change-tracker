using Xunit;
using Philiprehberger.ChangeTracker;

namespace Philiprehberger.ChangeTracker.Tests;

[TrackChanges]
public class SampleEntity
{
    public string Name { get; set; } = "";
    public int Age { get; set; }

    [IgnoreChanges]
    public string Internal { get; set; } = "";

    [SensitiveProperty]
    public string Password { get; set; } = "";
}

public class NotTrackedEntity
{
    public string Name { get; set; } = "";
}

public class ChangeTrackerTests
{
    [Fact]
    public void For_WithNullTarget_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ChangeTracker.For<SampleEntity>(null!));
    }

    [Fact]
    public void For_WithoutTrackChangesAttribute_ThrowsInvalidOperationException()
    {
        var entity = new NotTrackedEntity();

        Assert.Throws<InvalidOperationException>(() => ChangeTracker.For(entity));
    }

    [Fact]
    public void GetChanges_NoChanges_ReturnsEmpty()
    {
        var entity = new SampleEntity { Name = "Alice", Age = 30 };
        var tracker = ChangeTracker.For(entity);

        var changes = tracker.GetChanges();

        Assert.Empty(changes);
    }

    [Fact]
    public void GetChanges_PropertyChanged_ReturnsChange()
    {
        var entity = new SampleEntity { Name = "Alice", Age = 30 };
        var tracker = ChangeTracker.For(entity);

        entity.Name = "Bob";
        var changes = tracker.GetChanges();

        Assert.Single(changes);
        Assert.Equal("Name", changes[0].PropertyName);
        Assert.Equal("Alice", changes[0].OldValue);
        Assert.Equal("Bob", changes[0].NewValue);
    }

    [Fact]
    public void GetChanges_IgnoredProperty_NotTracked()
    {
        var entity = new SampleEntity { Internal = "old" };
        var tracker = ChangeTracker.For(entity);

        entity.Internal = "new";
        var changes = tracker.GetChanges();

        Assert.Empty(changes);
    }

    [Fact]
    public void GetChanges_SensitiveProperty_ValuesMasked()
    {
        var entity = new SampleEntity { Password = "secret" };
        var tracker = ChangeTracker.For(entity);

        entity.Password = "newsecret";
        var changes = tracker.GetChanges();

        Assert.Single(changes);
        Assert.Equal("***", changes[0].OldValue);
        Assert.Equal("***", changes[0].NewValue);
    }

    [Fact]
    public void GetChangeSet_ReturnsChangeSetWithTypeName()
    {
        var entity = new SampleEntity { Name = "Alice" };
        var tracker = ChangeTracker.For(entity);

        entity.Name = "Bob";
        var changeSet = tracker.GetChangeSet();

        Assert.Contains("SampleEntity", changeSet.TypeName);
        Assert.Single(changeSet.Changes);
    }
}
