using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Psycheflow.Api.Common.Auth;

namespace Psycheflow.Api.Common.RateLimiting;

/// <summary>Políticas de limite de requisições aplicadas por endpoint com <c>RequireRateLimiting</c>.</summary>
public static class RateLimitPolicies
{
    /// <summary>Login e registro, por IP: dificulta força bruta e criação de contas em massa (complementa o lockout por conta).</summary>
    public const string Auth = "auth";

    /// <summary>Sugestões de IA, por usuário: cada chamada consome créditos do provedor.</summary>
    public const string Ai = "ai";
}

/// <summary>Seção "RateLimiting" da configuração (janela fixa por política).</summary>
public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public FixedWindowSettings Auth { get; set; } = new() { PermitLimit = 10, WindowSeconds = 60 };

    public FixedWindowSettings Ai { get; set; } = new() { PermitLimit = 20, WindowSeconds = 60 };
}

public sealed class FixedWindowSettings
{
    public int PermitLimit { get; set; }

    public int WindowSeconds { get; set; }
}

/// <summary>DT-23: limite de requisições (middleware nativo do ASP.NET Core) com resposta 429 em ProblemDetails.</summary>
public static class RateLimitingSetup
{
    public const string ExceededCode = "rate_limit.exceeded";

    public static IServiceCollection AddPsycheflowRateLimiting(this IServiceCollection services)
    {
        services.AddOptions<RateLimitingOptions>().BindConfiguration(RateLimitingOptions.SectionName);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = WriteProblemAsync;

            options.AddPolicy(RateLimitPolicies.Auth, context =>
                FixedWindow(context, $"ip:{context.Connection.RemoteIpAddress}", settings => settings.Auth));

            options.AddPolicy(RateLimitPolicies.Ai, context =>
                FixedWindow(
                    context,
                    context.User.FindFirstValue(AuthClaims.UserId) is { } userId ? $"user:{userId}" : $"ip:{context.Connection.RemoteIpAddress}",
                    settings => settings.Ai));
        });

        return services;
    }

    private static RateLimitPartition<string> FixedWindow(
        HttpContext context, string partitionKey, Func<RateLimitingOptions, FixedWindowSettings> select)
    {
        FixedWindowSettings settings = select(context.RequestServices.GetRequiredService<IOptions<RateLimitingOptions>>().Value);

        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = settings.PermitLimit,
            Window = TimeSpan.FromSeconds(settings.WindowSeconds),
            QueueLimit = 0,
        });
    }

    private static async ValueTask WriteProblemAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        HttpContext http = context.HttpContext;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
        {
            http.Response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        }

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Muitas requisições.",
            Detail = "Muitas tentativas em pouco tempo. Aguarde um instante e tente novamente.",
        };
        problem.Extensions["code"] = ExceededCode;

        await http.RequestServices.GetRequiredService<IProblemDetailsService>()
            .WriteAsync(new ProblemDetailsContext { HttpContext = http, ProblemDetails = problem });
    }
}
