using Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace Api.IntegrationTests.Infrastructure;

public class IntegrationTestBase : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .Build();

    protected HttpClient Client { get; private set; } = null!;
    private WebApplicationFactory<Program> _factory = null!;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _redis.StartAsync());

        // Infrastructure DependencyInjection reads these env vars first,
        // so set them before the factory starts up.
        Environment.SetEnvironmentVariable("DATABASE_URL", _postgres.GetConnectionString());
        Environment.SetEnvironmentVariable("REDIS_URL", _redis.GetConnectionString());
        Environment.SetEnvironmentVariable("JWT_SECRET", "test-secret-key-at-least-32-characters!!");

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(host =>
            {
                host.UseEnvironment("Test");
                host.UseSetting("Jwt:Issuer", "saas-api");
                host.UseSetting("Jwt:Audience", "saas-client");
                host.UseSetting("App:Url", "http://localhost:3000");
            });

        Client = _factory.CreateClient();

        // Apply EF Core migrations against the test container
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        Environment.SetEnvironmentVariable("DATABASE_URL", null);
        Environment.SetEnvironmentVariable("REDIS_URL", null);
        Environment.SetEnvironmentVariable("JWT_SECRET", null);

        await _factory.DisposeAsync();
        await Task.WhenAll(_postgres.DisposeAsync().AsTask(), _redis.DisposeAsync().AsTask());
    }
}
