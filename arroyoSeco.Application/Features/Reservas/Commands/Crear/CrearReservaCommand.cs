using System;

namespace arroyoSeco.Application.Features.Reservas.Commands.Crear;

public class CrearReservaCommand
{
    public int AlojamientoId { get; set; }
    public DateTime FechaEntrada { get; set; }
    public DateTime FechaSalida { get; set; }
}