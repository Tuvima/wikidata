using System.Text.Json.Serialization;

namespace Tuvima.WikidataReconciliation.Internal.Json;

internal sealed class ParseResponse
{
    [JsonPropertyName("parse")]
    public ParseData? Parse { get; set; }

    [JsonPropertyName("error")]
    public ParseError? Error { get; set; }
}

internal sealed class ParseData
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("tocdata")]
    public TocData? TocData { get; set; }

    [JsonPropertyName("text")]
    public ParseText? Text { get; set; }
}

internal sealed class TocData
{
    [JsonPropertyName("sections")]
    public List<TocSection>? Sections { get; set; }
}

internal sealed class TocSection
{
    [JsonPropertyName("line")]
    public string Line { get; set; } = "";

    [JsonPropertyName("index")]
    public string Index { get; set; } = "";

    [JsonPropertyName("hLevel")]
    public int HLevel { get; set; }

    [JsonPropertyName("number")]
    public string Number { get; set; } = "";

    [JsonPropertyName("anchor")]
    public string Anchor { get; set; } = "";
}

internal sealed class ParseText
{
    [JsonPropertyName("*")]
    public string? Html { get; set; }
}

internal sealed class ParseError
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = "";
}
