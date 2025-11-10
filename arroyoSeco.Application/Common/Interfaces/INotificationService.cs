using System.Threading;
using System.Threading.Tasks;
using arroyoSeco.Domain.Entities.Notificaciones;

namespace arroyoSeco.Application.Common.Interfaces;

public interface INotificationService
{
    Task<int> PushAsync(
        string usuarioId,
        string titulo,
        string mensaje,
        string tipo,
        string? url = null,
        CancellationToken ct = default);

    Task MarkAsReadAsync(
        int id,
        string usuarioId,
        CancellationToken ct = default);
}