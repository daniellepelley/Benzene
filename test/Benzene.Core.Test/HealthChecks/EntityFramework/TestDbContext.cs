using Microsoft.EntityFrameworkCore;

namespace Benzene.Test.HealthChecks.EntityFramework;

public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
    {
    }

    public DbSet<TestEntity> Entities => Set<TestEntity>();
}

public class TestEntity
{
    public int Id { get; set; }
}
