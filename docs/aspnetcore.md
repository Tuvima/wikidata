# ASP.NET Core Integration

Host a W3C Reconciliation Service API compatible with OpenRefine and Google Sheets.

## Installation

```
dotnet add package Tuvima.Wikidata.AspNetCore
```

## DI Registration

```csharp
services.AddWikidataReconciliation(options =>
{
    options.Language = "en";
    options.UserAgent = "MyApp/1.0 (contact@example.com)";
});
```

## Endpoint Mapping

```csharp
app.MapReconciliation("/api/reconcile", options =>
{
    options.ServiceName = "My Wikidata Service";
    options.DefaultTypes =
    [
        new("Q5", "Human"),
        new("Q515", "City"),
        new("Q7725634", "Literary work")
    ];
});
```

## Endpoints

| Endpoint | Purpose |
|---|---|
| `GET /api/reconcile` | Service manifest (name, capabilities, default types) |
| `POST /api/reconcile` | Reconciliation queries (single or batch) |
| `GET /api/reconcile/suggest/entity?prefix=...` | Entity autocomplete |
| `GET /api/reconcile/suggest/property?prefix=...` | Property autocomplete |
| `GET /api/reconcile/suggest/type?prefix=...` | Type/class autocomplete |
| `GET /api/reconcile/preview?id=Q42` | HTML preview card (thumbnail, description, link) |

All endpoints respect the `Accept-Language` header — a French browser automatically gets French labels without extra configuration.

## Manual Registration (No Companion Package)

Register manually with zero extra dependencies:

```csharp
services.AddHttpClient("Wikidata", c =>
    c.DefaultRequestHeaders.UserAgent.ParseAdd("MyApp/1.0 (contact@example.com)"));

services.AddSingleton(sp => new WikidataReconciler(
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("Wikidata"),
    new WikidataReconcilerOptions { Language = "en" }));
```
