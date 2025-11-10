using Microsoft.AspNetCore.Mvc;
using arroyoSeco.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using arroyoSeco.Domain.Entities.Solicitudes;

namespace arroyoSeco.Api.Controllers;

[ApiController]
[Route("api/solicitudes-oferente")]
public class SolicitudesOferenteController : ControllerBase
{
    private readonly AppDbContext _ctx;
    public SolicitudesOferenteController(AppDbContext ctx) => _ctx = ctx;

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] SolicitudOferente s, CancellationToken ct)
    {
        s.Id = 0;
        _ctx.SolicitudesOferente.Add(s);
        await _ctx.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = s.Id }, s);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var s = await _ctx.SolicitudesOferente.FindAsync(id);
        return s == null ? NotFound() : Ok(s);
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? estatus)
    {
        var q = _ctx.SolicitudesOferente.AsQueryable();
        if (!string.IsNullOrWhiteSpace(estatus)) q = q.Where(x => x.Estatus == estatus);
        return Ok(await q.OrderByDescending(x => x.FechaSolicitud).ToListAsync());
    }

    [HttpPatch("{id:int}/estatus")]
    public async Task<IActionResult> Cambiar(int id, [FromBody] string nuevoEstatus, CancellationToken ct)
    {
        var s = await _ctx.SolicitudesOferente.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (s == null) return NotFound();
        s.Estatus = nuevoEstatus;
        s.FechaRespuesta = DateTime.UtcNow;
        await _ctx.SaveChangesAsync(ct);
        return NoContent();
    }
}