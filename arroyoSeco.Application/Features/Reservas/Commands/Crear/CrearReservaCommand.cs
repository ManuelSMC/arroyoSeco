using arroyoSeco.Application.Common.Interfaces;
using arroyoSeco.Domain.Entities.Alojamientos;
using Microsoft.EntityFrameworkCore;

namespace arroyoSeco.Application.Features.Reservas.Commands.Crear;

public class CrearReservaCommand
{
    public int AlojamientoId { get; set; }
    public DateTime FechaEntrada { get; set; }
    public DateTime FechaSalida { get; set; }
}

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
        if (request.FechaSalida <= request.FechaEntrada) throw new ArgumentException("Rango de fechas inválido");

        var alojamiento = await _ctx.Alojamientos.FirstOrDefaultAsync(a => a.Id == request.AlojamientoId, ct)
            ?? throw new KeyNotFoundException("Alojamiento no encontrado");

        var overlapping = await _ctx.Reservas.AnyAsync(r =>
            r.AlojamientoId == request.AlojamientoId &&
            r.Estado != "Cancelada" &&
            request.FechaEntrada < r.FechaSalida &&
            request.FechaSalida > r.FechaEntrada, ct);

        if (overlapping) throw new InvalidOperationException("Fechas no disponibles");

        var noches = (request.FechaSalida - request.FechaEntrada).Days;
        if (noches <= 0) throw new ArgumentException("Noches inválidas");

        var folio = await _folio.NextReservaFolioAsync(ct);
        var total = noches * alojamiento.PrecioPorNoche;

        var reserva = new Reserva
        {
            Folio = folio,
            AlojamientoId = alojamiento.Id,
            ClienteId = _current.UserId,
            FechaEntrada = request.FechaEntrada.Date,
            FechaSalida = request.FechaSalida.Date,
            Total = total,
            Estado = "Pendiente"
        };

        _ctx.Reservas.Add(reserva);
        await _ctx.SaveChangesAsync(ct);

        await _noti.PushAsync(alojamiento.OferenteId,
            "Nueva reserva",
            $"Nueva reserva {folio} del {reserva.FechaEntrada:dd/MM} al {reserva.FechaSalida:dd/MM}",
            "ReservaNueva",
            $"/oferente/reservas/{reserva.Id}",
            ct);

        return reserva.Id;
    }
}