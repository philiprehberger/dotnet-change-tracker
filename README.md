# Philiprehberger.ChangeTracker

[![CI](https://github.com/philiprehberger/dotnet-change-tracker/actions/workflows/ci.yml/badge.svg)](https://github.com/philiprehberger/dotnet-change-tracker/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Philiprehberger.ChangeTracker.svg)](https://www.nuget.org/packages/Philiprehberger.ChangeTracker)
[![License](https://img.shields.io/github/license/philiprehberger/dotnet-change-tracker)](LICENSE)

Track and diff property changes on objects over time for audit logging.

## Install

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

### `PropertyChange`

| Property | Type | Description |
|----------|------|-------------|
| `PropertyName` | `string` | Name of the changed property |
| `OldValue` | `object?` | Original value (masked as `"***"` for sensitive properties) |
| `NewValue` | `object?` | Current value (masked as `"***"` for sensitive properties) |
| `Timestamp` | `DateTimeOffset` | When the change was detected |

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
```

## License

MIT
