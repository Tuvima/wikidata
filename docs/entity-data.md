# Entity Data & Wikipedia Content

Fetch structured entity data, Wikipedia summaries and sections, images, revision history, editions, and child entities.

## Fetch Entity Data

After reconciliation, fetch full entity data including claims with qualifiers:

```csharp
var entities = await reconciler.GetEntitiesAsync(["Q42"]);
var adams = entities["Q42"];

Console.WriteLine(adams.Label);       // "Douglas Adams"
Console.WriteLine(adams.Description); // "English author and humourist (1952-2001)"

// Access claims with typed values
foreach (var claim in adams.Claims["P31"])
    Console.WriteLine($"Instance of: {claim.Value?.EntityId}"); // "Q5"

// Access qualifiers (e.g., educated at with start/end dates)
foreach (var claim in adams.Claims["P69"])
{
    Console.WriteLine($"Educated at: {claim.Value?.EntityId}");
    if (claim.Qualifiers.TryGetValue("P580", out var startDates))
        Console.WriteLine($"  Start: {startDates[0].RawValue}");
}
```

### Specific Properties

Fetch only the properties you need:

```csharp
var props = await reconciler.GetPropertiesAsync(["Q42", "Q30"], ["P27", "P569"]);
var citizenship = props["Q42"]["P27"][0].Value?.EntityId; // "Q145" (UK)
```

Entity-valued properties automatically include human-readable labels:

```csharp
var props = await reconciler.GetPropertiesAsync(["Q42"], ["P27"]);
var country = props["Q42"]["P27"][0].Value;
// country.EntityId    -> "Q145"
// country.EntityLabel -> "United Kingdom"
```

### Entity Label Resolution

Auto-resolve entity-valued claims to human-readable labels:

```csharp
var entities = await reconciler.GetEntitiesAsync(["Q42"], resolveEntityLabels: true);
foreach (var claim in entities["Q42"].Claims["P27"])
    Console.WriteLine($"Citizenship: {claim.Value?.EntityLabel}"); // "United Kingdom"
```

## Wikipedia URLs

Resolve entities to validated Wikipedia article links:

```csharp
var urls = await reconciler.GetWikipediaUrlsAsync(["Q42", "Q30"]);
// urls["Q42"] = "https://en.wikipedia.org/wiki/Douglas_Adams"

var deUrls = await reconciler.GetWikipediaUrlsAsync(["Q42"], "de");
// deUrls["Q42"] = "https://de.wikipedia.org/wiki/Douglas_Adams"
```

## Wikipedia Summaries

Fetch article summaries (first paragraph, description, thumbnail):

```csharp
var summaries = await reconciler.GetWikipediaSummariesAsync(["Q42", "Q937"]);
foreach (var s in summaries)
{
    Console.WriteLine($"{s.Title}: {s.Extract}");
    Console.WriteLine($"  Thumbnail: {s.ThumbnailUrl}");
    Console.WriteLine($"  Read more: {s.ArticleUrl}");
}
```

### Language Fallback

```csharp
var summaries = await reconciler.GetWikipediaSummariesAsync(["Q42"], "ja",
    fallbackLanguages: ["zh", "en"]);
Console.WriteLine(summaries[0].Language); // actual language used
```

## Wikipedia Section Content

Fetch specific sections from Wikipedia articles:

```csharp
// Get table of contents
var sections = await reconciler.GetWikipediaSectionsAsync(["Q208460"]); // 1984 (novel)
var toc = sections["Q208460"];

foreach (var section in toc)
    Console.WriteLine($"{section.Number} [{section.Level}] {section.Title}");

// Fetch a specific section as plain text (heading auto-stripped)
var plotIndex = toc.First(s => s.Title == "Plot summary").Index;
var plot = await reconciler.GetWikipediaSectionContentAsync("Q208460", plotIndex);

// Fetch a section with all subsections as a structured list
var content = await reconciler.GetWikipediaSectionWithSubsectionsAsync("Q83495", plotIndex);
// content[0] = { Title: "Plot", Content: "The story follows..." }
// content[1] = { Title: "Season 1", Content: "Walter White is a..." }
```

