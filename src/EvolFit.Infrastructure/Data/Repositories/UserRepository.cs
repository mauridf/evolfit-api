using EvolFit.Application.Features.Auth.Interfaces;
using EvolFit.Core.Entities;
using EvolFit.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace EvolFit.Infrastructure.Data.Repositories;

public class UserRepository : IUserRepository
{
    private readonly EvolFitDbContext _context;

    public UserRepository(EvolFitDbContext context)
    {
        _context = context;
    }

    public Task<User?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _context.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return _context.Users.FirstOrDefaultAsync(u => u.Email == normalized, ct);
    }

    public Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default) =>
        _context.Users.FirstOrDefaultAsync(u => u.Username == username, ct);

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return _context.Users.AnyAsync(u => u.Email == normalized, ct);
    }

    public Task<bool> UsernameExistsAsync(string username, CancellationToken ct = default) =>
        _context.Users.AnyAsync(u => u.Username == username, ct);

    public async Task AddAsync(User user, CancellationToken ct = default) =>
        await _context.Users.AddAsync(user, ct);

    public void Update(User user) => _context.Users.Update(user);
}
