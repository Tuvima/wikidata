using Tuvima.Wikidata.Services;

namespace Tuvima.Wikidata.Tests;

public class AuthorsSplitterTests
{
    [Fact]
    public void SplitAuthors_SingleName_ReturnsOne()
    {
        var (names, unresolved) = AuthorsService.SplitAuthors("Neil Gaiman");
        Assert.Single(names);
        Assert.Equal("Neil Gaiman", names[0]);
        Assert.Empty(unresolved);
    }

    [Fact]
    public void SplitAuthors_Ampersand_SplitsIntoTwo()
    {
        var (names, _) = AuthorsService.SplitAuthors("Neil Gaiman & Terry Pratchett");
        Assert.Equal(2, names.Count);
        Assert.Equal("Neil Gaiman", names[0]);
        Assert.Equal("Terry Pratchett", names[1]);
    }

    [Fact]
    public void SplitAuthors_And_SplitsIntoTwo()
    {
        var (names, _) = AuthorsService.SplitAuthors("Neil Gaiman and Terry Pratchett");
        Assert.Equal(2, names.Count);
        Assert.Equal("Neil Gaiman", names[0]);
        Assert.Equal("Terry Pratchett", names[1]);
    }

    [Fact]
    public void SplitAuthors_LastFirstForm_NotSplit()
    {
        // "Tolkien, J. R. R." is a single author in Last, First form — must not split on the comma.
        var (names, _) = AuthorsService.SplitAuthors("Tolkien, J. R. R.");
        Assert.Single(names);
        // After normalization, the internal form is "First Last".
        Assert.Equal("J. R. R. Tolkien", names[0]);
    }

    [Fact]
    public void SplitAuthors_Semicolon_SplitsMultiple()
    {
        var (names, _) = AuthorsService.SplitAuthors("Alice; Bob; Carol");
        Assert.Equal(3, names.Count);
    }

    [Fact]
    public void SplitAuthors_With_SplitsOnWord()
    {
        var (names, _) = AuthorsService.SplitAuthors("Stephen King with Peter Straub");
        Assert.Equal(2, names.Count);
        Assert.Equal("Stephen King", names[0]);
        Assert.Equal("Peter Straub", names[1]);
    }

    [Fact]
    public void SplitAuthors_EtAl_Captured()
    {
        var (names, unresolved) = AuthorsService.SplitAuthors("Jane Smith et al.");
        Assert.Single(names);
        Assert.Equal("Jane Smith", names[0]);
        Assert.Contains("et al.", unresolved);
    }

    [Fact]
    public void SplitAuthors_CjkComma_Splits()
    {
        var (names, _) = AuthorsService.SplitAuthors("太郎、花子");
        Assert.Equal(2, names.Count);
    }

    [Fact]
    public void SplitAuthors_CommaSeparatedFlatList_Splits()
    {
        // Three names with commas, not Last-First form: should split.
        var (names, _) = AuthorsService.SplitAuthors("Alice, Bob, Carol");
        Assert.Equal(3, names.Count);
    }

    [Fact]
    public void SplitAuthors_EmptyInput_ReturnsEmpty()
    {
        var (names, _) = AuthorsService.SplitAuthors("   ");
        Assert.Empty(names);
    }

    [Fact]
    public void SplitAuthors_MixedSeparators_SplitsAll()
    {
        var (names, _) = AuthorsService.SplitAuthors("Alice & Bob and Carol");
        Assert.Equal(3, names.Count);
    }
}
