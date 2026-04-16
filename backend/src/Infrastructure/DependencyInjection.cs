using Application.Common.Interfaces;
using Application.Common.Settings;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration config)
    {
        // ── PostgreSQL / EF Core ───────────────────────────────────────────────
        var connectionString =
            Environment.GetEnvironmentVariable("DATABASE_URL")
            ?? config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Database connection string not configured");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IAppDbContext>(sp =>
            sp.GetRequiredService<AppDbContext>());

        // ── Token store: Redis if configured, otherwise in-memory ────────────
        var redisUrl =
            Environment.GetEnvironmentVariable("REDIS_URL")
            ?? config.GetConnectionString("Redis");

        if (!string.IsNullOrWhiteSpace(redisUrl))
        {
            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(redisUrl));
            services.AddScoped<ITokenStore, RedisTokenStore>();
        }
        else
        {
            // No Redis configured — use in-memory store (dev only)
            services.AddSingleton<ITokenStore, InMemoryTokenStore>();
        }

        // ── JWT settings ──────────────────────────────────────────────────────
        var jwtSecret =
            Environment.GetEnvironmentVariable("JWT_SECRET")
            ?? config["Jwt:Secret"]
            ?? throw new InvalidOperationException("JWT_SECRET not configured");

        // Bind remaining settings from config, override secret from env
        services.Configure<JwtSettings>(opts =>
        {
            config.GetSection(JwtSettings.SectionName).Bind(opts);
            opts.Secret = jwtSecret;
        });

        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();

        // ── Email (Resend) ────────────────────────────────────────────────────
        services.AddHttpClient<IEmailService, EmailService>();

        // ── RBAC ──────────────────────────────────────────────────────────────
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IAuditService, AuditService>();

        // ── Billing ───────────────────────────────────────────────────────────
        var stripeKey =
            Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY")
            ?? config["Stripe:SecretKey"];

        if (!string.IsNullOrWhiteSpace(stripeKey))
        {
            // Real Stripe billing — keys are present
            services.Configure<StripeSettings>(opts =>
            {
                config.GetSection(StripeSettings.SectionName).Bind(opts);
                opts.SecretKey = stripeKey;

                var webhookSecret = Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET");
                if (!string.IsNullOrWhiteSpace(webhookSecret))
                    opts.WebhookSecret = webhookSecret;
            });

            services.AddSingleton<IStripeGateway, StripeGatewayAdapter>();
            services.AddScoped<IBillingService, StripeBillingService>();
        }
        else
        {
            // No payment keys — use mock (dev / regions without Stripe access)
            services.AddScoped<IBillingService, MockBillingService>();
        }

        services.AddScoped<IFeatureGate, FeatureGateService>();

        return services;
    }
}
