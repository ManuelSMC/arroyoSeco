namespace arroyoSeco.Domain.Entities.Alojamientos;

public class Oferente
{
    public string Id { get; set; } = null!;
    public string NombreCompleto { get; set; } = null!;
    public string? Telefono { get; set; }
    public int NumeroAlojamientos { get; set; } = 0;
    public string Estado { get; set; } = "PendienteVerificacion"; // string porque MySQL ENUM → string
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
    public List<Alojamiento> Alojamientos { get; set; } = new();
}