using Benzene.Examples.Aws.Data.Model;
using Microsoft.EntityFrameworkCore;

namespace Benzene.Examples.Aws.Data;

public class DataContext : DbContext
{
    public DataContext()
    {

    }

    public DataContext(DbContextOptions<DataContext> options)
        :base(options)
    {
    }

    public DbSet<Order> Orders { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>();

        base.OnModelCreating(modelBuilder);
    }
}