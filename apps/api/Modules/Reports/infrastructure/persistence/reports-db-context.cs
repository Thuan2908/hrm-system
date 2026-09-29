using Microsoft.EntityFrameworkCore;

namespace Hrm.Modules.Reports.Infrastructure.Persistence;

public sealed class ReportEmployee
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public long? DepartmentId { get; set; }
    public ReportDepartment? Department { get; set; }
    public string? EducationLevel { get; set; }
    public decimal? BaseSalary { get; set; }
    public DateOnly? JoinDate { get; set; }
    public DateOnly? HireDate { get; set; }
    public string Status { get; set; } = "WORKING";
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class ReportDepartment
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public sealed class ReportAttendance
{
    public Guid Id { get; set; }
    public long EmployeeId { get; set; }
    public DateOnly WorkDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal ActualHours { get; set; }
}

public sealed class ReportLeave
{
    public Guid Id { get; set; }
    public long EmployeeId { get; set; }
    public string LeaveType { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal DaysCount { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class ReportPayroll
{
    public Guid Id { get; set; }
    public long EmployeeId { get; set; }
    public int MonthPeriod { get; set; }
    public int YearPeriod { get; set; }
    public decimal GrossSalary { get; set; }
    public decimal NetSalary { get; set; }
    public decimal BhxhDeduct { get; set; }
    public decimal TaxDeduct { get; set; }
}

public sealed class ReportsDbContext(DbContextOptions<ReportsDbContext> options) : DbContext(options)
{
    public DbSet<ReportEmployee> Employees => Set<ReportEmployee>();
    public DbSet<ReportDepartment> Departments => Set<ReportDepartment>();
    public DbSet<ReportAttendance> Attendances => Set<ReportAttendance>();
    public DbSet<ReportLeave> Leaves => Set<ReportLeave>();
    public DbSet<ReportPayroll> Payrolls => Set<ReportPayroll>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ReportDepartment>(entity =>
        {
            entity.ToTable("departments");
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Id).HasColumnName("dept_id");
            entity.Property(d => d.Code).HasColumnName("dept_code");
            entity.Property(d => d.Name).HasColumnName("dept_name");
        });

        modelBuilder.Entity<ReportEmployee>(entity =>
        {
            entity.ToTable("employees");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("emp_id");
            entity.Property(e => e.Code).HasColumnName("emp_code");
            entity.Property(e => e.FullName).HasColumnName("full_name");
            entity.Property(e => e.DepartmentId).HasColumnName("dept_id");
            entity.Property(e => e.EducationLevel).HasColumnName("education_level");
            entity.Property(e => e.BaseSalary).HasColumnName("base_salary");
            entity.Property(e => e.JoinDate).HasColumnName("join_date");
            entity.Property(e => e.HireDate).HasColumnName("hire_date");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasOne(e => e.Department)
                .WithMany()
                .HasForeignKey(e => e.DepartmentId)
                .IsRequired(false);
        });

        modelBuilder.Entity<ReportAttendance>(entity =>
        {
            entity.ToTable("time_attendances");
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Id).HasColumnName("id");
            entity.Property(a => a.EmployeeId).HasColumnName("emp_id");
            entity.Property(a => a.WorkDate).HasColumnName("work_date");
            entity.Property(a => a.Status).HasColumnName("status");
            entity.Property(a => a.ActualHours).HasColumnName("actual_hours");
        });

        modelBuilder.Entity<ReportLeave>(entity =>
        {
            entity.ToTable("leave_requests");
            entity.HasKey(l => l.Id);
            entity.Property(l => l.Id).HasColumnName("id");
            entity.Property(l => l.EmployeeId).HasColumnName("emp_id");
            entity.Property(l => l.LeaveType).HasColumnName("leave_type");
            entity.Property(l => l.StartDate).HasColumnName("start_date");
            entity.Property(l => l.EndDate).HasColumnName("end_date");
            entity.Property(l => l.DaysCount).HasColumnName("days_count");
            entity.Property(l => l.Status).HasColumnName("status");
        });

        modelBuilder.Entity<ReportPayroll>(entity =>
        {
            entity.ToTable("payrolls");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Id).HasColumnName("payroll_id");
            entity.Property(p => p.EmployeeId).HasColumnName("emp_id");
            entity.Property(p => p.MonthPeriod).HasColumnName("month_period");
            entity.Property(p => p.YearPeriod).HasColumnName("year_period");
            entity.Property(p => p.GrossSalary).HasColumnName("gross_sal");
            entity.Property(p => p.NetSalary).HasColumnName("net_sal");
            entity.Property(p => p.BhxhDeduct).HasColumnName("bhxh_deduct");
            entity.Property(p => p.TaxDeduct).HasColumnName("tax_deduct");
        });
    }
}
