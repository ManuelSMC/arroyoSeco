namespace arroyoSeco.Domain.Entities.Enums;

public static class EstadosAlojamiento
{
    public const string Activo = "Activo";
    public const string Inactivo = "Inactivo";
    public const string Reservado = "Reservado";
    public const string PendientePago = "PendientePago";
}

public static class EstadosReserva
{
    public const string Pendiente = "Pendiente";
    public const string Confirmada = "Confirmada";
    public const string Cancelada = "Cancelada";
    public const string Completada = "Completada";
}

public static class EstadosOferente
{
    public const string Activo = "Activo";
    public const string Inactivo = "Inactivo";
    public const string PendienteVerificacion = "PendienteVerificacion";
}

public static class EstatusSolicitudOferente
{
    public const string Pendiente = "Pendiente";
    public const string Aprobada = "Aprobada";
    public const string Rechazada = "Rechazada";
}

public static class TiposNotificacion
{
    public const string ReservaNueva = "ReservaNueva";
    public const string ReservaCancelada = "ReservaCancelada";
    public const string SolicitudOferente = "SolicitudOferente";
    public const string Mensaje = "Mensaje";
}