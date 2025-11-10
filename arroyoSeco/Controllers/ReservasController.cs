using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using arroyoSeco.Application.Common.Interfaces;
using arroyoSeco.Application.Features.Reservas.Commands.Crear;
using arroyoSeco.Application.Features.Reservas.Commands.CambiarEstado;

namespace arroyoSeco.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // todas requieren token
public class ReservasController : ControllerBase
{
    private readonly IAppDbContext _db;
    private readonly CrearReservaCommandHandler _crear;
    private readonly CambiarEstadoReservaCommandHandler _cambiarEstado;

    public ReservasController(IAppDbContext db,
        CrearReservaCommandHandler crear,
        CambiarEstadoReservaCommandHandler cambiarEstado)
    {
        _db = db;
        _crear = crear;
        _cambiarEstado = cambiarEstado;
    }

    // GET /api/reservas/{folio}
    [HttpGet("{folio}")]
    public async Task<IActionResult> GetByFolio(string folio, CancellationToken ct)
    {
        var r = await _db.Reservas.AsNoTracking().FirstOrDefaultAsync(x => x.Folio == folio, ct);
        return r is null ? NotFound() : Ok(r);
    }

    // POST /api/reservas
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearReservaCommand cmd, CancellationToken ct)
    {
        var reservaId = await _crear.Handle(cmd, ct);
        var reserva = await _db.Reservas.AsNoTracking().FirstOrDefaultAsync(r => r.Id == reservaId, ct);
        if (reserva is null) return Created(nameof(Crear), new { Id = reservaId });
        return CreatedAtAction(nameof(GetByFolio), new { folio = reserva.Folio }, new { reserva.Id, reserva.Folio });
    }

    // PATCH /api/reservas/{id}/estado  (puedes limitar a Admin y Oferente)
    public record CambiarEstadoDto(string Estado);

    [Authorize(Roles = "Admin,Oferente")]
    [HttpPatch("{id:int}/estado")]
    public async Task<IActionResult> CambiarEstado(int id, [FromBody] CambiarEstadoDto dto, CancellationToken ct)
    {
        var cmd = new CambiarEstadoReservaCommand
        {
            ReservaId = id,
            NuevoEstado = dto.Estado
        };
        await _cambiarEstado.Handle(cmd, ct);
        return NoContent();
    }
}