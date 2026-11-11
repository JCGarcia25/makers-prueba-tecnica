using Makers.Application;
using Makers.Domain;
using Microsoft.EntityFrameworkCore;

namespace Makers.Infrastructure;

public static class DemoSeed
{
    public static async Task RunAsync(LoanDbContext db, IPasswordService passwords, string password, CancellationToken ct = default)
    {
        if (password.Length < 10) throw new InvalidOperationException("Seed:Password debe tener al menos 10 caracteres.");
        if (await db.Users.AnyAsync(ct)) return;
        var admin = new User { Email = "admin@test.com", Name = "Administrador demo", Role = "Admin" };
        var user = new User { Email = "usuario@test.com", Name = "Usuario demo" };
        var other = new User { Email = "otro@test.com", Name = "Otro usuario demo" };
        foreach (var account in new[] { admin, user, other }) account.PasswordHash = passwords.Hash(account, password);
        db.Users.AddRange(admin, user, other);
        var now = DateTimeOffset.UtcNow;
        for (var i = 0; i < 36; i++)
        {
            var created = now.AddDays(-90 + i * 2);
            var loan = Loan.Create(i % 2 == 0 ? user.Id : other.Id, 500_000m + i * 275_000m, 6 + i % 7 * 6, created);
            if (i % 3 != 0)
                loan.Decide(i % 3 == 1 ? LoanStatus.Approved : LoanStatus.Rejected, admin.Id,
                    "Decisión de ejemplo sobre datos sintéticos.", created.AddHours(2 + i));
            db.Loans.Add(loan);
        }
        await db.SaveChangesAsync(ct);
    }
}
