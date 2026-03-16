using System.Text.Json;
using System.Text.Json.Serialization;

namespace Philiprehberger.ChangeTracker;

/// <summary>
/// An immutable collection of property changes for a tracked type,
/// with JSON serialization support for audit logging.
/// </summary>
/// <param name="TypeName">The full name of the tracked type.</param>
/// <param name="Changes">The list of property changes detected.</param>
/// <param name="TrackedAt">The timestamp when the change set was created.</param>
public record ChangeSet(
    string TypeName,
    IReadOnlyList<PropertyChange> Changes,
    DateTimeOffset TrackedAt)
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Serializes this change set to a JSON string.
    /// </summary>
    /// <returns>A JSON representation of the change set.</returns>
    public string ToJson() => JsonSerializer.Serialize(this, SerializerOptions);

    /// <summary>
    /// Deserializes a change set from a JSON string.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized <see cref="ChangeSet"/>.</returns>
    /// <exception cref="JsonException">Thrown when the JSON is invalid.</exception>
    public static ChangeSet FromJson(string json) =>
        JsonSerializer.Deserialize<ChangeSet>(json, SerializerOptions)
        ?? throw new JsonException("Failed to deserialize ChangeSet: result was null.");
}
