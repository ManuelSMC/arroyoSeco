using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Text.Json;
using arroyoSeco.Application.Common.Interfaces;
using arroyoSeco.Application.Features.Reservas.Commands.Crear;
using arroyoSeco.Application.Features.Reservas.Commands.CambiarEstado;
using arroyoSeco.Infrastructure.Storage;
using arroyoSeco.Domain.Entities;
using arroyoSeco.Domain.Entities.Alojamientos;
using System.Data;

namespace arroyoSeco.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReservasController : ControllerBase
{
    private readonly IAppDbContext _db;
    private readonly CrearReservaCommandHandler _crear;
    private readonly CambiarEstadoReservaCommandHandler _cambiarEstado;
    private readonly ICurrentUserService _current;
    private readonly string _comprobantesPath;
    private readonly UserManager<IdentityUser> _userManager;

    public ReservasController(
        IAppDbContext db,
        CrearReservaCommandHandler crear,
        CambiarEstadoReservaCommandHandler cambiarEstado,
        ICurrentUserService current,
        IOptions<StorageOptions> storage,
        UserManager<IdentityUser> userManager)
    {
        _db = db;
        _crear = crear;
        _cambiarEstado = cambiarEstado;
        _current = current;
        _comprobantesPath = storage.Value.ComprobantesPath;
        _userManager = userManager;
    }

    // GET /api/reservas/alojamiento/{alojamientoId}?estado=Pendiente
    // Devuelve reservas de un alojamiento. Oferente sólo si es dueño; Admin cualquiera.
    [Authorize(Roles = "Admin,Oferente")]
    [HttpGet("alojamiento/{alojamientoId:int}")]
    public async Task<IActionResult> PorAlojamiento(int alojamientoId, [FromQuery] string? estado, CancellationToken ct)
    {
        if (alojamientoId <= 0) return BadRequest("alojamientoId inválido");

        if (User.IsInRole("Oferente"))
        {
            var esMio = await _db.Alojamientos
                .AsNoTracking()
                .AnyAsync(a => a.Id == alojamientoId && a.OferenteId == _current.UserId, ct);
            if (!esMio) return Forbid();
        }

        var q = _db.Reservas
            .AsNoTracking()
            .Where(r => r.AlojamientoId == alojamientoId);

        if (!string.IsNullOrWhiteSpace(estado))
            q = q.Where(r => r.Estado == estado);

        var items = await q
            .Include(r => r.Alojamiento) // mover Include después de los Where
            .OrderByDescending(r => r.FechaReserva)
            .ToListAsync(ct);

        // Obtener nombres de clientes (Identity) en bloque
        var ids = items.Select(i => i.ClienteId).Distinct().ToList();
        var nombres = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var id in ids)
        {
            var u = await _userManager.FindByIdAsync(id);
            nombres[id] = u?.Email ?? u?.UserName ?? id;
        }

        var result = items.Select(r => new
        {
            r.Id,
            r.Folio,
            r.AlojamientoId,
            AlojamientoNombre = r.Alojamiento?.Nombre,
            r.ClienteId,
            Huesped = nombres.TryGetValue(r.ClienteId, out var nom) ? nom : r.ClienteId,
            r.Estado,
            r.FechaEntrada,
            r.FechaSalida,
            r.Total,
            r.FechaReserva,
            r.ComprobanteUrl
        });

