using System.Diagnostics.CodeAnalysis;

namespace Tuvima.Wikidata;

/// <summary>
/// A property-value constraint used to improve reconciliation scoring.
/// Supports single-value matching via <see cref="Value"/> or multi-value matching via <see cref="Values"/>.
/// </summary>
public sealed class PropertyConstraint
{
    /// <summary>
    /// The Wikidata property ID (e.g., "P569" for date of birth, "P27" for country of citizenship).
    /// </summary>
    public required string PropertyId { get; init; }

    /// <summary>
    /// The expected value. Can be a QID (e.g., "Q145"), a string, a date ("1952-03-11"),
    /// a number, coordinates ("51.5,-0.1"), or a URL.
    /// When <see cref="Values"/> is also set, <see cref="Values"/> takes precedence.
    /// </summary>
    public string? Value { get; init; }

    /// <summary>
    /// Multiple expected values for this property. When provided, each constraint value is
    /// compared against each of the entity's claim values using the same fuzzy matching logic.
    /// The property score is the average of the best match score for each constraint value.
    /// Candidates matching more values score proportionally higher.
    /// Takes precedence over <see cref="Value"/> when both are set.
    /// </summary>
    public IReadOnlyList<string>? Values { get; init; }

    public PropertyConstraint() { }

    [SetsRequiredMembers]
    public PropertyConstraint(string propertyId, string value)
    {
        PropertyId = propertyId;
        Value = value;
    }

    [SetsRequiredMembers]
    public PropertyConstraint(string propertyId, IReadOnlyList<string> values)
    {
        PropertyId = propertyId;
        Values = values;
    }

    /// <summary>
    /// Returns the effective values list for scoring. If <see cref="Values"/> is set, returns it.
    /// Otherwise wraps <see cref="Value"/> in a single-element list. Returns empty if neither is set.
    /// </summary>
    internal IReadOnlyList<string> GetEffectiveValues()
    {
        if (Values is { Count: > 0 })
            return Values;
        if (!string.IsNullOrEmpty(Value))
            return [Value];
        return [];
    }
}
