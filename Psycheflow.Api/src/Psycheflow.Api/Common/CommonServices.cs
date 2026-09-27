using System.Diagnostics;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Common.Errors;
using Psycheflow.Api.Common.OpenApi;
using Psycheflow.Api.Common.Persistence;
using Psycheflow.Api.Common.Storage;
using Psycheflow.Api.Common.Time;
using Psycheflow.Api.Features.Users;

namespace Psycheflow.Api.Common;

public static class CommonServices
{
    public const string CorsSection = "Cors:AllowedOrigins";

    public static IServiceCollection AddCommon(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ClinicClock>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();
        services.AddFileStorage();

        services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
        {
            context.ProblemDetails.Instance ??= $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";
            context.ProblemDetails.Extensions.TryAdd("traceId", Activity.Current?.Id ?? context.HttpContext.TraceIdentifier);
        });
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.AddIdentityAndJwt();
        services.AddPsycheflowAuthorization();
        services.AddPsycheflowOpenApi();

        string[] allowedOrigins = configuration.GetSection(CorsSection).Get<string[]>() ?? [];
        services.AddCors(options => options.AddDefaultPolicy(policy => policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()));

        services.AddHealthChecks().AddDbContextCheck<AppDbContext>("database");

        return services;
    }

    private static void AddIdentityAndJwt(this IServiceCollection services)
    {
        services.AddIdentityCore<User>(options =>
            {
                options.User.RequireUniqueEmail = true;

                // RN-11: mínimo 6 caracteres com maiúscula, minúscula, número e símbolo.
                options.Password.RequiredLength = 6;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;

                // RN-12: bloqueio temporário após tentativas erradas.
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddErrorDescriber<PortugueseIdentityErrorDescriber>();

        services.AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<TokenService>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>, TimeProvider>((bearer, jwtOptions, timeProvider) =>
            {
                JwtOptions jwt = jwtOptions.Value;
                TimeSpan clockSkew = TimeSpan.FromSeconds(30);

                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = TokenService.CreateSigningKey(jwt.Key),
                    NameClaimType = AuthClaims.Name,
                    RoleClaimType = AuthClaims.Role,
                    ClockSkew = clockSkew,

                    // Mesmo relógio usado para emitir o token (TimeProvider), o que mantém a validade testável.
                    LifetimeValidator = (notBefore, expires, _, _) =>
                    {
                        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
                        return (notBefore is null || notBefore.Value <= now + clockSkew)
                            && (expires is null || expires.Value > now - clockSkew);
                    },
                };
            });
    }
}
