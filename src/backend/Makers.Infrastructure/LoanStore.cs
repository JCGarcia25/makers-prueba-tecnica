using System.Data;
using Makers.Application;
using Makers.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Makers.Infrastructure;

// A shared gate prevents an in-flight read from repopulating a stale cache after a local write.
public sealed class LoanCache(IMemoryCache memory)
{
    public IMemoryCache Memory { get; } = memory;
    public SemaphoreSlim Gate { get; } = new(1, 1);
}

public sealed class LoanStore(LoanDbContext db, LoanCache cache, Microsoft.Extensions.Logging.ILogger<LoanStore> logger) : ILoanStore
{
    public Task<Loan?> FindAsync(Guid id, CancellationToken ct) =>
        db.Loans.Include(l => l.Decisions).SingleOrDefaultAsync(l => l.Id == id, ct);

    public async Task<LoanView?> ReadAsync(Guid id, CancellationToken ct)
    {
        await cache.Gate.WaitAsync(ct);
        try
        {
            if (cache.Memory.TryGetValue(id, out LoanView? cached)) return cached;
            var loan = await db.Loans.AsNoTracking().Include(l => l.Decisions).SingleOrDefaultAsync(l => l.Id == id, ct);
            if (loan is null) return null;
            var result = LoanView.From(loan);
            cache.Memory.Set(id, result, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30), Size = 1
            });
            return result;
        }
        finally { cache.Gate.Release(); }
    }

    public async Task<Page<LoanView>> ListAsync(Guid? ownerId, int page, int size, LoanStatus? status, CancellationToken ct)
    {
        var query = db.Loans.AsNoTracking();
        if (ownerId.HasValue) query = query.Where(l => l.UserId == ownerId.Value);
        if (status.HasValue) query = query.Where(l => l.Status == status.Value);
        var count = await query.CountAsync(ct);
        var loans = await query.Include(l => l.Decisions).OrderByDescending(l => l.CreatedAt).ThenBy(l => l.Id)
            .Skip((page - 1) * size).Take(size).ToListAsync(ct);
        return new Page<LoanView>(loans.Select(LoanView.From).ToList(), count, page, size);
    }

    public async Task AddAsync(Loan loan, CancellationToken ct)
    {
        db.Loans.Add(loan);
        await db.SaveChangesAsync(ct);
    }

    public async Task SaveAsync(Loan loan, bool delete, CancellationToken ct)
    {
        await cache.Gate.WaitAsync(ct);
        try
        {
            if (delete) db.Loans.Remove(loan);
            foreach (var decision in loan.Decisions.Where(decision => db.Entry(decision).State == EntityState.Detached))
                db.Decisions.Add(decision);
            if (db.Database.IsRelational())
            {
                await using var transaction = await db.Database.BeginTransactionAsync(ct);
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
            }
            else await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            logger.LogWarning(exception, "Conflicto de concurrencia al guardar el préstamo {LoanId}", loan.Id);
            throw new BusinessException("La solicitud cambió durante la operación. Actualiza e inténtalo de nuevo.", 409);
        }
        finally
        {
            cache.Memory.Remove(loan.Id);
            cache.Gate.Release();
        }
    }

    public async Task<AnalyticsSnapshot> ExportAsync(CancellationToken ct)
    {
        // RepeatableRead fixes the dataset while the extraction metadata and rows are produced.
        var extractedAt = DateTimeOffset.UtcNow;
        List<AnalyticsRow> rows;
        if (db.Database.IsRelational())
        {
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
            rows = await ReadAnalyticsRowsAsync(ct);
            await transaction.CommitAsync(ct);
        }
        else rows = await ReadAnalyticsRowsAsync(ct);
        return new AnalyticsSnapshot("1.0", "COP", extractedAt, "operational-api", rows);
    }

    private async Task<List<AnalyticsRow>> ReadAnalyticsRowsAsync(CancellationToken ct)
    {
        var loans = await db.Loans.AsNoTracking().Include(l => l.Decisions)
            .OrderBy(l => l.CreatedAt).ThenBy(l => l.Id).ToListAsync(ct);
        return loans.Select(l => new AnalyticsRow(l.Id, l.UserId, l.Amount, l.TermMonths, l.Status.ToString(),
            l.CreatedAt, l.Decisions.Select(d => (DateTimeOffset?)d.DecidedAt).FirstOrDefault())).ToList();
    }
}
