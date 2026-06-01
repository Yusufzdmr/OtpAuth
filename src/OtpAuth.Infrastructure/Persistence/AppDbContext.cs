using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OtpAuth.Domain.Entities;
using OtpAuth.Infrastructure.Identity;

namespace OtpAuth.Infrastructure.Persistence;

/// <summary>
/// Uygulamanın EF Core DbContext'i. Identity tablolarını (AspNetUsers vb.) ve
/// OtpCodes tablosunu MSSQL üzerinde barındırır.
/// </summary>
public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<OtpCode>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.Property(o => o.PhoneNumber).IsRequired().HasMaxLength(20);
            entity.Property(o => o.Code).IsRequired().HasMaxLength(6);
            // Aynı numara için en güncel/aktif kodu hızlı bulmak adına index.
            entity.HasIndex(o => new { o.PhoneNumber, o.IsUsed });
        });
    }
}
