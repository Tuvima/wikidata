using System.Diagnostics.CodeAnalysis;

namespace Tuvima.Wikidata;

/// <summary>
/// A property-value constraint used to improve reconciliation scoring.
/// The constraint is satisfied by matching any of the supplied <see cref="Values"/>
/// against the entity's claim values for the given property.
/// </summary>
public sealed class PropertyConstraint
{
    /// <summary>
    /// The Wikidata property ID (e.g., "P569" for date of birth, "P27" for country of citizenship).
    /// </summary>
    public required string PropertyId { get; init; }

    /// <summary>
    /// One or more expected values for this property. Each value is compared against
    /// each of the entity's claim values using fuzzy matching appropriate to the property's
    /// data type. The property score is the average of the best match score for each
    /// constraint value — candidates matching more values score proportionally higher.
    /// Values can be QIDs ("Q145"), strings, dates ("1952-03-11"), numbers, coordinates
    /// ("51.5,-0.1"), or URLs.
    /// </summary>
    public required IReadOnlyList<string> Values { get; init; }

    public PropertyConstraint() { }

    /// <summary>
    /// Convenience constructor for a single-value constraint.
    /// </summary>
    [SetsRequiredMembers]
    public PropertyConstraint(string propertyId, string value)
    {
        PropertyId = propertyId;
        Values = [value];
    }

    /// <summary>
    /// Constructor for a multi-value constraint.
    /// </summary>
    [SetsRequiredMembers]
    public PropertyConstraint(string propertyId, IReadOnlyList<string> values)
    {
        PropertyId = propertyId;
        Values = values;
    }
}
