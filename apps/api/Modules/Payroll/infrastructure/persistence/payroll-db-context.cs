using Hrm.Modules.Payroll.Domain;
using Microsoft.EntityFrameworkCore;

namespace Hrm.Modules.Payroll.Infrastructure.Persistence;

public sealed class PayrollDbContext(DbContextOptions<PayrollDbContext> options) : DbContext(options)
{
    public DbSet<PayrollRecord> Payrolls => Set<PayrollRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PayrollRecord>(entity =>
        {
            entity.ToTable("payrolls");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("payroll_id");
            entity.Property(e => e.EmployeeId).HasColumnName("emp_id").IsRequired();
            entity.Property(e => e.MonthPeriod).HasColumnName("month_period").IsRequired();
            entity.Property(e => e.YearPeriod).HasColumnName("year_period").IsRequired();
            entity.Property(e => e.ActualDays).HasColumnName("actual_days").HasPrecision(5, 2).IsRequired();
            entity.Property(e => e.GrossSalary).HasColumnName("gross_sal").HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.BhxhDeduct).HasColumnName("bhxh_deduct").HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.TaxDeduct).HasColumnName("tax_deduct").HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.NetSalary).HasColumnName("net_sal").HasPrecision(18, 2).IsRequired();

            entity.HasIndex(e => e.EmployeeId).HasDatabaseName("ix_payrolls_emp_id");
        });
    }
}
