using LangKeep.Application.Services;
using LangKeep.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LangKeep.Application;

/// <summary>
/// Registers Application-layer services with the dependency injection container.
/// </summary>
public static class ApplicationModule
{
    /// <summary>
    /// Adds Application-layer services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register the rule matcher (singleton so in-memory rules persist)
        services.TryAddSingleton<RuleEvaluationService>();
        services.TryAddSingleton<IRuleMatcher>(sp => sp.GetRequiredService<RuleEvaluationService>());

        // Register application services
        services.TryAddSingleton<PreferenceManagementService>();
        services.TryAddSingleton<LanguageTrackingService>();

        return services;
    }
}
