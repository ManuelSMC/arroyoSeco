namespace arroyoSeco.Domain.Entities.Alojamientos;

public class FotoAlojamiento
{
    public int Id { get; set; }
    public int AlojamientoId { get; set; }
    public Alojamiento Alojamiento { get; set; } = null!;
    public string Url { get; set; } = null!;
    public int Orden { get; set; }
}