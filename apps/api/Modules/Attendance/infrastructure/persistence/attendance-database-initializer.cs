using Hrm.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Hrm.Modules.Attendance.Infrastructure.Persistence;

public interface IAttendanceDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken);
}

public sealed class AttendanceDatabaseInitializer(AttendanceDbContext dbContext) : IAttendanceDatabaseInitializer
{
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(7246012027)", cancellationToken);

        await dbContext.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS time_attendances (
                id uuid PRIMARY KEY,
                emp_id bigint NOT NULL,
                work_date date NOT NULL,
                check_in_time timestamp with time zone NOT NULL,
                check_out_time timestamp with time zone NULL,
                actual_hours numeric(5, 2) NULL,
                status varchar(50) NOT NULL DEFAULT 'PRESENT',
                notes text NULL,
                created_at timestamp with time zone NOT NULL,
                updated_at timestamp with time zone NULL
            );

            CREATE UNIQUE INDEX IF NOT EXISTS ix_time_attendances_emp_date
                ON time_attendances(emp_id, work_date);

            CREATE INDEX IF NOT EXISTS ix_time_attendances_work_date
                ON time_attendances(work_date);

            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM pg_constraint WHERE conname = 'fk_timeattendances_employees'
                ) THEN
                    ALTER TABLE time_attendances
                    ADD CONSTRAINT fk_timeattendances_employees
                    FOREIGN KEY (emp_id) REFERENCES employees(emp_id) ON DELETE RESTRICT;
                END IF;
            END $$;
            """, cancellationToken);



        await transaction.CommitAsync(cancellationToken);
    }
}
