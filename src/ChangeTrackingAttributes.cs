namespace Philiprehberger.ChangeTracker;

/// <summary>
/// Marks a class for property change tracking. Apply to classes whose
/// property changes should be recorded by <see cref="ChangeTracker{T}"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TrackChangesAttribute : Attribute;

/// <summary>
/// Excludes a property from change tracking. Properties marked with this
/// attribute will be ignored when computing changes.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class IgnoreChangesAttribute : Attribute;

/// <summary>
/// Marks a property as sensitive. Change records for sensitive properties
/// will have their values masked with <c>"***"</c> instead of showing the
/// actual old and new values.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class SensitivePropertyAttribute : Attribute;
