using Makers.Domain;
using Xunit;

namespace Makers.Domain.Tests;

public sealed class LoanTests
{
    [Fact]
    public void Create_RejectsAmountWithMoreThanTwoDecimals()
    {
        var error = Assert.Throws<BusinessException>(() => Loan.Create(Guid.NewGuid(), 1000.001m, 12, DateTimeOffset.UtcNow));
        Assert.Contains("dos decimales", error.Message);
    }

    [Fact]
    public void Decide_RecordsAuditDataAndPreventsSecondDecision()
    {
        var createdAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var administratorId = Guid.NewGuid();
        var loan = Loan.Create(Guid.NewGuid(), 2_000_000m, 24, createdAt);

        loan.Decide(LoanStatus.Approved, administratorId, "Capacidad verificada", createdAt.AddHours(4));

        Assert.Equal(LoanStatus.Approved, loan.Status);
        var decision = Assert.Single(loan.Decisions);
        Assert.Equal(administratorId, decision.AdministratorId);
        Assert.Equal("Capacidad verificada", decision.Reason);
        var error = Assert.Throws<BusinessException>(() =>
            loan.Decide(LoanStatus.Rejected, administratorId, "Cambio", createdAt.AddHours(5)));
        Assert.Equal(409, error.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(121)]
    public void Create_RejectsTermOutsideAllowedRange(int months)
    {
        Assert.Throws<BusinessException>(() => Loan.Create(Guid.NewGuid(), 1_000_000m, months, DateTimeOffset.UtcNow));
    }
}
