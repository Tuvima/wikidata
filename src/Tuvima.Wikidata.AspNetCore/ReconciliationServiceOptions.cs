namespace Tuvima.Wikidata.AspNetCore;

/// <summary>
/// Configuration for the W3C Reconciliation Service API manifest and request limits.
/// </summary>
public sealed class ReconciliationServiceOptions
{
    /// <summary>Maximum queries in one POST batch. Default is 100; must be positive.</summary>
    public int MaxBatchSize { get; set; } = 100;

    /// <summary>Maximum characters in a query, type, property ID/value, or suggest prefix. Default is 500.</summary>
    public int MaxQueryLength { get; set; } = 500;

    /// <summary>Maximum requested candidates per query. Default is 50; must be positive.</summary>
    public int MaxResultLimit { get; set; } = 50;

    /// <summary>Maximum property constraints per query. Default is 25; must be positive.</summary>
    public int MaxPropertiesPerQuery { get; set; } = 25;

    /// <summary>Maximum POST body size in bytes, including form encoding. Default is 1 MiB.</summary>
    public int MaxRequestBodyBytes { get; set; } = 1024 * 1024;

    /// <summary>
    /// The display name of the reconciliation service.
    /// </summary>
    public string ServiceName { get; set; } = "Wikidata Reconciliation Service";

    /// <summary>
    /// The identifier space URI. Default is the Wikidata entity namespace.
    /// </summary>
    public string IdentifierSpace { get; set; } = "http://www.wikidata.org/entity/";

    /// <summary>
    /// The schema space URI. Default is the Wikidata property namespace.
    /// </summary>
    public string SchemaSpace { get; set; } = "http://www.wikidata.org/prop/direct/";

    /// <summary>
    /// URL template for viewing entities. Use {{id}} as a placeholder.
    /// </summary>
    public string EntityViewUrl { get; set; } = "https://www.wikidata.org/wiki/{{id}}";

    /// <summary>
    /// Default types shown in the reconciliation UI.
    /// </summary>
    public List<DefaultType> DefaultTypes { get; set; } =
    [
        new("Q5", "Human"),
        new("Q515", "City"),
        new("Q6256", "Country"),
        new("Q4830453", "Business"),
        new("Q7725634", "Literary work"),
        new("Q11424", "Film"),
        new("Q5398426", "Television series")
    ];
}

/// <summary>
/// A default type suggestion for the reconciliation UI.
/// </summary>
public sealed class DefaultType(string id, string name)
{
    public string Id { get; set; } = id;
    public string Name { get; set; } = name;
}
