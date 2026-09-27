using System.Globalization;
using System.Reflection;
using FluentValidation;
using Psycheflow.Api.Features.Auth;
using Psycheflow.Api.Features.Companies;
using Psycheflow.Api.Features.Psychologists;
using Psycheflow.Api.Features.Users;

namespace Psycheflow.Api.Features;

/// <summary>Registro dos módulos: handlers e validators por convenção, e mapeamento das rotas em /api/v1.</summary>
public static class FeatureSetup
{
    private const string HandlerSuffix = "Handler";

    public static IServiceCollection AddFeatures(this IServiceCollection services)
    {
        Assembly assembly = typeof(FeatureSetup).Assembly;

        // Convenção: toda classe concreta "*Handler" dentro de Features é um caso de uso (um por slice).
        IEnumerable<Type> handlers = assembly.GetTypes().Where(type =>
            type is { IsClass: true, IsAbstract: false }
            && type.Name.EndsWith(HandlerSuffix, StringComparison.Ordinal)
            && type.Namespace?.StartsWith(typeof(FeatureSetup).Namespace!, StringComparison.Ordinal) == true);

        foreach (Type handler in handlers)
        {
            services.AddScoped(handler);
        }

        // Serviços compartilhados entre slices de um mesmo módulo.
        services.AddScoped<AccessTokenIssuer>();

        ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("pt-BR");
        ValidatorOptions.Global.DefaultRuleLevelCascadeMode = CascadeMode.Stop;
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        return services;
    }

    public static IEndpointRouteBuilder MapFeatures(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder api = app.MapGroup("/api/v1");

        api.MapAuthEndpoints()
            .MapUsersEndpoints()
            .MapSettingsEndpoints()
            .MapPsychologistsEndpoints();

        return api;
    }
}
