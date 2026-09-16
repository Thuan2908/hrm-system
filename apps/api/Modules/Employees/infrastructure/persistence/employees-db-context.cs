using Hrm.Modules.Employees.Domain;
using Microsoft.EntityFrameworkCore;

namespace Hrm.Modules.Employees.Infrastructure.Persistence;

public sealed class EmployeesDbContext(DbContextOptions<EmployeesDbContext> options) : DbContext(options)
{
    public DbSet<EmployeeLifecycleRecord> Employees => Set<EmployeeLifecycleRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EmployeeLifecycleRecord>(entity =>
        {
            entity.ToTable("employees");
            entity.HasKey(employee => employee.Id);
            entity.Property(employee => employee.Id).HasColumnName("emp_id").ValueGeneratedNever();
            entity.Property(employee => employee.Status).HasColumnName("status");
        });
    }
}
