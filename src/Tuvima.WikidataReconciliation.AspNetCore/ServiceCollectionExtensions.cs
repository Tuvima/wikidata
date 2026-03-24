using Microsoft.Extensions.DependencyInjection;

namespace Tuvima.WikidataReconciliation.AspNetCore;

/// <summary>
/// Extension methods for registering WikidataReconciler with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="WikidataReconciler"/> as a singleton with a named HttpClient.
    /// </summary>
    public static IServiceCollection AddWikidataReconciliation(
        this IServiceCollection services,
        Action<WikidataReconcilerOptions>? configure = null)
    {
        var options = new WikidataReconcilerOptions();
        configure?.Invoke(options);

        services.AddHttpClient("WikidataReconciliation", client =>
        {
            client.Timeout = options.Timeout;
            client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
        });

        services.AddSingleton(sp =>
        {
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("WikidataReconciliation");
            return new WikidataReconciler(httpClient, options);
        });

        return services;
    }
}
