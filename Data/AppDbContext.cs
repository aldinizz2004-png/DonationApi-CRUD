using DonationApi.Models;
using Microsoft.EntityFrameworkCore;

namespace DonationApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Donation> Donations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Donation>(entity =>
        {
            entity.ToTable("Donations", t =>
                t.HasCheckConstraint("CK_Donations_Amount_Positive", "[Amount] > 0"));

            entity.HasKey(d => d.Id);

            entity.Property(d => d.Amount)
                  .HasPrecision(18, 2)
                  .IsRequired();

            entity.Property(d => d.DonorName)
                  .HasMaxLength(100)
                  .IsRequired();

            entity.Property(d => d.IsAnonymous)
                  .HasDefaultValue(false);

            entity.Property(d => d.CreatedAt)
                  .HasDefaultValueSql("SYSUTCDATETIME()")
                  .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

            entity.HasIndex(d => d.CreatedAt);
        });
    }
}