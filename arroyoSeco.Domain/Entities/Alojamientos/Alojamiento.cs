using System.Collections.Generic;
using UsuarioOferente = arroyoSeco.Domain.Entities.Usuarios.Oferente;

namespace arroyoSeco.Domain.Entities.Alojamientos;

public class Alojamiento
{
    public int Id { get; set; }
    public string OferenteId { get; set; } = null!;
    public UsuarioOferente? Oferente { get; set; }

    public string Nombre { get; set; } = null!;
    public string Ubicacion { get; set; } = null!;
    public int MaxHuespedes { get; set; }
    public int Habitaciones { get; set; }
    public int Banos { get; set; }
    public decimal PrecioPorNoche { get; set; }
    public string? FotoPrincipal { get; set; }

    public List<FotoAlojamiento> Fotos { get; set; } = new();
    public List<Reserva> Reservas { get; set; } = new();
}