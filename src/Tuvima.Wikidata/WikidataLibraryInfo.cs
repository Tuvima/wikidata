using System.Reflection;

namespace Tuvima.Wikidata;

/// <summary>
/// Runtime/package version information for the Tuvima.Wikidata library.
/// </summary>
public static class WikidataLibraryInfo
{
    public static string PackageVersion { get; } =
        typeof(WikidataLibraryInfo).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion
        ?? typeof(WikidataLibraryInfo).Assembly.GetName().Version?.ToString()
        ?? "unknown";

    public static Version? AssemblyVersion { get; } =
        typeof(WikidataLibraryInfo).Assembly.GetName().Version;
}
