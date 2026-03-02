using System.Collections.Generic;
using System.Reflection.Emit;
using Importer.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Importer.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // AppUser
        modelBuilder.Entity<AppUser>(e =>
        {
            e.ToTable("Users");
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Username).IsUnique();
            e.Property(u => u.Role).HasMaxLength(20);
        });

        // RefreshToken
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
    }
}