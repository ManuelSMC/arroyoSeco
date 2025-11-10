namespace arroyoSeco.Application.Common.Interfaces;

public interface IFolioGenerator
{
    Task<string> NextReservaFolioAsync(CancellationToken ct = default);
}