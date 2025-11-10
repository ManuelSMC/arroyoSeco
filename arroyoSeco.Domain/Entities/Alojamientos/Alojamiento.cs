namespace arroyoSeco.Domain.Entities.Alojamientos;

public class Alojamiento
{
    public int Id { get; set; }
    public string OferenteId { get; set; } = null!;
    public Oferente Oferente { get; set; } = null!;

    public string Nombre { get; set; } = null!;
    public string Ubicacion { get; set; } = null!;
    public int MaxHuespedes { get; set; }
    public int Habitaciones { get; set; }
    public int Banos { get; set; }
    public decimal PrecioPorNoche { get; set; }
    public string? FotoPrincipal { get; set; }
    public string Estado { get; set; } = "Activo";
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public List<FotoAlojamiento> Fotos { get; set; } = new();
}