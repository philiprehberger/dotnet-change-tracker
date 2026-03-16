using System.Reflection;

namespace Philiprehberger.ChangeTracker;

/// <summary>
/// Provides a static factory for creating <see cref="ChangeTracker{T}"/> instances.
/// </summary>
public static class ChangeTracker
{
    /// <summary>
    /// Creates a new change tracker for the specified target object.
    /// The tracker snapshots the current property values immediately.
    /// </summary>
    /// <typeparam name="T">The type of the target object. Must have <see cref="TrackChangesAttribute"/>.</typeparam>
    /// <param name="target">The object to track.</param>
    /// <returns>A new <see cref="ChangeTracker{T}"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="target"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <typeparamref name="T"/> does not have <see cref="TrackChangesAttribute"/>.</exception>
    public static ChangeTracker<T> For<T>(T target) where T : class => new(target);
}

/// <summary>
/// Tracks property changes on an object by comparing current values against
/// an initial snapshot taken at construction time. Respects
/// <see cref="TrackChangesAttribute"/>, <see cref="IgnoreChangesAttribute"/>,
/// and <see cref="SensitivePropertyAttribute"/> attributes.
/// </summary>
/// <typeparam name="T">The type of the tracked object.</typeparam>
public sealed class ChangeTracker<T> where T : class
{
    private readonly T _target;
    private readonly Dictionary<string, object?> _snapshot;
    private readonly PropertyInfo[] _trackedProperties;
    private readonly HashSet<string> _sensitiveProperties;
    private readonly DateTimeOffset _trackedAt;

    private const string MaskedValue = "***";

    internal ChangeTracker(T target)
    {
        _target = target ?? throw new ArgumentNullException(nameof(target));

        var type = typeof(T);
        if (type.GetCustomAttribute<TrackChangesAttribute>() is null)
        {
            throw new InvalidOperationException(
                $"Type '{type.FullName}' must be decorated with [TrackChanges] to enable change tracking.");
        }

        _trackedProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetCustomAttribute<IgnoreChangesAttribute>() is null)
            .ToArray();

        _sensitiveProperties = _trackedProperties
            .Where(p => p.GetCustomAttribute<SensitivePropertyAttribute>() is not null)
            .Select(p => p.Name)
            .ToHashSet();

        _snapshot = new Dictionary<string, object?>();
        foreach (var prop in _trackedProperties)
        {
            _snapshot[prop.Name] = prop.GetValue(_target);
        }

        _trackedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Compares the current property values of the tracked object against the
    /// initial snapshot and returns a list of detected changes.
    /// </summary>
    /// <returns>A read-only list of <see cref="PropertyChange"/> records for properties that differ from the snapshot.</returns>
    public IReadOnlyList<PropertyChange> GetChanges()
    {
        var changes = new List<PropertyChange>();
        var now = DateTimeOffset.UtcNow;

        foreach (var prop in _trackedProperties)
        {
            var oldValue = _snapshot[prop.Name];
            var newValue = prop.GetValue(_target);

            if (Equals(oldValue, newValue))
                continue;

            var isSensitive = _sensitiveProperties.Contains(prop.Name);

            changes.Add(new PropertyChange(
                PropertyName: prop.Name,
                OldValue: isSensitive ? MaskedValue : oldValue,
                NewValue: isSensitive ? MaskedValue : newValue,
                Timestamp: now));
        }

        return changes.AsReadOnly();
    }

    /// <summary>
    /// Computes the current changes and wraps them in a <see cref="ChangeSet"/>
    /// with type metadata and the tracking start timestamp.
    /// </summary>
    /// <returns>A <see cref="ChangeSet"/> containing all detected changes.</returns>
    public ChangeSet GetChangeSet()
    {
        var changes = GetChanges();
        return new ChangeSet(
            TypeName: typeof(T).FullName ?? typeof(T).Name,
            Changes: changes,
            TrackedAt: _trackedAt);
    }
}
