using Hrm.Modules.Attendance.Domain;
using Microsoft.EntityFrameworkCore;

namespace Hrm.Modules.Attendance.Infrastructure.Persistence;

public sealed class AttendanceDbContext(DbContextOptions<AttendanceDbContext> options) : DbContext(options)
{
    public DbSet<TimeAttendance> TimeAttendances => Set<TimeAttendance>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TimeAttendance>(entity =>
        {
            entity.ToTable("time_attendances");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.EmployeeId).HasColumnName("emp_id");
            entity.Property(e => e.WorkDate).HasColumnName("work_date");
            entity.Property(e => e.CheckInTime).HasColumnName("check_in_time");
            entity.Property(e => e.CheckOutTime).HasColumnName("check_out_time");
            entity.Property(e => e.ActualHours).HasColumnName("actual_hours").HasPrecision(5, 2);
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(50);
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasIndex(e => new { e.EmployeeId, e.WorkDate }).IsUnique();
            entity.HasIndex(e => e.WorkDate);
        });
    }
}
