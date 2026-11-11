namespace Makers.Domain;

public enum LoanStatus { Pending, Approved, Rejected }

public sealed class BusinessException(string message, int statusCode = 400) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public sealed class Loan
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public decimal Amount { get; private set; }
    public int TermMonths { get; private set; }
    public LoanStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid Version { get; private set; }
    public List<LoanDecision> Decisions { get; private set; } = [];

    private Loan() { }

    public static Loan Create(Guid userId, decimal amount, int termMonths, DateTimeOffset now)
    {
        Validate(amount, termMonths);
        if (userId == Guid.Empty) throw new BusinessException("El usuario es obligatorio.");
        return new Loan
        {
            Id = Guid.NewGuid(), UserId = userId, Amount = amount, TermMonths = termMonths,
            Status = LoanStatus.Pending, CreatedAt = now, UpdatedAt = now, Version = Guid.NewGuid()
        };
    }

    public void Update(decimal amount, int termMonths, DateTimeOffset now)
    {
        EnsurePending();
        Validate(amount, termMonths);
        Amount = amount;
        TermMonths = termMonths;
        UpdatedAt = now;
        Version = Guid.NewGuid();
    }

    public void Decide(LoanStatus status, Guid administratorId, string reason, DateTimeOffset now)
    {
        EnsurePending();
        if (status is not (LoanStatus.Approved or LoanStatus.Rejected))
            throw new BusinessException("La decisión debe ser Approved o Rejected.");
        if (administratorId == Guid.Empty) throw new BusinessException("El administrador es obligatorio.");
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 500)
            throw new BusinessException("El motivo es obligatorio y admite hasta 500 caracteres.");
        Status = status;
        UpdatedAt = now;
        Version = Guid.NewGuid();
        Decisions.Add(new LoanDecision(Guid.NewGuid(), Id, administratorId, status, reason.Trim(), now));
    }

    public void EnsurePending()
    {
        if (Status != LoanStatus.Pending)
            throw new BusinessException("Solo se pueden modificar o eliminar solicitudes pendientes.", 409);
    }

    private static void Validate(decimal amount, int termMonths)
    {
        if (amount <= 0 || amount > 1_000_000_000m || decimal.Round(amount, 2) != amount)
            throw new BusinessException("El monto debe ser positivo, máximo 1.000.000.000 COP y tener hasta dos decimales.");
        if (termMonths is < 1 or > 120)
            throw new BusinessException("El plazo debe estar entre 1 y 120 meses.");
    }
}

public sealed record LoanDecision(Guid Id, Guid LoanId, Guid AdministratorId, LoanStatus Status,
    string Reason, DateTimeOffset DecidedAt);
