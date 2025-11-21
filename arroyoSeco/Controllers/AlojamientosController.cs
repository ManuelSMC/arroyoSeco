using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using arroyoSeco.Application.Common.Interfaces;
using arroyoSeco.Application.Features.Alojamiento.Commands.Crear;
using AlojamientoEntity = arroyoSeco.Domain.Entities.Alojamientos.Alojamiento;

namespace arroyoSeco.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AlojamientosController : ControllerBase
{
    private readonly IAppDbContext _db;
    private readonly CrearAlojamientoCommandHandler _crear;
    private readonly ICurrentUserService _current;

    public AlojamientosController(IAppDbContext db, CrearAlojamientoCommandHandler crear, ICurrentUserService current)
    {
        _db = db;
        _crear = crear;
        _current = current;
    }

    // Público
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AlojamientoEntity>>> List(CancellationToken ct)
        => Ok(await _db.Alojamientos
            .Include(a => a.Fotos)
            .AsNoTracking()
            .ToListAsync(ct));

    // Público
    [AllowAnonymous]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<AlojamientoEntity>> GetById(int id, CancellationToken ct)
    {
        var a = await _db.Alojamientos
            .Include(x => x.Fotos)
            .Include(x => x.Reservas)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        return a is null ? NotFound() : Ok(a);
    }

    // Nuevo: rangos ocupados (Confirmada) para pintar en calendario
    [AllowAnonymous]
    [HttpGet("{id:int}/calendario")]
    public async Task<IActionResult> Calendario(int id, CancellationToken ct)
    {
        var rangos = await _db.Reservas
            .Where(r => r.AlojamientoId == id && r.Estado == "Confirmada")
            .Select(r => new { inicio = r.FechaEntrada, fin = r.FechaSalida })
            .AsNoTracking()
            .ToListAsync(ct);

        return Ok(rangos);
    }

    // Solo Oferente autenticado: obtiene sus alojamientos
    [Authorize(Roles = "Oferente")]
    [HttpGet("mios")]
    public async Task<ActionResult<IEnumerable<AlojamientoEntity>>> MisAlojamientos(CancellationToken ct)
    {
        var userId = _current.UserId;
        var items = await _db.Alojamientos
            .Where(a => a.OferenteId == userId)
            .Include(a => a.Fotos)
            .Include(a => a.Reservas)
            .AsNoTracking()
            .ToListAsync(ct);

        return Ok(items);
    }

    // Solo Oferente autenticado
    [Authorize(Roles = "Oferente")]
    [HttpPost]
    public async Task<ActionResult<int>> Crear([FromBody] CrearAlojamientoCommand cmd, CancellationToken ct)
    {
        var id = await _crear.Handle(cmd, ct);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    public record ActualizarAlojamientoDto(string Nombre, string Ubicacion, int MaxHuespedes, int Habitaciones, int Banos, decimal PrecioPorNoche, string? FotoPrincipal);

    // Solo Oferente
    [Authorize(Roles = "Oferente")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ActualizarAlojamientoDto dto, CancellationToken ct)
    {
        var a = await _db.Alojamientos.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (a is null) return NotFound();

        a.Nombre = dto.Nombre;
        a.Ubicacion = dto.Ubicacion;
        a.MaxHuespedes = dto.MaxHuespedes;
        a.Habitaciones = dto.Habitaciones;
        a.Banos = dto.Banos;
        a.PrecioPorNoche = dto.PrecioPorNoche;
        a.FotoPrincipal = dto.FotoPrincipal;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // Solo Oferente
    [Authorize(Roles = "Oferente")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var a = await _db.Alojamientos.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (a is null) return NotFound();
        _db.Alojamientos.Remove(a);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}