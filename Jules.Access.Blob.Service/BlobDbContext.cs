using Jules.Access.Blob.Service.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Jules.Access.Blob.Service;

public class BlobDbContext : DbContext
{
    public BlobDbContext(DbContextOptions<BlobDbContext> options) : base(options)
    {
    }

    public DbSet<BlobDb> Blobs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Automatically set CreatedOn property to the current date and time on insert
        modelBuilder.Entity<BlobDb>()
            .Property(i => i.CreatedOn)
            .ValueGeneratedOnAdd()
            .HasValueGenerator<CustomCreatedOnValueGenerator>();  // Use custom value generator
    }
}

public class CustomCreatedOnValueGenerator : ValueGenerator<DateTimeOffset>
{
    public override DateTimeOffset Next(EntityEntry entry)
    {
        return DateTimeOffset.UtcNow;  // Or use custom logic to generate a timestamp
    }

    public override bool GeneratesTemporaryValues => false;  // We don't want to generate temporary values
}