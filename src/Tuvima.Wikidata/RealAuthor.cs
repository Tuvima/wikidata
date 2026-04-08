namespace Tuvima.Wikidata;

/// <summary>
/// A real author discovered by expanding a collective pseudonym or resolving a pen name.
/// Populated on <see cref="ResolvedAuthor.RealAuthors"/> when the resolved entity is a
/// shared / collective pseudonym (e.g., James S.A. Corey → Daniel Abraham + Ty Franck)
/// or on <see cref="ResolvedAuthor.RealNameQid"/> when reverse P742 lookup finds a solo
/// author behind a pen name (e.g., Richard Bachman → Stephen King).
/// </summary>
public sealed class RealAuthor
{
    /// <summary>The real author's Wikidata QID.</summary>
    public required string Qid { get; init; }

    /// <summary>The real author's canonical display label in the requested language.</summary>
    public required string CanonicalName { get; init; }
}
