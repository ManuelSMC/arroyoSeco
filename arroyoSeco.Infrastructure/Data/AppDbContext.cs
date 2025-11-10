using arroyoSeco.Domain.Entities.Alojamientos;
using Microsoft.EntityFrameworkCore;

namespace arroyoSeco.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Oferente> Oferentes => Set<Oferente>();
    public DbSet<Alojamiento> Alojamientos => Set<Alojamiento>();
    public DbSet<FotoAlojamiento> FotosAlojamiento => Set<FotoAlojamiento>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Oferente>(entity =>
        {
            entity.ToTable("Oferentes");
            entity.HasKey(o => o.Id);
            entity.Property(o => o.Id).HasMaxLength(450);
            entity.Property(o => o.Estado).HasMaxLength(50);
        });

        builder.Entity<Alojamiento>(entity =>
        {
            entity.ToTable("Alojamientos");
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Estado).HasMaxLength(50);
            entity.Property(a => a.FotoPrincipal).HasColumnType("LONGTEXT");
        });

        builder.Entity<FotoAlojamiento>(entity =>
        {
            entity.ToTable("FotosAlojamiento");
            entity.Property(f => f.Url).HasColumnType("LONGTEXT");
        });
    }
}