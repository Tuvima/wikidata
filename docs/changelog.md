# Changelog

## v1.0.0

- **Renamed from `Tuvima.WikidataReconciliation` to `Tuvima.Wikidata`** — package name now reflects the library's full scope: reconciliation, entity data, Wikipedia content, and graph traversal. All public types keep their names; only the namespace changes (`using Tuvima.WikidataReconciliation` becomes `using Tuvima.Wikidata`).
- **Graph module** — new `Tuvima.Wikidata.Graph` namespace with `EntityGraph` for in-memory entity graph traversal. Provides pathfinding (BFS), family tree construction, cross-media entity detection, neighbor lookup, and subgraph extraction. Zero dependencies, AOT compatible, thread-safe. Replaces the need for heavy graph libraries like dotNetRDF for common entity relationship operations.
- **Repository moved** to [github.com/Tuvima/wikidata](https://github.com/Tuvima/wikidata).
- **v1.0.0 stability signal** — the reconciliation API has been production-tested through 10 minor versions.

### Migration from v0.x

1. Update package references:
   - `Tuvima.WikidataReconciliation` -> `Tuvima.Wikidata`
   - `Tuvima.WikidataReconciliation.AspNetCore` -> `Tuvima.Wikidata.AspNetCore`
2. Update namespace imports:
   - `using Tuvima.WikidataReconciliation;` -> `using Tuvima.Wikidata;`
   - `using Tuvima.WikidataReconciliation.AspNetCore;` -> `using Tuvima.Wikidata.AspNetCore;`
3. All public types (`WikidataReconciler`, `ReconciliationRequest`, `ReconciliationResult`, etc.) are unchanged.

## v0.10.0

- **Section heading stripping** — `GetWikipediaSectionContentAsync` now automatically strips the section's own heading from the returned content.
- **Subsection content** — new `GetWikipediaSectionWithSubsectionsAsync` fetches a section and all its nested subsections as a structured list of `SectionContent` objects.
- **Multi-value property constraints** — `PropertyConstraint` now supports a `Values` property for matching against entities with multiple values (e.g., multiple authors).
- **Child entity discovery** — new `GetChildEntitiesAsync` traverses parent-child relationships generically. Supports forward and reverse (`^P179`) traversal, optional P31 type filtering, and automatic ordering.

## v0.9.0

- **Public EntityLabel setter** — `WikidataValue.EntityLabel` is now a public setter.

## v0.8.0

- **Automatic entity label resolution in GetPropertiesAsync** — labels are batch-fetched and respect the language parameter with fallback.

## v0.7.0

- **Entity label resolution for GetPropertiesAsync** — new `resolveEntityLabels` parameter.

## v0.6.0

- **Type-filtered search** — CirrusSearch `haswbstatement:P31=QID` at query time. Multi-type OR logic. Per-request `TypeHierarchyDepth` override.
- **Multi-language reconciliation** — concurrent search in multiple languages, deduplicated by QID.
- **Entity label resolution** — `GetEntitiesAsync(qids, resolveEntityLabels: true)`.
- **Work-to-edition pivoting** — `GetEditionsAsync` and `GetWorkForEditionAsync`.
- **Diacritic-aware search** — `DiacriticInsensitive` flag.
- **Display-friendly labels** — `IncludeSitelinkLabels` option.
- **Wikipedia summary language fallback**.
- **Query pre-cleaning** — `Cleaners` pipeline.
- **Pseudonym detection** — `GetAuthorPseudonymsAsync`.
- **Caching infrastructure** — `CachingDelegatingHandler` abstract base class.

## v0.5.0

- **Wikipedia section content** — `GetWikipediaSectionsAsync` and `GetWikipediaSectionContentAsync`.
- **Staleness detection** — `LastRevisionId`, `Modified`, and `GetRevisionIdsAsync`.

## v0.4.0

- **Cross-language label scoring** — scorer compares against labels in all languages.
- **MatchedLabel property** on results.

## v0.3.0

- **External ID lookup**, **value formatting**, **property labels**, **entity images**, **Wikipedia summaries**.
- **W3C Reconciliation API** — ASP.NET Core middleware.
- **Entity change monitoring**, **maxlag support**.

## v0.2.0

- **Data extension**, **qualifiers**, **P279 subclass matching**, **specific property fetching**.
- **Wikipedia URLs**, **batch reconciliation**, **exclude types**, **custom Wikibase support**.

## v0.1.0

- **Core reconciliation** — dual search, fuzzy matching, type filtering, property constraints, property paths, score breakdown, unique ID shortcut, streaming batch, suggest, retry with backoff.
- **Zero dependencies**, **AOT compatible**.
