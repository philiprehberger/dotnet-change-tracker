namespace Philiprehberger.ChangeTracker;

/// <summary>
/// Represents a single property change, capturing the property name,
/// old value, new value, the timestamp when the change was detected,
/// and an optional collection diff for collection properties.
/// </summary>
/// <param name="PropertyName">The name of the property that changed. Uses dot-notation for nested properties (e.g. <c>Address.City</c>).</param>
/// <param name="OldValue">The original value of the property, or <c>null</c> if it was unset.</param>
/// <param name="NewValue">The current value of the property, or <c>null</c> if it was cleared.</param>
/// <param name="Timestamp">The time at which the change was detected.</param>
/// <param name="CollectionDiff">Optional element-level diff for collection properties. <c>null</c> for non-collection changes.</param>
public record PropertyChange(
    string PropertyName,
    object? OldValue,
    object? NewValue,
    DateTimeOffset Timestamp,
    CollectionDiff? CollectionDiff = null);
