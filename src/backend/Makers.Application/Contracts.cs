using Makers.Domain;

namespace Makers.Application;

public sealed record LoanInput(decimal Amount, int TermMonths);
public sealed record DecisionInput(string Status, string Reason);
public sealed record DecisionView(Guid AdministratorId, string Status, string Reason, DateTimeOffset DecidedAt);
public sealed record LoanView(Guid Id, Guid UserId, decimal Amount, int TermMonths, string Status,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, Guid Version, IReadOnlyList<DecisionView> Decisions)
{
    public static LoanView From(Loan loan) => new(loan.Id, loan.UserId, loan.Amount, loan.TermMonths,
        loan.Status.ToString(), loan.CreatedAt, loan.UpdatedAt, loan.Version,
        loan.Decisions.OrderBy(d => d.DecidedAt).Select(d => new DecisionView(
            d.AdministratorId, d.Status.ToString(), d.Reason, d.DecidedAt)).ToList());
}
public sealed record Page<T>(IReadOnlyList<T> Items, int Total, int PageNumber, int PageSize);
public sealed record UserView(Guid Id, string Email, string Name, string Role, bool IsActive);
public sealed record UserInput(string Email, string Name, string Password);
public sealed record UserUpdate(string Name, string Email);
public sealed record AnalyticsRow(Guid LoanId, Guid ApplicantId, decimal Amount, int TermMonths,
    string Status, DateTimeOffset CreatedAt, DateTimeOffset? DecidedAt);
public sealed record AnalyticsSnapshot(string SchemaVersion, string Currency, DateTimeOffset ExtractedAt,
    string Source, IReadOnlyList<AnalyticsRow> Loans);

public interface ILoanStore
{
    Task<Loan?> FindAsync(Guid id, CancellationToken ct);
    Task<LoanView?> ReadAsync(Guid id, CancellationToken ct);
    Task<Page<LoanView>> ListAsync(Guid? ownerId, int page, int size, LoanStatus? status, CancellationToken ct);
    Task AddAsync(Loan loan, CancellationToken ct);
    Task SaveAsync(Loan loan, bool delete, CancellationToken ct);
    Task<AnalyticsSnapshot> ExportAsync(CancellationToken ct);
}

public interface IUserStore
{
    Task<User?> FindAsync(Guid id, CancellationToken ct);
    Task<User?> FindByEmailAsync(string email, CancellationToken ct);
    Task<Page<UserView>> ListAsync(int page, int size, CancellationToken ct);
    Task SaveAsync(User user, bool create, CancellationToken ct);
}

public interface IPasswordService
{
    string Hash(User user, string password);
    bool Verify(User user, string password);
}
