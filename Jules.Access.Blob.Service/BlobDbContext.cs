using Jules.Access.Blob.Service.Models;
using Jules.Util.Shared;
using Microsoft.EntityFrameworkCore;

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