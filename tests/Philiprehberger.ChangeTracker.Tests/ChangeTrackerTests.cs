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

public class Address
{
    public string City { get; set; } = "";
    public string Street { get; set; } = "";
    public string Zip { get; set; } = "";
}

[TrackChanges]
public class Customer
{
    public string Name { get; set; } = "";
    public Address Address { get; set; } = new();
}

[TrackChanges]
public class OrderEntity
{
    public List<string> Items { get; set; } = new();
    public List<int> Quantities { get; set; } = new();
}

[TrackChanges]
public class NestedWithNull
{
    public Address? OptionalAddress { get; set; }
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

    [Fact]
    public void GetChanges_NestedObject_ReturnsDotNotationPath()
    {
        var customer = new Customer
        {
            Name = "Alice",
            Address = new Address { City = "Berlin", Street = "Main St", Zip = "10115" }
        };
        var tracker = ChangeTracker.For(customer);

        customer.Address.City = "Munich";

        var changes = tracker.GetChanges();

        Assert.Single(changes);
        Assert.Equal("Address.City", changes[0].PropertyName);
        Assert.Equal("Berlin", changes[0].OldValue);
        Assert.Equal("Munich", changes[0].NewValue);
    }

    [Fact]
    public void GetChanges_NestedObject_MultipleNestedChanges()
    {
        var customer = new Customer
        {
            Name = "Alice",
            Address = new Address { City = "Berlin", Street = "Main St", Zip = "10115" }
        };
        var tracker = ChangeTracker.For(customer);

        customer.Address.City = "Munich";
        customer.Address.Street = "Other St";

        var changes = tracker.GetChanges();

        Assert.Equal(2, changes.Count);
        Assert.Contains(changes, c => c.PropertyName == "Address.City");
        Assert.Contains(changes, c => c.PropertyName == "Address.Street");
    }

    [Fact]
    public void GetChanges_NestedObject_NoChanges_ReturnsEmpty()
    {
        var customer = new Customer
        {
            Name = "Alice",
            Address = new Address { City = "Berlin" }
        };
        var tracker = ChangeTracker.For(customer);

        var changes = tracker.GetChanges();

        Assert.Empty(changes);
    }

    [Fact]
    public void GetChanges_NestedObjectNull_ToNonNull_ReturnsChange()
    {
        var entity = new NestedWithNull { OptionalAddress = null };
        var tracker = ChangeTracker.For(entity);

        entity.OptionalAddress = new Address { City = "Berlin" };
        var changes = tracker.GetChanges();

        Assert.Single(changes);
        Assert.Equal("OptionalAddress", changes[0].PropertyName);
        Assert.Null(changes[0].OldValue);
        Assert.NotNull(changes[0].NewValue);
    }

    [Fact]
    public void GetChanges_NestedObjectNonNull_ToNull_ReturnsChange()
    {
        var entity = new NestedWithNull { OptionalAddress = new Address { City = "Berlin" } };
        var tracker = ChangeTracker.For(entity);

        entity.OptionalAddress = null;
        var changes = tracker.GetChanges();

        Assert.Single(changes);
        Assert.Equal("OptionalAddress", changes[0].PropertyName);
        Assert.NotNull(changes[0].OldValue);
        Assert.Null(changes[0].NewValue);
    }

    [Fact]
    public void GetChanges_Collection_ElementModified_ReportsCollectionDiff()
    {
        var order = new OrderEntity { Items = new List<string> { "Apple", "Banana" } };
        var tracker = ChangeTracker.For(order);

        order.Items = new List<string> { "Apple", "Cherry" };
        var changes = tracker.GetChanges();

        Assert.Single(changes);
        Assert.Equal("Items", changes[0].PropertyName);
        Assert.NotNull(changes[0].CollectionDiff);

        var diff = changes[0].CollectionDiff!;
        Assert.Single(diff.Changes);
        Assert.Equal(1, diff.Changes[0].Index);
        Assert.Equal(CollectionChangeKind.Modified, diff.Changes[0].Kind);
        Assert.Equal("Banana", diff.Changes[0].OldValue);
        Assert.Equal("Cherry", diff.Changes[0].NewValue);
    }

