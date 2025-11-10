using System;

namespace arroyoSeco.Domain.Entities.Notificaciones;

public class Notificacion
{
    public int Id { get; set; }
    public string UsuarioId { get; set; } = null!;
    public string Titulo { get; set; } = null!;
    public string Mensaje { get; set; } = null!;
    public string Tipo { get; set; } = null!;
    public bool Leida { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public string? UrlAccion { get; set; }
}