using Makers.Domain;
using Microsoft.EntityFrameworkCore;

namespace Makers.Infrastructure;

public sealed class LoanDbContext(DbContextOptions<LoanDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Loan> Loans => Set<Loan>();
    public DbSet<LoanDecision> Decisions => Set<LoanDecision>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Email).HasMaxLength(254);
            entity.Property(u => u.Name).HasMaxLength(100);
            entity.Property(u => u.Role).HasMaxLength(10);
            entity.Property(u => u.SecurityVersion).IsConcurrencyToken();
            entity.ToTable("Users", t => t.HasCheckConstraint("CK_User_Role", "\"Role\" IN ('User','Admin')"));
        });
        model.Entity<Loan>(entity =>
        {
            entity.HasKey(l => l.Id);
            entity.Property(l => l.Amount).HasPrecision(18, 2);
            entity.Property(l => l.Status).HasConversion<string>().HasMaxLength(10);
            entity.Property(l => l.Version).IsConcurrencyToken();
            entity.HasIndex(l => new { l.UserId, l.CreatedAt });
            entity.HasIndex(l => new { l.Status, l.CreatedAt });
            entity.HasOne<User>().WithMany().HasForeignKey(l => l.UserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(l => l.Decisions).WithOne().HasForeignKey(d => d.LoanId).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable("Loans", t =>
            {
                t.HasCheckConstraint("CK_Loan_Amount", "\"Amount\" > 0 AND \"Amount\" <= 1000000000");
                t.HasCheckConstraint("CK_Loan_Term", "\"TermMonths\" BETWEEN 1 AND 120");
                t.HasCheckConstraint("CK_Loan_Status", "\"Status\" IN ('Pending','Approved','Rejected')");
                t.HasCheckConstraint("CK_Loan_Dates", "\"UpdatedAt\" >= \"CreatedAt\"");
            });
        });
        model.Entity<LoanDecision>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.HasIndex(d => d.LoanId).IsUnique();
            entity.Property(d => d.Status).HasConversion<string>().HasMaxLength(10);
            entity.Property(d => d.Reason).HasMaxLength(500);
            entity.HasOne<User>().WithMany().HasForeignKey(d => d.AdministratorId).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable("Decisions", t => t.HasCheckConstraint("CK_Decision_Status", "\"Status\" IN ('Approved','Rejected')"));
        });
    }
}
