using EvolFit.Application.Features.Auth.Interfaces;
using EvolFit.Core.Entities;
using EvolFit.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace EvolFit.Infrastructure.Data.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly EvolFitDbContext _context;

    public RefreshTokenRepository(EvolFitDbContext context)
    {
        _context = context;
    }

    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default) =>
        _context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public async Task AddAsync(RefreshToken token, CancellationToken ct = default) =>
        await _context.RefreshTokens.AddAsync(token, ct);

    public void Update(RefreshToken token) => _context.RefreshTokens.Update(token);

    public async Task RevokeAllForUserAsync(int userId, CancellationToken ct = default)
    {
        var tokens = await _context.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync(ct);

        foreach (var t in tokens)
            t.Revoke();
    }
}
