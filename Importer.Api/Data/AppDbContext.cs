using Importer.Api.Data.Entities;
using Importer.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Importer.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<DocumentEntity> Documents => Set<DocumentEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>(e =>
        {
            e.ToTable("Users");
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Username).IsUnique();
            e.Property(u => u.Role).HasMaxLength(20);
        });

        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.ToTable("RefreshTokens");
            e.HasKey(t => t.Id);
            e.HasIndex(t => t.TokenHash);
            e.HasOne(t => t.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DocumentEntity>(e =>
        {
            e.ToTable("Documents");
            e.HasKey(d => d.Id);

            e.Property(d => d.Source).HasMaxLength(20);
            e.Property(d => d.DocumentType).HasMaxLength(40);
            e.Property(d => d.Series).HasMaxLength(40);
            e.Property(d => d.DocumentNumber).HasMaxLength(60);
            e.Property(d => d.IssuerTaxId).HasMaxLength(30);
            e.Property(d => d.IssuerName).HasMaxLength(200);
            e.Property(d => d.Atcud).HasMaxLength(120);
            e.Property(d => d.SyncStatus).HasMaxLength(30);
            e.Property(d => d.StorageObjectKey).HasMaxLength(500);
            e.Property(d => d.StorageMetadataKey).HasMaxLength(500);
            e.Property(d => d.OriginalFileName).HasMaxLength(255);
            e.Property(d => d.FileContentType).HasMaxLength(120);

            e.HasIndex(d => d.CreatedAtUtc);
            e.HasIndex(d => d.DocumentDate);
            e.HasIndex(d => d.IssuerTaxId);
            e.HasIndex(d => d.Atcud);
            e.HasIndex(d => d.SyncStatus);
        });
    }
}