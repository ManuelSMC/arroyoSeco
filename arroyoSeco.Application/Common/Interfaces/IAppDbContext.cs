using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using arroyoSeco.Domain.Entities.Alojamientos;
using arroyoSeco.Domain.Entities.Notificaciones;
using arroyoSeco.Domain.Entities.Solicitudes;
// Alias para desambiguar 'Oferente'
using UsuarioOferente = arroyoSeco.Domain.Entities.Usuarios.Oferente;

namespace arroyoSeco.Application.Common.Interfaces;

public interface IAppDbContext
{
    DbSet<UsuarioOferente> Oferentes { get; }
    DbSet<Alojamiento> Alojamientos { get; }
    DbSet<FotoAlojamiento> FotosAlojamiento { get; }
    DbSet<Reserva> Reservas { get; }
    DbSet<Notificacion> Notificaciones { get; }
    DbSet<SolicitudOferente> SolicitudesOferente { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}