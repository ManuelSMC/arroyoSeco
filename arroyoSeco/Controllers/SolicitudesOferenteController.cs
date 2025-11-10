using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using arroyoSeco.Application.Common.Interfaces;
using arroyoSeco.Domain.Entities.Solicitudes;

namespace arroyoSeco.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SolicitudesOferenteController : ControllerBase
{
    private readonly IAppDbContext _db;
    public SolicitudesOferenteController(IAppDbContext db) => _db = db;

    // GET /api/solicitudesoferente?estatus=Pendiente
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? estatus, CancellationToken ct)
    {
        var q = _db.SolicitudesOferente.AsQueryable();
        if (!string.IsNullOrWhiteSpace(estatus)) q = q.Where(s => s.Estatus == estatus);
        return Ok(await q.AsNoTracking().ToListAsync(ct));
    }

    // POST /api/solicitudesoferente
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] SolicitudOferente s, CancellationToken ct)
    {
        s.Id = 0;
        s.FechaSolicitud = DateTime.UtcNow;
        _db.SolicitudesOferente.Add(s);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = s.Id }, s.Id);
    }

    // GET /api/solicitudesoferente/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var s = await _db.SolicitudesOferente.FindAsync(new object[] { id }, ct);
        return s is null ? NotFound() : Ok(s);
    }
}