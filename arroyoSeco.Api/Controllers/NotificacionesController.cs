using Microsoft.AspNetCore.Mvc;
using arroyoSeco.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using arroyoSeco.Application.Common.Interfaces;

namespace arroyoSeco.Api.Controllers;

[ApiController]
[Route("api/notificaciones")]
public class NotificacionesController : ControllerBase
{
    private readonly AppDbContext _ctx;
    private readonly ICurrentUserService _current;
    private readonly INotificationService _noti;

    public NotificacionesController(AppDbContext ctx, ICurrentUserService current, INotificationService noti)
    {
        _ctx = ctx; _current = current; _noti = noti;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] bool soloNoLeidas = false)
    {
        var q = _ctx.Notificaciones.Where(n => n.UsuarioId == _current.UserId);
        if (soloNoLeidas) q = q.Where(n => !n.Leida);
        var list = await q.OrderByDescending(n => n.Fecha).ToListAsync();
        return Ok(list);
    }

    [HttpPost("{id:int}/leer")]
    public async Task<IActionResult> Leer(int id, CancellationToken ct)
    {
        await _noti.MarkAsReadAsync(id, _current.UserId, ct);
        return NoContent();
    }
}