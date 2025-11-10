using System;
using System.Collections.Generic;
using arroyoSeco.Domain.Entities.Alojamientos;

namespace arroyoSeco.Domain.Entities.Usuarios;

public class Oferente
{
    public string Id { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public int NumeroAlojamientos { get; set; }
    public ICollection<Alojamiento> Alojamientos { get; set; } = new List<Alojamiento>();
}