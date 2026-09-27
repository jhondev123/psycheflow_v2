using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Psycheflow.Api.Common.OpenApi;

public static class OpenApiSetup
{
    public const string BearerScheme = "Bearer";

    public static IServiceCollection AddPsycheflowOpenApi(this IServiceCollection services) =>
        services.AddOpenApi(options => options.AddDocumentTransformer<PsycheflowDocumentTransformer>());

    /// <summary>Metadados do documento e esquema de autenticação JWT (botão "Authorize" no Scalar).</summary>
    private sealed class PsycheflowDocumentTransformer : IOpenApiDocumentTransformer
    {
        public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
        {
            document.Info = new OpenApiInfo
            {
                Title = "Psycheflow API",
                Version = "v1",
                Description = "ERP para clínicas de psicologia e psicólogos autônomos (TCC – FAG).",
            };

            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            document.Components.SecuritySchemes[BearerScheme] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Token obtido em POST /api/v1/auth/login.",
            };

            document.Security ??= [];
            document.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(BearerScheme, document)] = [],
            });

            return Task.CompletedTask;
        }
    }
}
