namespace Tuvima.WikidataReconciliation;

/// <summary>
/// The kind of value stored in a Wikidata claim.
/// </summary>
public enum WikidataValueKind
{
    /// <summary>Plain string value.</summary>
    String,

    /// <summary>Reference to another Wikidata entity (QID).</summary>
    EntityId,

    /// <summary>Date/time value with precision.</summary>
    Time,

    /// <summary>Numeric quantity, optionally with a unit.</summary>
    Quantity,

    /// <summary>Geographic coordinates (latitude/longitude).</summary>
    GlobeCoordinate,

    /// <summary>Text tagged with a language code.</summary>
    MonolingualText,

    /// <summary>Value type not recognized by this library version.</summary>
    Unknown
}

/// <summary>
/// A typed value from a Wikidata claim or qualifier.
/// Use <see cref="Kind"/> to determine which properties are populated.
/// </summary>
public sealed class WikidataValue
{
    /// <summary>
    /// The kind of value, determining which typed properties are populated.
    /// </summary>
    public WikidataValueKind Kind { get; init; }

    /// <summary>
    /// Raw string representation of the value. Always populated.
    /// </summary>
    public required string RawValue { get; init; }

    /// <summary>
    /// For EntityId values: the entity ID (e.g., "Q42"). Null for other kinds.
    /// </summary>
    public string? EntityId { get; init; }

    /// <summary>
    /// For Time values: precision level (9=year, 10=month, 11=day). Null for other kinds.
    /// </summary>
    public int? TimePrecision { get; init; }

    /// <summary>
    /// For Quantity values: the numeric amount. Null for other kinds.
    /// </summary>
    public decimal? Amount { get; init; }

    /// <summary>
    /// For Quantity values: the unit entity URI (e.g., "http://www.wikidata.org/entity/Q11573" for metres).
    /// Null if dimensionless or for other kinds.
    /// </summary>
    public string? Unit { get; init; }

    /// <summary>
    /// For GlobeCoordinate values: latitude in decimal degrees. Null for other kinds.
    /// </summary>
    public double? Latitude { get; init; }

    /// <summary>
    /// For GlobeCoordinate values: longitude in decimal degrees. Null for other kinds.
    /// </summary>
    public double? Longitude { get; init; }

    /// <summary>
    /// For MonolingualText values: the language code. Null for other kinds.
    /// </summary>
    public string? Language { get; init; }
}
