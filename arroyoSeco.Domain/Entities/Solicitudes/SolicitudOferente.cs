using System;

namespace arroyoSeco.Domain.Entities.Solicitudes;

public class SolicitudOferente
{
    public int Id { get; set; }
    public string NombreSolicitante { get; set; } = null!;
    public string Telefono { get; set; } = null!;
    public string NombreNegocio { get; set; } = null!;
    public string Correo { get; set; } = null!;
    public string? Mensaje { get; set; }
    public string Estatus { get; set; } = "Pendiente";
    public DateTime FechaSolicitud { get; set; } = DateTime.UtcNow;
    public DateTime? FechaRespuesta { get; set; }
    public string? AdminId { get; set; }
}