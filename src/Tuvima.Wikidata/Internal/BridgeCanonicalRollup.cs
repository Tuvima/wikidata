using static Tuvima.Wikidata.Internal.BridgeEntityFacts;

namespace Tuvima.Wikidata.Internal;

/// <summary>Selects edition/work identity and preserves the supporting relationship path.</summary>
internal static class BridgeCanonicalRollup
{
    public static CanonicalRollup BuildRollup(BridgeResolutionRequest request, WikidataEntityInfo entity)
    {
        var resolvedEntityQid = entity.Id;
        var canonicalWorkQid = entity.Id;
        var path = new List<BridgeRelationshipPathStep>();

        var parentWorks = GetEntityIds(entity, "P629");
        if (parentWorks.Count > 0 && request.RollupTarget != BridgeRollupTarget.ResolvedEntity)
        {
            canonicalWorkQid = parentWorks[0];
            if (request.RollupTarget == BridgeRollupTarget.PreferCanonicalWork)
                resolvedEntityQid = canonicalWorkQid;

            path.Add(new BridgeRelationshipPathStep
            {
                SubjectQid = entity.Id,
                PropertyId = "P629",
                ObjectQid = canonicalWorkQid,
                Direction = Direction.Outgoing
            });
        }
        else if (request.RollupTarget == BridgeRollupTarget.PreferEdition)
        {
            var editions = GetEntityIds(entity, "P747");
            if (editions.Count > 0)
            {
                canonicalWorkQid = entity.Id;
                resolvedEntityQid = editions.OrderBy(QidNumber).First();
                path.Add(new BridgeRelationshipPathStep
                {
                    SubjectQid = entity.Id,
                    PropertyId = "P747",
                    ObjectQid = resolvedEntityQid,
                    Direction = Direction.Outgoing
                });
            }
        }

        return new CanonicalRollup
        {
            ResolvedEntityQid = resolvedEntityQid,
            CanonicalWorkQid = canonicalWorkQid,
            IsRollup = path.Count > 0,
            RelationshipPath = path
        };
    }
}
