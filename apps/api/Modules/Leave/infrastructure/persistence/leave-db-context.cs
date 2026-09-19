using Hrm.Modules.Leave.Domain;
using Microsoft.EntityFrameworkCore;

namespace Hrm.Modules.Leave.Infrastructure.Persistence;

public sealed class LeaveDbContext(DbContextOptions<LeaveDbContext> options) : DbContext(options)
{
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LeaveRequest>(entity =>
        {
            entity.ToTable("leave_requests");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.EmployeeId).HasColumnName("emp_id").IsRequired();
            entity.Property(e => e.LeaveType).HasColumnName("leave_type").HasMaxLength(50).IsRequired();
            entity.Property(e => e.StartDate).HasColumnName("start_date").IsRequired();
            entity.Property(e => e.EndDate).HasColumnName("end_date").IsRequired();
            entity.Property(e => e.DaysCount).HasColumnName("days_count").HasPrecision(4, 1).IsRequired();
            entity.Property(e => e.Reason).HasColumnName("reason").IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(50).IsRequired();
            entity.Property(e => e.RejectionReason).HasColumnName("rejection_reason");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasIndex(e => e.EmployeeId).HasDatabaseName("ix_leave_requests_emp_id");
            entity.HasIndex(e => new { e.EmployeeId, e.StartDate, e.EndDate }).HasDatabaseName("ix_leave_requests_emp_dates");
        });
    }
}
