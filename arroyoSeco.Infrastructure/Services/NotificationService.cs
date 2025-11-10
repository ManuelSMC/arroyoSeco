using arroyoSeco.Application.Common.Interfaces;
using arroyoSeco.Domain.Entities.Notificaciones;

namespace arroyoSeco.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly IAppDbContext _ctx;

    public NotificationService(IAppDbContext ctx) => _ctx = ctx;

    public async Task<int> PushAsync(
        string usuarioId,
        string titulo,
        string mensaje,
        string tipo,
        string? url = null,
        CancellationToken ct = default)
    {
        var n = new Notificacion
        {
            UsuarioId = usuarioId,
            Titulo = titulo,
            Mensaje = mensaje,
            Tipo = tipo,
            UrlAccion = url,
            Leida = false,
            Fecha = DateTime.UtcNow
        };

        _ctx.Notificaciones.Add(n);
        await _ctx.SaveChangesAsync(ct);
        return n.Id;
    }

    public async Task MarkAsReadAsync(int id, string usuarioId, CancellationToken ct = default)
    {
        var n = await _ctx.Notificaciones.FindAsync(new object[] { id }, ct);
        if (n is null || n.UsuarioId != usuarioId) return;

        n.Leida = true;
        await _ctx.SaveChangesAsync(ct);
    }
}