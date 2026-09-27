using System.Reflection;
using FluentValidation;

namespace Psycheflow.Api.Features;

/// <summary>Registro dos módulos: handlers e validators por convenção, e mapeamento das rotas em /api/v1.</summary>
public static class FeatureSetup
{
    private const string HandlerSuffix = "Handler";

    public static IServiceCollection AddFeatures(this IServiceCollection services)
    {
        Assembly assembly = typeof(FeatureSetup).Assembly;

        IEnumerable<Type> handlers = assembly.GetTypes().Where(type =>
            type is { IsClass: true, IsAbstract: false }
            && type.Name.EndsWith(HandlerSuffix, StringComparison.Ordinal)
            && type.Namespace?.StartsWith(typeof(FeatureSetup).Namespace!, StringComparison.Ordinal) == true);

        foreach (Type handler in handlers)
        {
            services.AddScoped(handler);
        }

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);
        return services;
    }

    public static IEndpointRouteBuilder MapFeatures(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder api = app.MapGroup("/api/v1");
        return api;
    }
}
