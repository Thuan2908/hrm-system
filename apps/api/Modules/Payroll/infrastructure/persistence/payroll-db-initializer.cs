using Hrm.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Hrm.Modules.Payroll.Infrastructure.Persistence;

public interface IPayrollDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken);
}

public sealed class PayrollDatabaseInitializer(PayrollDbContext dbContext) : IPayrollDatabaseInitializer
{
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(7246012029)", cancellationToken);

        // 1. Xóa bảng payslips theo yêu cầu để tránh dư thừa dữ liệu
        await dbContext.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS payslips CASCADE;", cancellationToken);

        // 2. Đảm bảo index trên bảng payrolls có sẵn
        await dbContext.Database.ExecuteSqlRawAsync("""
            CREATE INDEX IF NOT EXISTS ix_payrolls_emp_id
                ON payrolls(emp_id);
            """, cancellationToken);



        // Đồng bộ sequence cho identity column payroll_id để tránh lỗi duplicate key pk_payrolls
        const string syncSequenceSql = """
            DO $$
            DECLARE
                max_id bigint;
                seq_name text;
            BEGIN
                IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'payrolls') THEN
                    SELECT COALESCE(MAX(payroll_id), 0) INTO max_id FROM payrolls;
                    seq_name := pg_get_serial_sequence('payrolls', 'payroll_id');
                    IF seq_name IS NOT NULL THEN
                        IF max_id > 0 THEN
                            PERFORM setval(seq_name, max_id, true);
                        ELSE
                            PERFORM setval(seq_name, 1, false);
                        END IF;
                    END IF;
                END IF;
            END $$;
            """;

        await dbContext.Database.ExecuteSqlRawAsync(syncSequenceSql, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }
}
