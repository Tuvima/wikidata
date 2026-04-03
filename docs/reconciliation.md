# Reconciliation

Match text (names, titles, places) to Wikidata entities with confidence scoring.

## Basic Usage

```csharp
using Tuvima.Wikidata;

using var reconciler = new WikidataReconciler();

// Simple lookup
var results = await reconciler.ReconcileAsync("Douglas Adams");

// With type filter — only match humans (Q5)
var results = await reconciler.ReconcileAsync("Douglas Adams", "Q5");

// With type filter — only match literary works (Q7725634)
var results = await reconciler.ReconcileAsync("1984", "Q7725634");
```

## Property Constraints

Supply known property values to improve scoring:

```csharp
var results = await reconciler.ReconcileAsync(new ReconciliationRequest
{
    Query = "Douglas Adams",
    Type = "Q5",
    Limit = 5,
    Properties =
    [
        new PropertyConstraint("P27", "Q145"),         // country of citizenship: UK
        new PropertyConstraint("P569", "1952-03-11"),  // date of birth
    ]
});
```

Property values can be:

| Data type | Example value | Description |
|---|---|---|
| Item (QID) | `"Q145"` | Exact entity match |
| String | `"Douglas Adams"` | Fuzzy string match (token-sort-ratio) |
| External ID | `"118500902"` | Exact match (e.g., GND identifier) |
| Date | `"1952-03-11"` | Precision-aware (year, month, or full date) |
| Quantity | `"42"` | Log-decay curve for numeric proximity |
| URL | `"https://example.com"` | Scheme-normalized exact match |
| Coordinates | `"51.5074,-0.1278"` | Distance-based (score decreases to 0 at 1 km) |

### Multi-Value Property Constraints

When an entity has multiple values for a property (e.g., a book with multiple authors), provide all expected values for proportional scoring:

```csharp
var results = await reconciler.ReconcileAsync(new ReconciliationRequest
{
    Query = "Good Omens",
    Properties =
    [
        new PropertyConstraint
        {
            PropertyId = "P50",  // author
            Values = ["Neil Gaiman", "Terry Pratchett"]
        }
    ]
});
// Candidates matching both authors score higher than those matching only one
```

### Property Paths

Chain properties to match against related entities:

```csharp
var results = await reconciler.ReconcileAsync(new ReconciliationRequest
{
    Query = "Douglas Adams",
    Properties =
    [
        new PropertyConstraint("P19", "Q350"),     // place of birth: Cambridge (direct)
        new PropertyConstraint("P19/P17", "Q145"), // place of birth -> country: UK (chained)
    ]
});
```

## Exclude Types

Remove candidates of specific types:

```csharp
var results = await reconciler.ReconcileAsync(new ReconciliationRequest
{
    Query = "Cambridge",
    ExcludeTypes = ["Q17442446"],  // exclude Wikimedia internal items
});
```

## Batch Reconciliation

Reconcile multiple queries with automatic concurrency limiting (default: 5 concurrent requests):

```csharp
var results = await reconciler.ReconcileBatchAsync([
    new ReconciliationRequest { Query = "Douglas Adams", Type = "Q5" },
    new ReconciliationRequest { Query = "Albert Einstein", Type = "Q5" },
    new ReconciliationRequest { Query = "Nineteen Eighty-Four", Type = "Q7725634" },
]);
```

### Streaming Batch

For large datasets, use `ReconcileBatchStreamAsync` to process results as they arrive:

```csharp
var requests = LoadThousandsOfRequests();
var completed = 0;

await foreach (var (index, results) in reconciler.ReconcileBatchStreamAsync(requests))
{
    completed++;
    Console.WriteLine($"[{completed}/{requests.Count}] {requests[index].Query} -> {results[0].Id}");
    SaveResult(index, results);
}
```

## Suggest / Autocomplete

For interactive UIs with type-ahead search:

```csharp
var entities = await reconciler.SuggestAsync("Douglas");
var properties = await reconciler.SuggestPropertiesAsync("date");
var types = await reconciler.SuggestTypesAsync("book");
```

## Multi-Type Filtering with CirrusSearch

Filter by multiple types (OR logic) with per-request subclass depth override:

```csharp
var results = await reconciler.ReconcileAsync(new ReconciliationRequest
{
    Query = "Shogun",
    Types = ["Q5398426", "Q15416"],  // TV series OR TV program
    TypeHierarchyDepth = 3,          // walk P279 up to 3 levels
});
```

## Multi-Language Search

Search in multiple languages concurrently (results deduplicated by QID):

```csharp
var results = await reconciler.ReconcileAsync(new ReconciliationRequest
{
    Query = "千と千尋の神隠し",
    Languages = ["ja", "en"],
});
```

## Diacritic-Insensitive Search

Match entities regardless of accents:

```csharp
var results = await reconciler.ReconcileAsync(new ReconciliationRequest
{
    Query = "Shogun",
    DiacriticInsensitive = true,  // matches "Shogun"
});
```

## Query Pre-Cleaning

Strip noise from queries before search:

```csharp
var results = await reconciler.ReconcileAsync(new ReconciliationRequest
{
    Query = "The Hitchhiker's Guide to the Galaxy (Unabridged)",
    Cleaners = [QueryCleaners.StripParenthetical()],
});

// Or use all built-in cleaners at once
var results = await reconciler.ReconcileAsync(new ReconciliationRequest
{
    Query = "Dune: Part Two S01E03 (Special Edition)",
    Cleaners = QueryCleaners.All(),
});
```

## Score Breakdown

Every result includes a detailed `Breakdown` explaining how the score was computed:

```csharp
var results = await reconciler.ReconcileAsync(new ReconciliationRequest
{
    Query = "Douglas Adams",
    Type = "Q5",
    Properties = [new PropertyConstraint("P27", "Q145")]
});

var b = results[0].Breakdown!;
Console.WriteLine($"Label match:    {b.LabelScore}");
Console.WriteLine($"P27 match:      {b.PropertyScores["P27"]}");
Console.WriteLine($"Type matched:   {b.TypeMatched}");
Console.WriteLine($"Weighted score: {b.WeightedScore}");
Console.WriteLine($"Type penalty:   {b.TypePenaltyApplied}");
```

## Reverse Lookup by External ID

Find an entity by ISBN, IMDB ID, ORCID, or any external identifier:

```csharp
var results = await reconciler.LookupByExternalIdAsync("P214", "113230702"); // VIAF
var results = await reconciler.LookupByExternalIdAsync("P212", "978-0-345-39180-3"); // ISBN
var results = await reconciler.LookupByExternalIdAsync("P345", "tt0371724"); // IMDB
```

## Direct QID Lookup

If you already have a QID:

```csharp
var results = await reconciler.ReconcileAsync("Q42");
// results[0].Id == "Q42", Score == 100
```

## Result Object

Each `ReconciliationResult` contains:

| Property | Type | Description |
|---|---|---|
| `Id` | `string` | Wikidata entity ID (e.g., `"Q42"`) |
| `Name` | `string` | Entity label in the requested language |
| `Description` | `string?` | Entity description |
| `Score` | `double` | Confidence score from 0 to 100 |
| `Match` | `bool` | `true` if this is a confident automatic match |
| `Types` | `IReadOnlyList<string>?` | P31 type IDs |
| `MatchedLabel` | `string?` | The label/alias that best matched the query |
| `Breakdown` | `ScoreBreakdown?` | Detailed scoring breakdown |

Results are sorted by score descending, with QID number as a tiebreaker (lower QID = older, more established entity).
