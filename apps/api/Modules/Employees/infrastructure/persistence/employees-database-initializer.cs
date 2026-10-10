using Microsoft.EntityFrameworkCore;

namespace Hrm.Modules.Employees.Infrastructure.Persistence;

public interface IEmployeesDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken);
}

public sealed class EmployeesDatabaseInitializer(EmployeesDbContext dbContext) : IEmployeesDatabaseInitializer
{
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(7246012031)", cancellationToken);

        await dbContext.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS employee_timeline_events (
                id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                emp_id bigint NOT NULL,
                event_type varchar(100) NOT NULL,
                title varchar(255) NOT NULL,
                description text NOT NULL,
                timestamp timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                created_by_user_id bigint NULL,
                created_by_name varchar(255) NULL
            );

            CREATE INDEX IF NOT EXISTS ix_employee_timeline_events_emp_id
                ON employee_timeline_events(emp_id);

            DO $$
            BEGIN
                -- FK: employee_timeline_events -> employees
                IF NOT EXISTS (
                    SELECT 1 FROM pg_constraint WHERE conname = 'fk_employeetimelineevents_employees'
                ) THEN
                    ALTER TABLE employee_timeline_events
                    ADD CONSTRAINT fk_employeetimelineevents_employees
                    FOREIGN KEY (emp_id) REFERENCES employees(emp_id) ON DELETE CASCADE;
                END IF;

                -- FK: employee_timeline_events -> users
                IF NOT EXISTS (
                    SELECT 1 FROM pg_constraint WHERE conname = 'fk_employeetimelineevents_users'
                ) THEN
                    ALTER TABLE employee_timeline_events
                    ADD CONSTRAINT fk_employeetimelineevents_users
                    FOREIGN KEY (created_by_user_id) REFERENCES users(user_id) ON DELETE SET NULL;
                END IF;
            END $$;
            """, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }
}
