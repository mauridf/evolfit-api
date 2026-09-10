using EvolFit.Application.Common;
using EvolFit.Infrastructure.Data.Context;

namespace EvolFit.Infrastructure.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly EvolFitDbContext _context;

    public UnitOfWork(EvolFitDbContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        _context.SaveChangesAsync(ct);
}
