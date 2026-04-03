# Architecture

## Component Overview

```
WikidataReconciler (public entry point)
├── WikidataSearchClient        <- dual search: wbsearchentities + full-text
├── WikidataEntityFetcher       <- wbgetentities in batches of 50, rank-aware
├── ReconciliationScorer        <- weighted label + property scoring
├── TypeChecker                 <- P31 matching + optional P279 subclass walking
│   └── SubclassResolver        <- BFS P279 walker with in-memory cache
├── ResilientHttpClient         <- retry-on-429, exponential backoff, maxlag
├── EntityMapper                <- internal JSON DTO -> public model mapping
├── FuzzyMatcher                <- token-sort-ratio (Levenshtein-based)
├── PropertyMatcher             <- type-specific matching (items, dates, quantities, coords, URLs)
├── PropertyPath                <- chained paths like "P131/P17"
└── LanguageFallback            <- "de-ch" -> "de" -> "mul" -> "en"

EntityGraph (graph module)
├── Adjacency lists             <- outgoing + incoming edge dictionaries
├── BFS pathfinding             <- FindPaths
├── BFS family tree             <- GetFamilyTree
├── LINQ cross-media            <- FindCrossMediaEntities
└── BFS subgraph extraction     <- GetSubgraph
```

## Reconciliation Pipeline (4 Stages)

### 1. Dual Search

Two MediaWiki API searches run concurrently:

- **`wbsearchentities`** (autocomplete): Matches labels and aliases directly. Fast and precise for well-known names.
- **`action=query&list=search`** (full-text): Searches across all entity content. Finds items like "1984" where the label ("Nineteen Eighty-Four") differs from the query.

Results are merged (full-text first, then autocomplete) and deduplicated. When types are specified, a CirrusSearch `haswbstatement:P31=QID` query also runs for better type recall. Multi-language search runs all languages concurrently. Queries are truncated at 250 characters to avoid silent failures from the MediaWiki API.

### 2. Entity Fetching

Candidate entities are fetched via `wbgetentities` in batches of up to 50, retrieving labels, descriptions, aliases, and claims in the requested language with fallback. The library respects the Wikidata statement rank hierarchy:

- **Preferred** rank values are used if available
- **Normal** rank values are used otherwise
- **Deprecated** rank values are always excluded

### 3. Scoring

Each candidate receives a weighted score from 0 to 100:

```
label_score  = max(token_sort_ratio(query, label) for each label and alias)
prop_score_i = max(type_specific_match(query_value, claim_value) for each claim)

score = (label_score * 1.0 + sum(prop_score_i * 0.4)) / (1.0 + 0.4 * num_properties)
```

If a type constraint was specified and the entity has no type claims, the score is halved. The auto-match flag is set on the top result when the score exceeds the threshold and the gap over the second-best candidate is sufficient.

For multi-value constraints, the property score is the average of the best match for each constraint value (e.g., 2 of 2 authors match = full score, 1 of 2 = half).

### 4. Type Filtering

Candidates are checked against the requested type (P31 direct match) and excluded types. With `TypeHierarchyDepth > 0`, the library walks the P279 (subclass of) hierarchy — for example, a "novel" (Q8261) matches a query for "literary work" (Q7725634) because novel is a subclass of literary work. The subclass hierarchy is cached in memory within the reconciler's lifetime.

## Design Decisions

- **Zero external dependencies** — only `System.Text.Json` (built into .NET). No FuzzySharp, no Polly, no caching libraries.
- **AOT compatible** — `IsAotCompatible` and `IsTrimmable` set in .csproj. All JSON serialization uses source-generated `JsonSerializerContext` (no reflection).
- **No built-in cache** — deliberate; avoids stale data issues. Users add caching via `HttpClient` `DelegatingHandler` pattern.
- **Dual search** — both `wbsearchentities` and full-text search run concurrently. Critical for recall (e.g., "1984" finds the novel whose label is "Nineteen Eighty-Four").
- **Claim rank hierarchy** — preferred rank values used if available, then normal, deprecated always excluded.
- **Language fallback chain** — exact -> subtag parent -> "mul" -> "en". API requests include all fallback languages.
- **Concurrency limiting** — `SemaphoreSlim` gates parallel API requests (default 5) to avoid Wikimedia rate limits.
- **maxlag parameter** — appended to every Wikidata API request per Wikimedia bot etiquette.
- **Graph module: no RDF** — the graph module uses adjacency lists and BFS, not RDF/SPARQL. The operations (pathfinding, family trees, cross-media detection) don't require a full graph database.

## Wikidata API Endpoints Used

| API | Purpose |
|---|---|
| `wbsearchentities` | Autocomplete search by label/alias |
| `action=query&list=search` | Full-text search across entity content |
| `wbgetentities` | Fetch entity data (labels, descriptions, aliases, claims, sitelinks) |
| Wikipedia REST API `/page/summary/` | Article summaries with thumbnails |
| Wikipedia `action=parse` | Section TOC (tocdata) and section content (text) |
| `action=query&prop=revisions` | Lightweight revision ID lookup for staleness detection |
| `action=query&list=recentchanges` | Entity change monitoring |
| CirrusSearch `haswbstatement:` | External ID reverse lookup + type-filtered search |
