namespace Philiprehberger.ChangeTracker;

/// <summary>
/// Describes the kind of change that occurred to a collection element.
/// </summary>
public enum CollectionChangeKind
{
    /// <summary>An element was added to the collection.</summary>
    Added,

    /// <summary>An element was removed from the collection.</summary>
    Removed,

    /// <summary>An element at the same index was modified.</summary>
    Modified
}

/// <summary>
/// Represents a single change within a collection property, capturing the index,
/// the kind of change, and the old and new values.
/// </summary>
/// <param name="Index">The zero-based index of the element in the collection.</param>
/// <param name="Kind">The kind of change (added, removed, or modified).</param>
/// <param name="OldValue">The original value, or <c>null</c> for additions.</param>
/// <param name="NewValue">The current value, or <c>null</c> for removals.</param>
public record CollectionChange(
    int Index,
    CollectionChangeKind Kind,
    object? OldValue,
    object? NewValue);

/// <summary>
/// Represents the diff of a collection property, containing individual element changes.
/// </summary>
/// <param name="Changes">The list of individual element changes within the collection.</param>
public record CollectionDiff(IReadOnlyList<CollectionChange> Changes);
