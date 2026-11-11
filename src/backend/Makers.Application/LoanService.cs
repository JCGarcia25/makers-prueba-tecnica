using Makers.Domain;

namespace Makers.Application;

public sealed class LoanService(ILoanStore store)
{
    public async Task<LoanView> CreateAsync(Guid ownerId, LoanInput input, CancellationToken ct)
    {
        var loan = Loan.Create(ownerId, input.Amount, input.TermMonths, DateTimeOffset.UtcNow);
        await store.AddAsync(loan, ct);
        return LoanView.From(loan);
    }

    public async Task<LoanView> ReadAsync(Guid id, Guid userId, bool isAdmin, CancellationToken ct)
    {
        var loan = await store.ReadAsync(id, ct) ?? throw new BusinessException("Préstamo no encontrado.", 404);
        CheckOwner(loan.UserId, userId, isAdmin);
        return loan;
    }

    public async Task<LoanView> UpdateAsync(Guid id, Guid userId, bool isAdmin, LoanInput input, CancellationToken ct)
    {
        var loan = await GetOwnedAsync(id, userId, isAdmin, ct);
        loan.Update(input.Amount, input.TermMonths, DateTimeOffset.UtcNow);
        await store.SaveAsync(loan, false, ct);
        return LoanView.From(loan);
    }

    public async Task DeleteAsync(Guid id, Guid userId, bool isAdmin, CancellationToken ct)
    {
        var loan = await GetOwnedAsync(id, userId, isAdmin, ct);
        loan.EnsurePending();
        await store.SaveAsync(loan, true, ct);
    }

    public async Task<LoanView> DecideAsync(Guid id, Guid administratorId, DecisionInput input, CancellationToken ct)
    {
        var loan = await store.FindAsync(id, ct) ?? throw new BusinessException("Préstamo no encontrado.", 404);
        if (!Enum.TryParse<LoanStatus>(input.Status, false, out var status))
            throw new BusinessException("La decisión debe ser Approved o Rejected.");
        loan.Decide(status, administratorId, input.Reason, DateTimeOffset.UtcNow);
        await store.SaveAsync(loan, false, ct);
        return LoanView.From(loan);
    }

    private async Task<Loan> GetOwnedAsync(Guid id, Guid userId, bool isAdmin, CancellationToken ct)
    {
        var loan = await store.FindAsync(id, ct) ?? throw new BusinessException("Préstamo no encontrado.", 404);
        CheckOwner(loan.UserId, userId, isAdmin);
        return loan;
    }

    private static void CheckOwner(Guid ownerId, Guid userId, bool isAdmin)
    {
        if (!isAdmin && ownerId != userId) throw new BusinessException("Préstamo no encontrado.", 404);
    }
}
