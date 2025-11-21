using Microsoft.EntityFrameworkCore;
using arroyoSeco.Application.Common.Interfaces;
using arroyoSeco.Domain.Entities.Alojamientos;

namespace arroyoSeco.Application.Features.Reservas.Commands.Crear;

public class CrearReservaCommandHandler
{
    private readonly IAppDbContext _ctx;
    private readonly ICurrentUserService _current;
    private readonly IFolioGenerator _folio;
    private readonly INotificationService _noti;

    public CrearReservaCommandHandler(IAppDbContext ctx, ICurrentUserService current, IFolioGenerator folio, INotificationService noti)
    {
        _ctx = ctx;
        _current = current;
        _folio = folio;
        _noti = noti;
    }

    public async Task<int> Handle(CrearReservaCommand request, CancellationToken ct = default)
    {
        // Normalizar fechas (solo fecha, sin hora) si quieres bloquear por días.
        var entrada = request.FechaEntrada.Date;
        var salida = request.FechaSalida.Date;

        if (entrada >= salida)
            throw new InvalidOperationException("Rango de fechas inválido");

        // Estado que bloquea disponibilidad (confirmadas o en curso de confirmación)
        var estadosBloquean = new[] { "Confirmada", "PagoEnRevision" };

        // Solapamiento (entrada < existente.FechaSalida && salida > existente.FechaEntrada)
        var existeOverlap = await _ctx.Reservas
            .AsNoTracking()
            .AnyAsync(r =>
                r.AlojamientoId == request.AlojamientoId &&
                estadosBloquean.Contains(r.Estado) &&
                entrada < r.FechaSalida &&
                salida > r.FechaEntrada, ct);

        if (existeOverlap)
            throw new InvalidOperationException("Fechas no disponibles");

        var alojamiento = await _ctx.Alojamientos
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.AlojamientoId, ct);
        if (alojamiento is null)
            throw new InvalidOperationException("Alojamiento inexistente");

        var noches = (salida - entrada).Days;
        if (noches <= 0) throw new InvalidOperationException("Duración inválida");

        var total = noches * alojamiento.PrecioPorNoche;

        var reserva = new Reserva
        {
            AlojamientoId = request.AlojamientoId,
            ClienteId = _current.UserId,
            FechaEntrada = entrada,
            FechaSalida = salida,
            Total = total,
            Folio = await _folio.NextReservaFolioAsync(ct),
            Estado = "Pendiente",
            FechaReserva = DateTime.UtcNow
        };

        _ctx.Reservas.Add(reserva);
        await _ctx.SaveChangesAsync(ct);

        // Notificación al oferente (opcional)
        if (!string.IsNullOrWhiteSpace(alojamiento.OferenteId))
        {
            await _noti.PushAsync(alojamiento.OferenteId,
                "Nueva reserva",
                $"Reserva {reserva.Folio} pendiente de comprobante.",
                "Reserva",
                url: $"/reservas/{reserva.Folio}",
                ct: ct);
        }

        return reserva.Id;
    }
}