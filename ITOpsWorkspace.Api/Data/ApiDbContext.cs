using ITOpsWorkspace.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace ITOpsWorkspace.Api.Data;

public class ApiDbContext : DbContext
{
    public ApiDbContext(DbContextOptions<ApiDbContext> options) : base(options)
    {
    }

    public DbSet<Organisation> Organisations => Set<Organisation>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Invitation> Invitations => Set<Invitation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Organisation>()
            .HasMany(o => o.Users)
            .WithOne(u => u.Organisation)
            .HasForeignKey(u => u.OrganisationId);

        modelBuilder.Entity<AppUser>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Invitation>()
            .HasOne(i => i.User)
            .WithMany()
            .HasForeignKey(i => i.UserId);

        modelBuilder.Entity<Invitation>()
            .HasIndex(i => i.Token)
            .IsUnique();
    }
}