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
    public DbSet<CompanyEntity> Companies => Set<CompanyEntity>();
    public DbSet<AuditLogEntity> AuditLogs => Set<AuditLogEntity>();

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
            e.Property(d => d.IssuerAddress).HasMaxLength(300);
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

        modelBuilder.Entity<CompanyEntity>(e =>
        {
            e.ToTable("Companies");
            e.HasKey(c => c.Id);

            e.Property(c => c.TaxId).HasMaxLength(30);
            e.Property(c => c.Name).HasMaxLength(200);
            e.Property(c => c.Address).HasMaxLength(300);
            e.Property(c => c.Source).HasMaxLength(120);

            e.HasIndex(c => c.TaxId).IsUnique();
            e.HasIndex(c => c.UpdatedAtUtc);
        });

        modelBuilder.Entity<AuditLogEntity>(e =>
        {
            e.ToTable("AuditLogs");
            e.HasKey(a => a.Id);

            e.Property(a => a.EventType).HasMaxLength(80);
            e.Property(a => a.Outcome).HasMaxLength(20);
            e.Property(a => a.Actor).HasMaxLength(120);
            e.Property(a => a.Source).HasMaxLength(120);

            e.HasIndex(a => a.DocumentId);
            e.HasIndex(a => a.CreatedAtUtc);
            e.HasIndex(a => a.EventType);
        });
    }
}

