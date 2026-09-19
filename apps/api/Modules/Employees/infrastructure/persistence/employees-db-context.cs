using Hrm.Modules.Employees.Domain;
using Microsoft.EntityFrameworkCore;

namespace Hrm.Modules.Employees.Infrastructure.Persistence;

public sealed class EmployeesDbContext(DbContextOptions<EmployeesDbContext> options) : DbContext(options)
{
    public DbSet<EmployeeEntity> Employees => Set<EmployeeEntity>();
    public DbSet<DepartmentEntity> Departments => Set<DepartmentEntity>();
    public DbSet<PositionEntity> Positions => Set<PositionEntity>();
    public DbSet<EmployeeTimelineEvent> TimelineEvents => Set<EmployeeTimelineEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EmployeeEntity>(entity =>
        {
            entity.ToTable("employees");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("emp_id").ValueGeneratedOnAdd();
            entity.Property(e => e.Code).HasColumnName("emp_code");
            entity.Property(e => e.FullName).HasColumnName("full_name");
            entity.Property(e => e.DepartmentId).HasColumnName("dept_id");
            entity.Property(e => e.PositionId).HasColumnName("position_id");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.Phone).HasColumnName("phone");
            entity.Property(e => e.Address).HasColumnName("address");
            entity.Property(e => e.HireDate).HasColumnName("hire_date").HasColumnType("date");
            entity.Property(e => e.JoinDate).HasColumnName("join_date").HasColumnType("date");
            entity.Property(e => e.BaseSalary).HasColumnName("base_salary");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasOne(e => e.Department).WithMany().HasForeignKey(e => e.DepartmentId);
            entity.HasOne(e => e.Position).WithMany().HasForeignKey(e => e.PositionId);
        });

        modelBuilder.Entity<DepartmentEntity>(entity =>
        {
            entity.ToTable("departments");
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Id).HasColumnName("dept_id").ValueGeneratedOnAdd();
            entity.Property(d => d.Code).HasColumnName("dept_code");
            entity.Property(d => d.Name).HasColumnName("dept_name");
            entity.Property(d => d.Description).HasColumnName("description");
            entity.Property(d => d.IsActive).HasColumnName("is_active");
            entity.Property(d => d.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<PositionEntity>(entity =>
        {
            entity.ToTable("positions");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Id).HasColumnName("position_id").ValueGeneratedOnAdd();
            entity.Property(p => p.Code).HasColumnName("position_code");
            entity.Property(p => p.Title).HasColumnName("title");
            entity.Property(p => p.BaseSalary).HasColumnName("base_salary");
            entity.Property(p => p.Allowance).HasColumnName("allowance");
            entity.Property(p => p.Description).HasColumnName("description");
            entity.Property(p => p.IsActive).HasColumnName("is_active");
            entity.Property(p => p.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<EmployeeTimelineEvent>(entity =>
        {
            entity.ToTable("employee_timeline_events");
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(t => t.EmployeeId).HasColumnName("emp_id");
            entity.Property(t => t.EventType).HasColumnName("event_type");
            entity.Property(t => t.Title).HasColumnName("title");
            entity.Property(t => t.Description).HasColumnName("description");
            entity.Property(t => t.Timestamp).HasColumnName("timestamp");
            entity.Property(t => t.CreatedByUserId).HasColumnName("created_by_user_id");
            entity.Property(t => t.CreatedByName).HasColumnName("created_by_name");
        });
    }
}
