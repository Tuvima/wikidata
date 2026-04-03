using System.Net;
using System.Text.RegularExpressions;

namespace Tuvima.Wikidata.Internal;

/// <summary>
/// Lightweight HTML-to-plain-text converter for Wikipedia's action=parse output.
/// Not a general-purpose HTML parser — relies on the well-structured HTML that
/// MediaWiki produces.
/// </summary>
internal static partial class HtmlTextExtractor
{
    /// <summary>
    /// Converts Wikipedia section HTML to plain text.
    /// </summary>
    public static string ExtractText(string html)
    {
        var text = html;

        // Remove elements that produce noise in plain text
        text = SupRegex().Replace(text, "");           // Footnote markers [1], [2]
        text = StyleRegex().Replace(text, "");         // Embedded stylesheets
        text = TableRegex().Replace(text, "");         // Tables (infoboxes, data tables)
        text = CommentRegex().Replace(text, "");       // HTML comments

        // Convert block elements to newlines for paragraph separation
        text = BlockBreakRegex().Replace(text, "\n");

        // Strip remaining HTML tags
        text = TagRegex().Replace(text, "");

        // Decode HTML entities
        text = WebUtility.HtmlDecode(text);

        // Remove [edit] markers left from section headers
        text = EditMarkerRegex().Replace(text, "");

        // Normalize whitespace: collapse runs of spaces/tabs on a line
        text = HorizontalWhitespaceRegex().Replace(text, " ");

        // Collapse multiple blank lines
        text = BlankLineRegex().Replace(text, "\n\n");

        return text.Trim();
    }

    /// <summary>
    /// Strips inline HTML from section titles (e.g., "<i>Monty Python</i>" → "Monty Python").
    /// </summary>
    public static string StripInlineHtml(string html)
    {
        var text = TagRegex().Replace(html, "");
        return WebUtility.HtmlDecode(text).Trim();
    }

    /// <summary>
    /// Strips a leading section heading from plain text content.
    /// Handles MediaWiki markup (== Title ==), plain-text headings matching a known title, and HTML headings.
    /// </summary>
    public static string StripLeadingHeading(string text)
    {
        // Strip MediaWiki-style heading: == Title ==, === Title ===, etc.
        var stripped = LeadingWikiHeadingRegex().Replace(text, "");
        if (stripped.Length < text.Length)
            return stripped.TrimStart('\r', '\n');

        // Strip HTML heading tags: <h2>Title</h2>, <h3>Title</h3>, etc.
        stripped = LeadingHtmlHeadingRegex().Replace(text, "");
        if (stripped.Length < text.Length)
            return stripped.TrimStart('\r', '\n');

        // Strip a plain-text first line followed by a blank line (likely a heading)
        stripped = LeadingPlainHeadingRegex().Replace(text, "");
        if (stripped.Length < text.Length)
            return stripped.TrimStart('\r', '\n');

        return text;
    }

    [GeneratedRegex(@"^\s*=+\s*[^=\n]+\s*=+\s*\n*", RegexOptions.Compiled)]
    private static partial Regex LeadingWikiHeadingRegex();

    [GeneratedRegex(@"^\s*<h[1-6][^>]*>.*?</h[1-6]>\s*\n*", RegexOptions.Singleline | RegexOptions.Compiled)]
    private static partial Regex LeadingHtmlHeadingRegex();

    [GeneratedRegex(@"^[^\n]{1,200}\n\n", RegexOptions.Compiled)]
    private static partial Regex LeadingPlainHeadingRegex();

    [GeneratedRegex(@"<sup[^>]*>.*?</sup>", RegexOptions.Singleline | RegexOptions.Compiled)]
    private static partial Regex SupRegex();

    [GeneratedRegex(@"<style[^>]*>.*?</style>", RegexOptions.Singleline | RegexOptions.Compiled)]
    private static partial Regex StyleRegex();

    [GeneratedRegex(@"<table[^>]*>.*?</table>", RegexOptions.Singleline | RegexOptions.Compiled)]
    private static partial Regex TableRegex();

    [GeneratedRegex(@"<!--.*?-->", RegexOptions.Singleline | RegexOptions.Compiled)]
    private static partial Regex CommentRegex();

    [GeneratedRegex(@"</?(p|div|br|h[1-6]|li|ul|ol|blockquote|dd|dt|dl)[^>]*>", RegexOptions.Compiled)]
    private static partial Regex BlockBreakRegex();

    [GeneratedRegex(@"<[^>]+>", RegexOptions.Compiled)]
    private static partial Regex TagRegex();

    [GeneratedRegex(@"\[edit\]", RegexOptions.Compiled)]
    private static partial Regex EditMarkerRegex();

    [GeneratedRegex(@"[^\S\n]+", RegexOptions.Compiled)]
    private static partial Regex HorizontalWhitespaceRegex();

    [GeneratedRegex(@"\n{3,}", RegexOptions.Compiled)]
    private static partial Regex BlankLineRegex();
}
