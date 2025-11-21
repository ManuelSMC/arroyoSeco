using System;

namespace arroyoSeco.Domain.Entities.Alojamientos;

public class Reserva
{
    public int Id { get; set; }
    public string Folio { get; set; } = null!;
    public int AlojamientoId { get; set; }
    public Alojamiento Alojamiento { get; set; } = null!;
    public string ClienteId { get; set; } = null!;
    public DateTime FechaEntrada { get; set; }
    public DateTime FechaSalida { get; set; }
    public decimal Total { get; set; }
    public string Estado { get; set; } = "Pendiente";
    public DateTime FechaReserva { get; set; } = DateTime.UtcNow;
    public string? ComprobanteUrl { get; set; }
}