    [Fact]
    public void GetChanges_Collection_ElementAdded_ReportsAddition()
    {
        var order = new OrderEntity { Items = new List<string> { "Apple" } };
        var tracker = ChangeTracker.For(order);

        order.Items = new List<string> { "Apple", "Banana" };
        var changes = tracker.GetChanges();

        Assert.Single(changes);
        var diff = changes[0].CollectionDiff!;
        Assert.Single(diff.Changes);
        Assert.Equal(1, diff.Changes[0].Index);
        Assert.Equal(CollectionChangeKind.Added, diff.Changes[0].Kind);
        Assert.Null(diff.Changes[0].OldValue);
        Assert.Equal("Banana", diff.Changes[0].NewValue);
    }

    [Fact]
    public void GetChanges_Collection_ElementRemoved_ReportsRemoval()
    {
        var order = new OrderEntity { Items = new List<string> { "Apple", "Banana" } };
        var tracker = ChangeTracker.For(order);

        order.Items = new List<string> { "Apple" };
        var changes = tracker.GetChanges();

        Assert.Single(changes);
        var diff = changes[0].CollectionDiff!;
        Assert.Single(diff.Changes);
        Assert.Equal(1, diff.Changes[0].Index);
        Assert.Equal(CollectionChangeKind.Removed, diff.Changes[0].Kind);
        Assert.Equal("Banana", diff.Changes[0].OldValue);
        Assert.Null(diff.Changes[0].NewValue);
    }

    [Fact]
    public void GetChanges_Collection_NoChanges_ReturnsEmpty()
    {
        var order = new OrderEntity { Items = new List<string> { "Apple", "Banana" } };
        var tracker = ChangeTracker.For(order);

        order.Items = new List<string> { "Apple", "Banana" };
        var changes = tracker.GetChanges();

        Assert.Empty(changes);
    }

    [Fact]
    public void GetChanges_Collection_MixedChanges_ReportsAll()
    {
        var order = new OrderEntity { Items = new List<string> { "Apple", "Banana" } };
        var tracker = ChangeTracker.For(order);

        order.Items = new List<string> { "Apple", "Cherry", "Date" };
        var changes = tracker.GetChanges();

        Assert.Single(changes);
        var diff = changes[0].CollectionDiff!;
        Assert.Equal(2, diff.Changes.Count);
        Assert.Equal(CollectionChangeKind.Modified, diff.Changes[0].Kind);
        Assert.Equal(CollectionChangeKind.Added, diff.Changes[1].Kind);
    }

    [Fact]
    public void Rollback_RevertsAllProperties()
    {
        var entity = new SampleEntity { Name = "Alice", Age = 30 };
        var tracker = ChangeTracker.For(entity);

        entity.Name = "Bob";
        entity.Age = 25;

        tracker.Rollback();

        Assert.Equal("Alice", entity.Name);
        Assert.Equal(30, entity.Age);
    }

    [Fact]
    public void Rollback_AfterRollback_GetChangesReturnsEmpty()
    {
        var entity = new SampleEntity { Name = "Alice", Age = 30 };
        var tracker = ChangeTracker.For(entity);

        entity.Name = "Bob";
        tracker.Rollback();

        var changes = tracker.GetChanges();
        Assert.Empty(changes);
    }

    [Fact]
    public void Rollback_WithNoChanges_LeavesObjectUnchanged()
    {
        var entity = new SampleEntity { Name = "Alice", Age = 30 };
        var tracker = ChangeTracker.For(entity);

        tracker.Rollback();

        Assert.Equal("Alice", entity.Name);
        Assert.Equal(30, entity.Age);
    }

    [Fact]
    public void Rollback_RevertsCollectionProperty()
    {
        var order = new OrderEntity { Items = new List<string> { "Apple", "Banana" } };
        var tracker = ChangeTracker.For(order);

        order.Items = new List<string> { "Cherry" };

        tracker.Rollback();

        Assert.Equal(2, order.Items.Count);
        Assert.Equal("Apple", order.Items[0]);
        Assert.Equal("Banana", order.Items[1]);
    }

    [Fact]
    public void GetChanges_SimplePropertyChange_HasNullCollectionDiff()
    {
        var entity = new SampleEntity { Name = "Alice" };
        var tracker = ChangeTracker.For(entity);

        entity.Name = "Bob";
        var changes = tracker.GetChanges();

        Assert.Single(changes);
        Assert.Null(changes[0].CollectionDiff);
    }
}
