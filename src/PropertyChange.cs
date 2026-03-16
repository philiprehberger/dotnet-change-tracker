namespace Philiprehberger.ChangeTracker;

/// <summary>
/// Represents a single property change, capturing the property name,
/// old value, new value, and the timestamp when the change was detected.
/// </summary>
/// <param name="PropertyName">The name of the property that changed.</param>
/// <param name="OldValue">The original value of the property, or <c>null</c> if it was unset.</param>
/// <param name="NewValue">The current value of the property, or <c>null</c> if it was cleared.</param>
/// <param name="Timestamp">The time at which the change was detected.</param>
public record PropertyChange(
    string PropertyName,
    object? OldValue,
    object? NewValue,
    DateTimeOffset Timestamp);
