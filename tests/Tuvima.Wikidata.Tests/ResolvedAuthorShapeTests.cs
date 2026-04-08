namespace Tuvima.Wikidata.Tests;

/// <summary>
/// Contract tests for the v2.4.0 ResolvedAuthor DTO additions — verifies RealAuthor
/// shape and that the optional fields default to null when not populated.
/// </summary>
public class ResolvedAuthorShapeTests
{
    [Fact]
    public void ResolvedAuthor_Defaults_AllPseudonymFieldsNull()
    {
        var author = new ResolvedAuthor { OriginalName = "Jane Doe" };

        Assert.Equal("Jane Doe", author.OriginalName);
        Assert.Null(author.Qid);
        Assert.Null(author.CanonicalName);
        Assert.Null(author.RealNameQid);
        Assert.Null(author.RealAuthors);
        Assert.Null(author.Pseudonyms);
        Assert.Equal(0.0, author.Confidence);
    }

    [Fact]
    public void ResolvedAuthor_WithRealAuthors_HoldsList()
    {
        var author = new ResolvedAuthor
        {
            OriginalName = "James S.A. Corey",
            Qid = "Q17452",
            CanonicalName = "James S.A. Corey",
            RealAuthors =
            [
                new RealAuthor { Qid = "Q1163559", CanonicalName = "Daniel Abraham" },
                new RealAuthor { Qid = "Q7876", CanonicalName = "Ty Franck" }
            ]
        };

        Assert.NotNull(author.RealAuthors);
        Assert.Equal(2, author.RealAuthors!.Count);
        Assert.Equal("Daniel Abraham", author.RealAuthors[0].CanonicalName);
        Assert.Equal("Q1163559", author.RealAuthors[0].Qid);
    }

    [Fact]
    public void RealAuthor_RequiresQidAndCanonicalName()
    {
        var a = new RealAuthor { Qid = "Q42", CanonicalName = "Douglas Adams" };

        Assert.Equal("Q42", a.Qid);
        Assert.Equal("Douglas Adams", a.CanonicalName);
    }

    [Fact]
    public void ResolvedAuthor_RealNameQidAndRealAuthors_AreIndependent()
    {
        // Pattern 1 (solo pen name): RealNameQid is set, RealAuthors stays null.
        var pattern1 = new ResolvedAuthor
        {
            OriginalName = "Richard Bachman",
            Qid = "Q39829",
            CanonicalName = "Stephen King",
            RealNameQid = "Q39829"
        };

        Assert.NotNull(pattern1.RealNameQid);
        Assert.Null(pattern1.RealAuthors);

        // Pattern 3 (collective pseudonym): RealAuthors is set, RealNameQid stays null.
        var pattern3 = new ResolvedAuthor
        {
            OriginalName = "James S.A. Corey",
            Qid = "Q17452",
            RealAuthors = [new RealAuthor { Qid = "Q1163559", CanonicalName = "Daniel Abraham" }]
        };

        Assert.Null(pattern3.RealNameQid);
        Assert.NotNull(pattern3.RealAuthors);
    }
}