## Property Labels

Resolve property IDs to human-readable names:

```csharp
var labels = await reconciler.GetPropertyLabelsAsync(["P569", "P27", "P31"]);
// labels["P569"] = "date of birth"
```

## Entity Images

Fetch Wikimedia Commons image URLs:

```csharp
var urls = await reconciler.GetImageUrlsAsync(["Q42", "Q937"]);

// Or build URLs from any WikidataValue
var imageUrl = entity.Claims["P18"][0].Value?.ToCommonsImageUrl();
```

## Value Formatting

```csharp
var dob = entity.Claims["P569"][0].Value!;
Console.WriteLine(dob.ToDisplayString()); // "11 March 1952"

var coords = entity.Claims["P625"][0].Value!;
Console.WriteLine(coords.ToDisplayString()); // "51.5074, -0.1278"
```

## Staleness Detection

Check if cached entities have been modified:

```csharp
// Initial fetch — LastRevisionId and Modified come automatically
var entities = await reconciler.GetEntitiesAsync(["Q42", "Q5"]);
var cached = entities.ToDictionary(e => e.Key, e => (Entity: e.Value, Rev: e.Value.LastRevisionId));

// Later — lightweight check (no labels/claims fetched)
var currentRevs = await reconciler.GetRevisionIdsAsync(cached.Keys.ToList());
var stale = currentRevs.Where(r => cached[r.Key].Rev != r.Value.RevisionId).ToList();

// Only re-fetch what changed
if (stale.Count > 0)
{
    var refreshed = await reconciler.GetEntitiesAsync(stale.Select(s => s.Key).ToList());
}
```

## Entity Change Monitoring

Get detailed edit history for watched entities:

```csharp
var changes = await reconciler.GetRecentChangesAsync(
    ["Q42", "Q30"], since: DateTimeOffset.UtcNow.AddDays(-7));

foreach (var change in changes)
    Console.WriteLine($"{change.EntityId} changed at {change.Timestamp} by {change.User}");
```

## Work-to-Edition Pivoting

Navigate between works and their editions/translations:

```csharp
var editions = await reconciler.GetEditionsAsync("Q190192"); // Hitchhiker's Guide
var audiobooks = await reconciler.GetEditionsAsync("Q190192", filterTypes: ["Q122731938"]);
var work = await reconciler.GetWorkForEditionAsync("Q15228");
```

## Child Entity Discovery

Discover child entities linked to a parent via any relationship property:

```csharp
// TV series seasons (forward traversal)
var seasons = await reconciler.GetChildEntitiesAsync(
    parentQid: "Q1079",                        // Breaking Bad
    relationshipProperty: "P527",               // has parts
    childTypeFilter: ["Q3464665"],              // TV season
    childProperties: ["P1476", "P1545"]);       // title, ordinal

// Books in a series (reverse traversal)
var books = await reconciler.GetChildEntitiesAsync(
    parentQid: "Q8337",                         // Harry Potter series
    relationshipProperty: "^P179",              // reverse: "part of the series"
    childTypeFilter: ["Q7725634"],              // literary work
    childProperties: ["P1476", "P1545", "P577", "P50"]);
```

Results are ordered by P1545 (ordinal) if available, then P577 (date), then label. Use `^` prefix for reverse traversal.

## Pseudonym Detection

Find pen names for authors:

```csharp
var pseudonyms = await reconciler.GetAuthorPseudonymsAsync("Q190192"); // from a book
var pseudonyms = await reconciler.GetAuthorPseudonymsAsync("Q42");     // from an author
foreach (var p in pseudonyms)
    Console.WriteLine($"{p.AuthorLabel}: {string.Join(", ", p.Pseudonyms)}");
```

## Cancellation

All async methods accept a `CancellationToken`:

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
var results = await reconciler.ReconcileAsync("Douglas Adams", cts.Token);
```
