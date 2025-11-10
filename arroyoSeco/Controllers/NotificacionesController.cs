using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using arroyoSeco.Application.Common.Interfaces;

namespace arroyoSeco.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // requiere token
public class NotificacionesController : ControllerBase
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _current;
    private readonly INotificationService _noti;

    public NotificacionesController(IAppDbContext db, ICurrentUserService current, INotificationService noti)
    {
        _db = db;
        _current = current;
        _noti = noti;
    }

    // GET /api/notificaciones?soloNoLeidas=true
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool soloNoLeidas = false, CancellationToken ct = default)
    {
        IQueryable<arroyoSeco.Domain.Entities.Notificaciones.Notificacion> q =
            _db.Notificaciones.AsNoTracking()
               .Where(n => n.UsuarioId == _current.UserId);

        if (soloNoLeidas)
            q = q.Where(n => !n.Leida);

        var items = await q.OrderByDescending(n => n.Fecha).ToListAsync(ct);
        return Ok(items);
    }

    // PATCH /api/notificaciones/{id}/leer
    [HttpPatch("{id:int}/leer")]
    public async Task<IActionResult> MarcarLeida(int id, CancellationToken ct)
    {
        await _noti.MarkAsReadAsync(id, _current.UserId, ct);
        return NoContent();
    }

    // DELETE /api/notificaciones/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var n = await _db.Notificaciones.FirstOrDefaultAsync(x => x.Id == id && x.UsuarioId == _current.UserId, ct);
        if (n is null) return NotFound();
        _db.Notificaciones.Remove(n);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}