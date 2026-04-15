using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.UnitTests.TestInfrastructure;

/// <summary>
/// Shared in-memory DbContext for unit tests.
/// Use a unique database name per test to ensure isolation.
/// </summary>
public class TestDbContext : AppDbContext
{
    public TestDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public static TestDbContext Create(string dbName)
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new TestDbContext(opts);
    }
}
