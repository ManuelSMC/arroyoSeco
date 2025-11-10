using arroyoSeco.Domain.Entities.Alojamientos;
using arroyoSeco.Infrastructure.Data;
using System;

namespace arroyoSeco.Application.Features.Alojamiento.Commands.Crear;

public class CrearAlojamientoCommand
{
    public string Nombre { get; set; } = null!;
    public string Ubicacion { get; set; } = null!;
    public int MaxHuespedes { get; set; }
    public int Habitaciones { get; set; }
    public int Banos { get; set; }
    public decimal PrecioPorNoche { get; set; }
    public string? FotoPrincipal { get; set; }
    public List<string> FotosUrls { get; set; } = new();
}

public class CrearAlojamientoCommandHandler
{
    private readonly AppDbContext _context;

    public CrearAlojamientoCommandHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(CrearAlojamientoCommand request, CancellationToken ct = default)
    {
        var alojamiento = new arroyoSeco.Domain.Entities.Alojamientos.Alojamiento
        {
            OferenteId = "oferente-001", // Cambiá después con autenticación
            Nombre = request.Nombre,
            Ubicacion = request.Ubicacion,
            MaxHuespedes = request.MaxHuespedes,
            Habitaciones = request.Habitaciones,
            Banos = request.Banos,
            PrecioPorNoche = request.PrecioPorNoche,
            FotoPrincipal = request.FotoPrincipal,
            Fotos = request.FotosUrls.Select((url, i) => new FotoAlojamiento { Url = url, Orden = i + 1 }).ToList()
        };

        _context.Alojamientos.Add(alojamiento);
        await _context.SaveChangesAsync(ct);

        var oferente = await _context.Oferentes.FindAsync("oferente-001");
        if (oferente != null)
        {
            oferente.NumeroAlojamientos++;
            await _context.SaveChangesAsync(ct);
        }

        return alojamiento.Id;
    }
}