        return Ok(result);
    }

    // POST JSON simple (sin comprobante)
    // POST JSON simple (sin comprobante) con manejo de errores de disponibilidad
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearReservaCommand cmd, CancellationToken ct)
    {
        try
        {
            var id = await _crear.Handle(cmd, ct);
            var r = await _db.Reservas.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
            if (r is null) return Created(nameof(Crear), new { Id = id });
            return CreatedAtAction(nameof(GetByFolio), new { folio = r.Folio }, new { r.Id, r.Folio, r.Estado });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Fechas no disponibles", StringComparison.OrdinalIgnoreCase))
        {
            return Conflict(new { error = "Fechas no disponibles", detalle = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error interno", detalle = ex.Message });
        }
    }

    // POST multipart: reserva + comprobante
    // FormData: reserva (JSON string), comprobante (File PDF/JPG/PNG)
    [HttpPost("crear-con-comprobante")]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> CrearConComprobante([FromForm] string reserva, [FromForm] IFormFile comprobante, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reserva)) return BadRequest("Campo 'reserva' requerido.");
        if (comprobante is null || comprobante.Length == 0) return BadRequest("Archivo 'comprobante' requerido.");

        var permitidos = new[] { "application/pdf", "image/jpeg", "image/png" };
        if (!permitidos.Contains(comprobante.ContentType))
            return BadRequest("Formato no permitido (PDF/JPG/PNG).");

        CrearReservaCommand? cmd;
        try
        {
            cmd = JsonSerializer.Deserialize<CrearReservaCommand>(reserva, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (cmd is null) return BadRequest("JSON inválido.");
        }
        catch (Exception ex)
        {
            return BadRequest($"Error JSON: {ex.Message}");
        }

        int id;
        try
        {
            id = await _crear.Handle(cmd!, ct);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Fechas no disponibles", StringComparison.OrdinalIgnoreCase))
        {
            return Conflict(new { error = "Fechas no disponibles", detalle = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest($"Error creando reserva: {ex.Message}");
        }

        var entidad = await _db.Reservas.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entidad is null) return StatusCode(500, "Reserva no disponible.");

        Directory.CreateDirectory(_comprobantesPath);
        var ext = Path.GetExtension(comprobante.FileName);
        var safeFolio = string.Join("_", (entidad.Folio ?? "folio").Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        var fileName = $"{safeFolio}_{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(_comprobantesPath, fileName);

        try
        {
            await using var fs = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
            await comprobante.CopyToAsync(fs, ct);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error guardando archivo: {ex.Message}");
        }

        entidad.ComprobanteUrl = $"/comprobantes/{fileName}";
        if (entidad.Estado == "Pendiente")
            entidad.Estado = "PagoEnRevision";

        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetByFolio),
            new { folio = entidad.Folio },
            new { entidad.Id, entidad.Folio, entidad.ComprobanteUrl, entidad.Estado });
    }

    // Subir/actualizar comprobante después (opcional)
    [HttpPost("{id:int}/comprobante")]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> SubirComprobante(int id, IFormFile archivo, CancellationToken ct)
    {
        if (archivo is null || archivo.Length == 0) return BadRequest("Archivo requerido.");
        var permitidos = new[] { "application/pdf", "image/jpeg", "image/png" };
        if (!permitidos.Contains(archivo.ContentType))
            return BadRequest("Formato no permitido.");

        var r = await _db.Reservas.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (r is null) return NotFound();

        // Solo cliente dueño, oferente propietario o admin (simplificado)
        var esCliente = r.ClienteId == _current.UserId;
        var esOferente = User.IsInRole("Oferente");
        var esAdmin = User.IsInRole("Admin");
        if (!(esCliente || esOferente || esAdmin))
            return Forbid();

        Directory.CreateDirectory(_comprobantesPath);
        var ext = Path.GetExtension(archivo.FileName);
        var safeFolio = string.Join("_", (r.Folio ?? "folio").Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        var fileName = $"{safeFolio}_{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(_comprobantesPath, fileName);

        try
        {
            await using var fs = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
            await archivo.CopyToAsync(fs, ct);
            await fs.FlushAsync(ct);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error guardando archivo: {ex.Message}");
        }

        r.ComprobanteUrl = $"/comprobantes/{fileName}";
        if (esCliente && r.Estado == "Pendiente")
            r.Estado = "PagoEnRevision";

        await _db.SaveChangesAsync(ct);
        return Ok(new { r.Id, r.Folio, r.ComprobanteUrl, r.Estado });
    }

    // GET folio
    [HttpGet("folio/{folio}")]
    public async Task<IActionResult> GetByFolio(string folio, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(folio)) return BadRequest("Folio requerido.");
        var r = await _db.Reservas.AsNoTracking()
            .Include(x => x.Alojamiento)
            .FirstOrDefaultAsync(x => x.Folio == folio, ct);
        return r is null ? NotFound() : Ok(r);
    }

    public record CambiarEstadoDto(string Estado);

    [Authorize(Roles = "Admin,Oferente")]
    [HttpPatch("{id:int}/estado")]
    public async Task<IActionResult> CambiarEstado(int id, [FromBody] CambiarEstadoDto dto, CancellationToken ct)
    {
        var cmd = new CambiarEstadoReservaCommand { ReservaId = id, NuevoEstado = dto.Estado };
        await _cambiarEstado.Handle(cmd, ct);
        return NoContent();
    }

    // GET /api/reservas/activas?clienteId=GUID&alojamientoId=123
    // Activas: FechaEntrada <= now && FechaSalida > now && Estado <> Cancelada
    [HttpGet("activas")]
    public async Task<IActionResult> Activas([FromQuery] string? clienteId, [FromQuery] int? alojamientoId, CancellationToken ct)
    {
        var now = DateTime.UtcNow; // comparación directa, evita CONVERT(date)

        IQueryable<Reserva> q = _db.Reservas
            .AsNoTracking()
            .Include(r => r.Alojamiento)
            .Where(r => r.Estado != "Cancelada" && r.FechaEntrada <= now && r.FechaSalida > now);

        // Filtrado por cliente: si se pasa clienteId lo usamos, sino si el usuario tiene rol Cliente usamos su propio Id.
        if (!string.IsNullOrWhiteSpace(clienteId))
        {
            q = q.Where(r => r.ClienteId == clienteId);
        }
        else if (User.IsInRole("Cliente"))
        {
            q = q.Where(r => r.ClienteId == _current.UserId);
        }

        // Filtrar por alojamiento si se envía
        if (alojamientoId.HasValue && alojamientoId > 0)
            q = q.Where(r => r.AlojamientoId == alojamientoId.Value);

        // Si es oferente restringir a sus alojamientos
        if (User.IsInRole("Oferente"))
            q = q.Where(r => r.Alojamiento!.OferenteId == _current.UserId);

        var items = await q.OrderBy(r => r.FechaEntrada).ToListAsync(ct);
        var nombres = await MapearNombres(items.Select(r => r.ClienteId).Distinct());

        var result = items.Select(r => new
        {
            r.Id,
            r.Folio,
            r.AlojamientoId,
            AlojamientoNombre = r.Alojamiento?.Nombre,
            r.ClienteId,
            Huesped = nombres.TryGetValue(r.ClienteId, out var nom) ? nom : r.ClienteId,
            r.Estado,
            r.FechaEntrada,
            r.FechaSalida,
            r.Total,
            r.FechaReserva,
            r.ComprobanteUrl,
            EnCurso = true
        });
        return Ok(result);
    }

    // GET /api/reservas/historial?clienteId=GUID&alojamientoId=123
    // Historial: FechaSalida <= now OR estado en (Cancelada, Completada)
    [HttpGet("historial")]
    public async Task<IActionResult> Historial([FromQuery] string? clienteId, [FromQuery] int? alojamientoId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var estadosFin = new[] { "Cancelada", "Completada" };

        IQueryable<Reserva> q = _db.Reservas
            .AsNoTracking()
            .Include(r => r.Alojamiento)
            .Where(r => r.FechaSalida <= now || estadosFin.Contains(r.Estado));

        if (!string.IsNullOrWhiteSpace(clienteId))
        {
            q = q.Where(r => r.ClienteId == clienteId);
        }
        else if (User.IsInRole("Cliente"))
        {
            q = q.Where(r => r.ClienteId == _current.UserId);
        }

        if (alojamientoId.HasValue && alojamientoId > 0)
            q = q.Where(r => r.AlojamientoId == alojamientoId.Value);

        if (User.IsInRole("Oferente"))
            q = q.Where(r => r.Alojamiento!.OferenteId == _current.UserId);

        var items = await q
            .OrderByDescending(r => r.FechaSalida)
            .ThenByDescending(r => r.FechaReserva)
            .ToListAsync(ct);
        var nombres = await MapearNombres(items.Select(r => r.ClienteId).Distinct());

        var result = items.Select(r => new
        {
            r.Id,
            r.Folio,
            r.AlojamientoId,
            AlojamientoNombre = r.Alojamiento?.Nombre,
            r.ClienteId,
            Huesped = nombres.TryGetValue(r.ClienteId, out var nom) ? nom : r.ClienteId,
            r.Estado,
            r.FechaEntrada,
            r.FechaSalida,
            r.Total,
            r.FechaReserva,
            r.ComprobanteUrl,
            EnCurso = false
        });
        return Ok(result);
    }

    // Nuevo: historial completo de un cliente (todas sus reservas, más recientes primero)
    [HttpGet("cliente/{clienteId}/historial")]
    public async Task<IActionResult> HistorialCliente(string clienteId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(clienteId)) return BadRequest("clienteId requerido");
        var esMismoCliente = _current.UserId == clienteId;
        var esAdmin = User.IsInRole("Admin");
        if (!(esMismoCliente || esAdmin)) return Forbid();

        var items = await _db.Reservas
            .AsNoTracking()
            .Include(r => r.Alojamiento)
            .Where(r => r.ClienteId == clienteId)
            .OrderByDescending(r => r.FechaEntrada) // más actuales primero
            .ThenByDescending(r => r.FechaReserva)
            .ToListAsync(ct);

        var nombres = await MapearNombres(new[] { clienteId });
        var nombre = nombres.TryGetValue(clienteId, out var n) ? n : clienteId;

        var result = items.Select(r => new
        {
            r.Id,
            r.Folio,
            r.AlojamientoId,
            AlojamientoNombre = r.Alojamiento?.Nombre,
            r.ClienteId,
            Huesped = nombre,
            r.Estado,
            r.FechaEntrada,
            r.FechaSalida,
            r.Total,
            r.FechaReserva,
            r.ComprobanteUrl
        });
        return Ok(result);
    }
    //
    private async Task<Dictionary<string,string>> MapearNombres(IEnumerable<string> ids)
    {
        var dic = new Dictionary<string,string>();
        foreach (var id in ids)
        {
            var u = await _userManager.FindByIdAsync(id);
            dic[id] = u?.Email ?? u?.UserName ?? id;
        }
        return dic;
    }
}