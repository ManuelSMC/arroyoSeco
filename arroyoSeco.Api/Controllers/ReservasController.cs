using Microsoft.AspNetCore.Mvc;
using arroyoSeco.Application.Features.Reservas.Commands.Crear;
using arroyoSeco.Application.Features.Reservas.Commands.CambiarEstado;
using arroyoSeco.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace arroyoSeco.Api.Controllers;

[ApiController]
[Route("api/reservas")]
public class ReservasController : ControllerBase
{
    private readonly AppDbContext _ctx;
    private readonly CrearReservaCommandHandler _crear;
    private readonly CambiarEstadoReservaCommandHandler _cambiar;

    public ReservasController(AppDbContext ctx, CrearReservaCommandHandler crear, CambiarEstadoReservaCommandHandler cambiar)
    {
        _ctx = ctx; _crear = crear; _cambiar = cambiar;
    }

    [HttpGet("por-alojamiento/{alojamientoId:int}")]
    public async Task<IActionResult> PorAlojamiento(int alojamientoId)
    {
        var list = await _ctx.Reservas
            .Where(r => r.AlojamientoId == alojamientoId)
            .OrderByDescending(r => r.FechaReserva)
            .Select(r => new { r.Id, r.Folio, r.FechaEntrada, r.FechaSalida, r.Total, r.Estado })
            .ToListAsync();
        return Ok(list);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearReservaCommand cmd, CancellationToken ct)
    {
        var id = await _crear.Handle(cmd, ct);
        return CreatedAtAction(nameof(Get), new { id }, new { id });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var r = await _ctx.Reservas.FirstOrDefaultAsync(x => x.Id == id);
        return r == null ? NotFound() : Ok(r);
    }

    [HttpPatch("{id:int}/estado")]
    public async Task<IActionResult> Cambiar(int id, [FromBody] string nuevoEstado, CancellationToken ct)
    {
        await _cambiar.Handle(new CambiarEstadoReservaCommand { ReservaId = id, NuevoEstado = nuevoEstado }, ct);
        return NoContent();
    }
}