using System.Collections;
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
/// Supports nested object tracking with dot-notation paths, collection diffs,
/// and rollback to a previous snapshot state.
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

        _snapshot = SnapshotProperties(_target, _trackedProperties);
        _trackedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Compares the current property values of the tracked object against the
    /// initial snapshot and returns a list of detected changes. Nested complex
    /// properties produce dot-notation paths (e.g. <c>Address.City</c>).
    /// Collection properties produce individual element-level changes.
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
            var isSensitive = _sensitiveProperties.Contains(prop.Name);

            CollectChanges(changes, prop.Name, prop.PropertyType, oldValue, newValue, isSensitive, now);
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

    /// <summary>
    /// Reverts the tracked object to its snapshot state by copying all snapshotted
    /// property values back to the target. Only writable properties are reverted.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a tracked property cannot be written to.
    /// </exception>
    public void Rollback()
    {
        foreach (var prop in _trackedProperties)
        {
            if (!prop.CanWrite)
            {
                throw new InvalidOperationException(
                    $"Property '{prop.Name}' on type '{typeof(T).FullName}' is read-only and cannot be rolled back.");
            }

            prop.SetValue(_target, _snapshot[prop.Name]);
        }
    }

    private static void CollectChanges(
        List<PropertyChange> changes,
        string path,
        Type propertyType,
        object? oldValue,
        object? newValue,
        bool isSensitive,
        DateTimeOffset timestamp)
    {
        if (isSensitive)
        {
            if (!Equals(oldValue, newValue))
            {
                changes.Add(new PropertyChange(path, MaskedValue, MaskedValue, timestamp));
            }

            return;
        }

        // Collection diff
        if (IsCollectionType(propertyType) && (oldValue is IList || newValue is IList))
        {
            var oldList = oldValue as IList;
            var newList = newValue as IList;

            if (oldList is null && newList is null)
                return;

            if (oldList is null || newList is null)
            {
                changes.Add(new PropertyChange(path, oldValue, newValue, timestamp));
                return;
            }

            var collectionChanges = ComputeCollectionDiff(oldList, newList);
            if (collectionChanges.Count > 0)
            {
                changes.Add(new PropertyChange(
                    path,
                    oldValue,
                    newValue,
                    timestamp,
                    new CollectionDiff(collectionChanges)));
            }

            return;
        }

        // Nested object tracking
        if (IsNestedTrackableType(propertyType))
        {
            if (oldValue is null && newValue is null)
                return;

            if (oldValue is null || newValue is null)
            {
                changes.Add(new PropertyChange(path, oldValue, newValue, timestamp));
                return;
            }

            var nestedProperties = propertyType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead)
                .ToArray();

            foreach (var nestedProp in nestedProperties)
            {
                var nestedOld = nestedProp.GetValue(oldValue);
                var nestedNew = nestedProp.GetValue(newValue);
                var nestedPath = $"{path}.{nestedProp.Name}";

                CollectChanges(changes, nestedPath, nestedProp.PropertyType, nestedOld, nestedNew, false, timestamp);
            }

            return;
        }

        // Simple value comparison
        if (!Equals(oldValue, newValue))
        {
            changes.Add(new PropertyChange(path, oldValue, newValue, timestamp));
        }
    }

    private static IReadOnlyList<CollectionChange> ComputeCollectionDiff(IList oldList, IList newList)
    {
        var changes = new List<CollectionChange>();
        var maxShared = Math.Min(oldList.Count, newList.Count);

        for (var i = 0; i < maxShared; i++)
        {
            if (!Equals(oldList[i], newList[i]))
            {
                changes.Add(new CollectionChange(i, CollectionChangeKind.Modified, oldList[i], newList[i]));
            }
        }

        for (var i = maxShared; i < oldList.Count; i++)
        {
            changes.Add(new CollectionChange(i, CollectionChangeKind.Removed, oldList[i], null));
        }

        for (var i = maxShared; i < newList.Count; i++)
        {
            changes.Add(new CollectionChange(i, CollectionChangeKind.Added, null, newList[i]));
        }

        return changes.AsReadOnly();
    }

    private static bool IsCollectionType(Type type)
    {
        if (type == typeof(string))
            return false;

        return typeof(IList).IsAssignableFrom(type);
    }

    private static bool IsNestedTrackableType(Type type)
    {
        if (type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal)
            || type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(Guid)
            || type == typeof(TimeSpan))
            return false;

        if (Nullable.GetUnderlyingType(type) is not null)
            return false;

        if (typeof(IEnumerable).IsAssignableFrom(type))
            return false;

        return type.IsClass || type.IsValueType;
    }

    private static Dictionary<string, object?> SnapshotProperties(T target, PropertyInfo[] properties)
    {
        var snapshot = new Dictionary<string, object?>();
        foreach (var prop in properties)
        {
            var value = prop.GetValue(target);

            // Deep-copy nested objects so the snapshot is isolated from mutations
            if (value is not null && IsNestedTrackableType(prop.PropertyType))
            {
                snapshot[prop.Name] = ShallowCloneObject(value, prop.PropertyType);
            }
            else if (value is IList list)
            {
                snapshot[prop.Name] = CloneList(list);
            }
            else
            {
                snapshot[prop.Name] = value;
            }
        }

        return snapshot;
    }

    private static object ShallowCloneObject(object source, Type type)
    {
        var clone = Activator.CreateInstance(type)!;
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (prop.CanRead && prop.CanWrite)
            {
                prop.SetValue(clone, prop.GetValue(source));
            }
        }

        return clone;
    }

    private static IList CloneList(IList source)
    {
        var listType = source.GetType();
        var clone = (IList)Activator.CreateInstance(listType)!;
        foreach (var item in source)
        {
            clone.Add(item);
        }

        return clone;
    }
}
