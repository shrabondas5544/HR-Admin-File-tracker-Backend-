using CabinetMap.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CabinetMap.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Cabinet> Cabinets => Set<Cabinet>();
    public DbSet<Shelf> Shelves => Set<Shelf>();
    public DbSet<Magazine> Magazines => Set<Magazine>();
    public DbSet<Folder> Folders => Set<Folder>();
    public DbSet<RecordFile> RecordFiles => Set<RecordFile>();
    public DbSet<DocumentType> DocumentTypes => Set<DocumentType>();
    public DbSet<User> Users => Set<User>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Cascade delete configurations
        modelBuilder.Entity<Cabinet>()
            .HasMany(c => c.Shelves)
            .WithOne(s => s.Cabinet)
            .HasForeignKey(s => s.CabinetId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Shelf>()
            .HasMany(s => s.Magazines)
            .WithOne(m => m.Shelf)
            .HasForeignKey(m => m.ShelfId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Shelf>()
            .HasMany(s => s.Folders)
            .WithOne(f => f.Shelf)
            .HasForeignKey(f => f.ShelfId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Shelf>()
            .HasMany(s => s.StandaloneFiles)
            .WithOne(f => f.Shelf)
            .HasForeignKey(f => f.ShelfId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Magazine>()
            .HasMany(m => m.Files)
            .WithOne(f => f.Magazine)
            .HasForeignKey(f => f.MagazineId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
