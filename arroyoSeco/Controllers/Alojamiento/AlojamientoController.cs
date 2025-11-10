using arroyoSeco.Application.Features.Alojamiento.Commands.Crear;
using arroyoSeco.Domain.Entities.Alojamientos;
using arroyoSeco.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace arroyoSeco.Controllers;

[Route("api/alojamientos")]
[ApiController]
public class AlojamientoController : ControllerBase
{
    private readonly CrearAlojamientoCommandHandler _handler;
    private readonly AppDbContext _context;

    public AlojamientoController(CrearAlojamientoCommandHandler handler, AppDbContext context)
    {
        _handler = handler;
        _context = context;
    }

    [HttpPost]
    public async Task<ActionResult<int>> Crear([FromBody] CrearAlojamientoCommand command)
    {
        var id = await _handler.Handle(command);
        return Created($"/api/alojamientos/{id}", id);
    }

    [HttpGet]
    public async Task<ActionResult<List<Alojamiento>>> GetAll()
    {
        var datos = await _context.Alojamientos
            .Include(a => a.Oferente)
            .Include(a => a.Fotos)
            .ToListAsync();
        return Ok(datos);
    }
}