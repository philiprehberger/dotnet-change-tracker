# Philiprehberger.ChangeTracker

[![CI](https://github.com/philiprehberger/dotnet-change-tracker/actions/workflows/ci.yml/badge.svg)](https://github.com/philiprehberger/dotnet-change-tracker/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Philiprehberger.ChangeTracker.svg)](https://www.nuget.org/packages/Philiprehberger.ChangeTracker)
[![Last updated](https://img.shields.io/github/last-commit/philiprehberger/dotnet-change-tracker)](https://github.com/philiprehberger/dotnet-change-tracker/commits/main)

Track and diff property changes on objects over time for audit logging.

## Installation

```bash
dotnet add package Philiprehberger.ChangeTracker
```

## Usage

Mark your class with `[TrackChanges]`, then create a tracker and inspect changes:

```csharp
using Philiprehberger.ChangeTracker;

[TrackChanges]
public class User
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";

    [IgnoreChanges]
    public DateTime LastLogin { get; set; }

    [SensitiveProperty]
    public string Password { get; set; } = "";
}

var user = new User { Name = "Alice", Email = "alice@example.com", Password = "secret" };
var tracker = ChangeTracker.For(user);

user.Name = "Bob";
user.Email = "bob@example.com";
user.Password = "new-secret";
user.LastLogin = DateTime.UtcNow; // ignored

IReadOnlyList<PropertyChange> changes = tracker.GetChanges();
// [
//   PropertyChange { PropertyName = "Name", OldValue = "Alice", NewValue = "Bob", ... },
//   PropertyChange { PropertyName = "Email", OldValue = "alice@example.com", NewValue = "bob@example.com", ... },
//   PropertyChange { PropertyName = "Password", OldValue = "***", NewValue = "***", ... }
// ]
```

### Nested Object Tracking

Properties that are complex objects are tracked recursively with dot-notation paths:

```csharp
public class Address
{
    public string City { get; set; } = "";
    public string Street { get; set; } = "";
}

[TrackChanges]
public class Customer
{
    public string Name { get; set; } = "";
    public Address Address { get; set; } = new();
}

var customer = new Customer
{
    Name = "Alice",
    Address = new Address { City = "Berlin", Street = "Main St" }
};
var tracker = ChangeTracker.For(customer);

customer.Address.City = "Munich";

var changes = tracker.GetChanges();
// PropertyChange { PropertyName = "Address.City", OldValue = "Berlin", NewValue = "Munich" }
```

### Collection Diff

When a tracked list property changes, individual element additions, removals, and modifications are reported:

```csharp
[TrackChanges]
public class Order
{
    public List<string> Items { get; set; } = new();
}

var order = new Order { Items = new List<string> { "Apple", "Banana" } };
var tracker = ChangeTracker.For(order);

order.Items = new List<string> { "Apple", "Cherry", "Date" };

var changes = tracker.GetChanges();
// changes[0].CollectionDiff.Changes:
//   CollectionChange { Index = 1, Kind = Modified, OldValue = "Banana", NewValue = "Cherry" }
//   CollectionChange { Index = 2, Kind = Added, OldValue = null, NewValue = "Date" }
```

### Rollback

Revert the tracked object to its snapshot state:

```csharp
var user = new User { Name = "Alice", Email = "alice@example.com" };
var tracker = ChangeTracker.For(user);

user.Name = "Bob";
user.Email = "bob@example.com";

tracker.Rollback();
// user.Name == "Alice", user.Email == "alice@example.com"
```

### JSON Serialization

```csharp
ChangeSet changeSet = tracker.GetChangeSet();
string json = changeSet.ToJson();

ChangeSet restored = ChangeSet.FromJson(json);
```

## API

### `ChangeTracker`

| Method | Description |
|--------|-------------|
| `For<T>(T target)` | Creates a new `ChangeTracker<T>` that snapshots the target's current state |

### `ChangeTracker<T>`

| Method | Description |
|--------|-------------|
| `GetChanges()` | Returns an `IReadOnlyList<PropertyChange>` of properties that differ from the snapshot |
| `GetChangeSet()` | Returns a `ChangeSet` wrapping all changes with type metadata and timestamp |
| `Rollback()` | Reverts the tracked object to the snapshot state taken at construction time |

### `PropertyChange`

| Property | Type | Description |
|----------|------|-------------|
| `PropertyName` | `string` | Name of the changed property (dot-notation for nested properties) |
| `OldValue` | `object?` | Original value (masked as `"***"` for sensitive properties) |
| `NewValue` | `object?` | Current value (masked as `"***"` for sensitive properties) |
| `Timestamp` | `DateTimeOffset` | When the change was detected |
| `CollectionDiff` | `CollectionDiff?` | Element-level diff for collection properties, `null` otherwise |

### `CollectionDiff`

| Property | Type | Description |
|----------|------|-------------|
| `Changes` | `IReadOnlyList<CollectionChange>` | Individual element changes within the collection |

### `CollectionChange`

| Property | Type | Description |
|----------|------|-------------|
| `Index` | `int` | Zero-based index of the element |
| `Kind` | `CollectionChangeKind` | `Added`, `Removed`, or `Modified` |
| `OldValue` | `object?` | Original value (`null` for additions) |
| `NewValue` | `object?` | Current value (`null` for removals) |

### `ChangeSet`

| Member | Description |
|--------|-------------|
| `TypeName` | Full name of the tracked type |
| `Changes` | `IReadOnlyList<PropertyChange>` of detected changes |
| `TrackedAt` | Timestamp when tracking began |
| `ToJson()` | Serializes the change set to JSON |
| `FromJson(string)` | Deserializes a change set from JSON |

### Attributes

| Attribute | Target | Description |
|-----------|--------|-------------|
| `[TrackChanges]` | Class | Required. Opts a class into change tracking |
| `[IgnoreChanges]` | Property | Excludes the property from tracking |
| `[SensitiveProperty]` | Property | Masks old/new values with `"***"` in change records |

## Development

```bash
dotnet build src/Philiprehberger.ChangeTracker.csproj --configuration Release
dotnet test tests/Philiprehberger.ChangeTracker.Tests/Philiprehberger.ChangeTracker.Tests.csproj --configuration Release
```

## Support

If you find this project useful:

⭐ [Star the repo](https://github.com/philiprehberger/dotnet-change-tracker)

🐛 [Report issues](https://github.com/philiprehberger/dotnet-change-tracker/issues?q=is%3Aissue+is%3Aopen+label%3Abug)

💡 [Suggest features](https://github.com/philiprehberger/dotnet-change-tracker/issues?q=is%3Aissue+is%3Aopen+label%3Aenhancement)

❤️ [Sponsor development](https://github.com/sponsors/philiprehberger)

🌐 [All Open Source Projects](https://philiprehberger.com/open-source-packages)

💻 [GitHub Profile](https://github.com/philiprehberger)

🔗 [LinkedIn Profile](https://www.linkedin.com/in/philiprehberger)

## License

[MIT](LICENSE)
