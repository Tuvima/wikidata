using Tuvima.Wikidata.Services;
using static Tuvima.Wikidata.Internal.BridgeEntityFacts;

namespace Tuvima.Wikidata.Internal;

/// <summary>Extracts factual series and relationship evidence from fetched entities.</summary>
internal static class BridgeRelationshipExtractor
{
    public static IReadOnlyList<BridgeSeriesInfo> ExtractSeries(
        WikidataEntityInfo entity,
        IReadOnlyDictionary<string, string?> labels,
        IReadOnlyDictionary<string, WikidataEntityInfo> relatedEntities)
    {
        var result = new List<BridgeSeriesInfo>();

        if (entity.Claims.TryGetValue("P179", out var seriesClaims))
        {
            foreach (var claim in seriesClaims)
            {
                var seriesQid = claim.Value?.EntityId;
                if (string.IsNullOrWhiteSpace(seriesQid))
                    continue;

                result.Add(BuildSeriesInfo(
                    entity,
                    labels,
                    relatedEntities,
                    seriesQid,
                    "P179",
                    TryGetQualifierValue(claim, "P1545") ?? GetFirstRawValue(entity, "P1545"),
                    baseConfidence: 1.0));
            }
        }

        var partOf = GetEntityIds(entity, "P361");
        foreach (var qid in partOf)
        {
            result.Add(BuildSeriesInfo(
                entity,
                labels,
                relatedEntities,
                qid,
                "P361",
                GetFirstRawValue(entity, "P1545"),
                baseConfidence: 0.75));
        }

        return result;
    }

    private static BridgeSeriesInfo BuildSeriesInfo(
        WikidataEntityInfo sourceEntity,
        IReadOnlyDictionary<string, string?> labels,
        IReadOnlyDictionary<string, WikidataEntityInfo> relatedEntities,
        string seriesQid,
        string sourcePropertyId,
        string? position,
        double baseConfidence)
    {
        relatedEntities.TryGetValue(seriesQid, out var container);
        var label = labels.TryGetValue(seriesQid, out var fetchedLabel) ? fetchedLabel : container?.Label;
        var kind = container is not null
            ? WikidataContainerClassifier.Classify(container)
            : WikidataContainerClassifier.Classify(label, null, null);
        var hasOrderingEvidence = !string.IsNullOrWhiteSpace(position)
            || !string.IsNullOrWhiteSpace(GetFirstEntityId(sourceEntity, "P155"))
            || !string.IsNullOrWhiteSpace(GetFirstEntityId(sourceEntity, "P156"));
        var isImmediateSeries = WikidataContainerClassifier.IsImmediateSeriesKind(kind)
            || (sourcePropertyId == "P179"
                && kind == WikidataContainerKind.Unknown
                && hasOrderingEvidence);

        return new BridgeSeriesInfo
        {
            SeriesQid = seriesQid,
            SeriesLabel = label,
            ContainerKind = kind,
            IsImmediateSeries = isImmediateSeries,
            Position = position,
            PreviousQid = GetFirstEntityId(sourceEntity, "P155"),
            NextQid = GetFirstEntityId(sourceEntity, "P156"),
            SourcePropertyId = sourcePropertyId,
            Confidence = isImmediateSeries ? baseConfidence : Math.Min(baseConfidence, 0.35)
        };
    }

    public static IReadOnlyList<BridgeRelationshipEdge> ExtractRelationships(
        WikidataEntityInfo entity,
        IReadOnlyDictionary<string, string?> labels)
    {
        var result = new List<BridgeRelationshipEdge>();
        AddEdges(entity, labels, result, "P179", "series");
        AddEdges(entity, labels, result, "P1080", "universe");
        AddEdges(entity, labels, result, "P361", "parent-work");
        AddEdges(entity, labels, result, "P527", "has-part");
        AddEdges(entity, labels, result, "P155", "previous");
        AddEdges(entity, labels, result, "P156", "next");
        return result;
    }

    private static void AddEdges(
        WikidataEntityInfo entity,
        IReadOnlyDictionary<string, string?> labels,
        List<BridgeRelationshipEdge> result,
        string propertyId,
        string kind)
    {
        foreach (var objectQid in GetEntityIds(entity, propertyId))
        {
            result.Add(new BridgeRelationshipEdge
            {
                SubjectQid = entity.Id,
                PropertyId = propertyId,
                ObjectQid = objectQid,
                ObjectLabel = labels.TryGetValue(objectQid, out var label) ? label : null,
                RelationshipKind = kind,
                Confidence = 1.0
            });
        }
    }

    public static IReadOnlyList<string> GetRelationshipQids(WikidataEntityInfo entity)
    {
        return new[] { "P179", "P1080", "P361", "P527", "P8345", "P155", "P156", "P629", "P747" }
            .SelectMany(propertyId => GetEntityIds(entity, propertyId))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
