using arroyoSeco.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace arroyoSeco.Application.Features.Reservas.Commands.CambiarEstado;

public class CambiarEstadoReservaCommand
{
    public int ReservaId { get; set; }
    public string NuevoEstado { get; set; } = null!;
}

public class CambiarEstadoReservaCommandHandler
{
    private static readonly string[] Permitidos = { "Pendiente", "Confirmada", "Cancelada", "Completada" };
    private readonly IAppDbContext _ctx;

    public CambiarEstadoReservaCommandHandler(IAppDbContext ctx) => _ctx = ctx;

    public async Task Handle(CambiarEstadoReservaCommand request, CancellationToken ct = default)
    {
        if (!Permitidos.Contains(request.NuevoEstado)) throw new ArgumentException("Estado inválido");

        var reserva = await _ctx.Reservas.FirstOrDefaultAsync(r => r.Id == request.ReservaId, ct)
            ?? throw new KeyNotFoundException("Reserva no encontrada");

        reserva.Estado = request.NuevoEstado;
        await _ctx.SaveChangesAsync(ct);
    }
}