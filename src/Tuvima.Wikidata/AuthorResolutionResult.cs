namespace Tuvima.Wikidata;

/// <summary>
/// The outcome of an <see cref="AuthorResolutionRequest"/>.
/// </summary>
public sealed class AuthorResolutionResult
{
    /// <summary>
    /// Per-name resolution results, in the order the names appeared in the input string.
    /// Entries where reconciliation failed have a null <see cref="ResolvedAuthor.Qid"/>.
    /// </summary>
    public IReadOnlyList<ResolvedAuthor> Authors { get; init; } = [];

    /// <summary>
    /// Names that were extracted from the input but could not be resolved to a QID.
    /// Includes any "et al." marker the input contained.
    /// </summary>
    public IReadOnlyList<string> UnresolvedNames { get; init; } = [];
}
