using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using arroyoSeco.Application.Common.Interfaces;
using arroyoSeco.Domain.Entities.Solicitudes;
using UsuarioOferente = arroyoSeco.Domain.Entities.Usuarios.Oferente;

namespace arroyoSeco.Controllers;

[ApiController]
[Route("api/admin/oferentes")]
[Authorize(Roles = "Admin")] // solo admin
public class OferentesAdminController : ControllerBase
{
    private readonly IAppDbContext _db;
    private readonly INotificationService _noti;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public OferentesAdminController(
        IAppDbContext db,
        INotificationService noti,
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _db = db;
        _noti = noti;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    // Crear usuario Identity de tipo Oferente y su registro en tabla Oferentes
    public record CrearUsuarioOferenteDto(string Email, string Password, string Nombre);

    [HttpPost("usuarios")]
    public async Task<IActionResult> CrearUsuarioOferente([FromBody] CrearUsuarioOferenteDto dto, CancellationToken ct)
    {
        var existing = await _userManager.FindByEmailAsync(dto.Email);
        if (existing is not null) return Conflict("Ya existe un usuario con ese email.");

        var user = new IdentityUser { UserName = dto.Email, Email = dto.Email, EmailConfirmed = true };
        var res = await _userManager.CreateAsync(user, dto.Password);
        if (!res.Succeeded) return BadRequest(res.Errors);

        if (!await _roleManager.RoleExistsAsync("Oferente"))
            await _roleManager.CreateAsync(new IdentityRole("Oferente"));
        await _userManager.AddToRoleAsync(user, "Oferente");

        // Crea el Oferente (dominio)
        if (!await _db.Oferentes.AnyAsync(o => o.Id == user.Id, ct))
        {
            var o = new UsuarioOferente { Id = user.Id, Nombre = dto.Nombre, NumeroAlojamientos = 0 };
            _db.Oferentes.Add(o);
            await _db.SaveChangesAsync(ct);
        }

        // Notificación simple
        await _noti.PushAsync(user.Id, "Cuenta de Oferente creada",
            "Tu cuenta de oferente ha sido creada por un administrador.", "Oferente", null, ct);

        return CreatedAtAction(nameof(Get), new { id = user.Id }, new { user.Id, user.Email });
    }

    // CRUD Oferente
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) =>
        Ok(await _db.Oferentes.AsNoTracking().ToListAsync(ct));

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id, CancellationToken ct)
    {
        var o = await _db.Oferentes.Include(x => x.Alojamientos).FirstOrDefaultAsync(x => x.Id == id, ct);
        return o is null ? NotFound() : Ok(o);
    }

    public record ActualizarOferenteDto(string Nombre);

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] ActualizarOferenteDto dto, CancellationToken ct)
    {
        var o = await _db.Oferentes.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (o is null) return NotFound();
        o.Nombre = dto.Nombre;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var o = await _db.Oferentes.Include(x => x.Alojamientos).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (o is null) return NotFound();
        if (o.Alojamientos?.Any() == true) return BadRequest("No se puede eliminar: tiene alojamientos asociados.");
        _db.Oferentes.Remove(o);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // Gestión de Solicitudes de Oferente (opcional si usas el flujo de solicitudes)
    [HttpGet("solicitudes")]
    public async Task<IActionResult> ListSolicitudes([FromQuery] string? estatus, CancellationToken ct)
    {
        var q = _db.SolicitudesOferente.AsQueryable();
        if (!string.IsNullOrWhiteSpace(estatus)) q = q.Where(s => s.Estatus == estatus);
        var items = await q.OrderByDescending(s => s.FechaSolicitud).AsNoTracking().ToListAsync(ct);
        return Ok(items);
    }

    [HttpPost("solicitudes/{id:int}/aprobar")]
    public async Task<IActionResult> Aprobar(int id, CancellationToken ct)
    {
        var s = await _db.SolicitudesOferente.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (s is null) return NotFound();

        // crea (o reutiliza) usuario por correo de la solicitud
        var email = s.Correo.Trim();
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
            var tempPass = "Temp" + Guid.NewGuid().ToString("N")[..8] + "!";
            var res = await _userManager.CreateAsync(user, tempPass);
            if (!res.Succeeded) return BadRequest(res.Errors);
            await _userManager.AddToRoleAsync(user, "Oferente");
        }

        if (!await _db.Oferentes.AnyAsync(o => o.Id == user.Id, ct))
        {
            _db.Oferentes.Add(new UsuarioOferente { Id = user.Id, Nombre = s.NombreNegocio, NumeroAlojamientos = 0 });
        }

        s.Estatus = "Aprobada";
        s.FechaRespuesta = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _noti.PushAsync(user.Id, "Solicitud aprobada",
            "Tu solicitud para ser oferente fue aprobada.", "SolicitudOferente", null, ct);

        return Ok(new { user.Id, email = user.Email });
    }

    [HttpPost("solicitudes/{id:int}/rechazar")]
    public async Task<IActionResult> Rechazar(int id, CancellationToken ct)
    {
        var s = await _db.SolicitudesOferente.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (s is null) return NotFound();
        s.Estatus = "Rechazada";
        s.FechaRespuesta = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}