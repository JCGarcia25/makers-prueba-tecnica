using Makers.Application;
using Makers.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Makers.Infrastructure;

public sealed class UserStore(LoanDbContext db) : IUserStore
{
    public Task<User?> FindAsync(Guid id, CancellationToken ct) => db.Users.SingleOrDefaultAsync(u => u.Id == id, ct);
    public Task<User?> FindByEmailAsync(string email, CancellationToken ct) => db.Users.SingleOrDefaultAsync(u => u.Email == email, ct);

    public async Task<Page<UserView>> ListAsync(int page, int size, CancellationToken ct)
    {
        var total = await db.Users.CountAsync(ct);
        var users = await db.Users.AsNoTracking().OrderBy(u => u.Email).Skip((page - 1) * size).Take(size)
            .Select(u => new UserView(u.Id, u.Email, u.Name, u.Role, u.IsActive)).ToListAsync(ct);
        return new Page<UserView>(users, total, page, size);
    }

    public async Task SaveAsync(User user, bool create, CancellationToken ct)
    {
        if (create) db.Users.Add(user);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new BusinessException("El correo ya está registrado.", 409);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new BusinessException("El usuario cambió durante la operación.", 409);
        }
    }
}

public sealed class PasswordService : IPasswordService
{
    private readonly PasswordHasher<User> hasher = new();
    public string Hash(User user, string password) => hasher.HashPassword(user, password);
    public bool Verify(User user, string password) =>
        hasher.VerifyHashedPassword(user, user.PasswordHash, password) != PasswordVerificationResult.Failed;
}
