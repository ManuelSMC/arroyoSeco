using arroyoSeco.Application.Common.Interfaces;
using arroyoSeco.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace arroyoSeco.Infrastructure.Services;

public class FolioGenerator : IFolioGenerator
{
    private readonly AppDbContext _ctx;
    public FolioGenerator(AppDbContext ctx) => _ctx = ctx;

    public async Task<string> NextReservaFolioAsync(CancellationToken ct = default)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"RES-{year}-";
        var count = await _ctx.Reservas.CountAsync(r => r.Folio.StartsWith(prefix), ct);
        return $"{prefix}{(count + 1).ToString("D3")}";
    }
}