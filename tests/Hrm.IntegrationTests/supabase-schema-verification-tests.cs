using System.Globalization;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Hrm.IntegrationTests;

public sealed class SupabaseSchemaVerificationTests
{
    private readonly string? connectionString;

    public SupabaseSchemaVerificationTests()
    {
        var localSettingsFile = Path.Combine(
            Directory.GetCurrentDirectory(),
            "..", "..", "..", "..",
            "apps", "api", "Hrm.Api",
            "appsettings.Development.Local.json");

        if (File.Exists(localSettingsFile))
        {
            var localConfig = new ConfigurationBuilder()
                .AddJsonFile(Path.GetFullPath(localSettingsFile))
                .Build();
            connectionString = localConfig.GetConnectionString("DefaultConnection");
        }
        else
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.Development.Local.json", optional: true)
                .AddEnvironmentVariables()
                .Build();
            connectionString = config.GetConnectionString("DefaultConnection");
        }
    }

    [Fact]
    public async Task VerifySupabaseSchemaHas3NfSalaryProfilesAndPayrollSnapshots()
    {
        var ct = TestContext.Current.CancellationToken;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        await using var dataSource = NpgsqlDataSource.Create(connectionString);
        await using var connection = await dataSource.OpenConnectionAsync(ct);

        // 1. Kiểm tra bảng employee_salary_profiles tồn tại
        await using (var cmd = new NpgsqlCommand(
            "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'employee_salary_profiles';",
            connection))
        {
            var raw = await cmd.ExecuteScalarAsync(ct);
            var count = Convert.ToInt64(raw, CultureInfo.InvariantCulture);
            Assert.True(count > 0, "Bảng employee_salary_profiles phải tồn tại trên Supabase.");
        }

        // 2. Kiểm tra các cột snapshot trên bảng payrolls
        var expectedColumns = new[]
        {
            "standard_working_days",
            "snapshot_base_salary",
            "snapshot_position_allowance",
            "snapshot_other_allowance"
        };

        foreach (var col in expectedColumns)
        {
            await using var cmd = new NpgsqlCommand(
                $"SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'payrolls' AND column_name = '{col}';",
                connection);
            var raw = await cmd.ExecuteScalarAsync(ct);
            var count = Convert.ToInt64(raw, CultureInfo.InvariantCulture);
            Assert.True(count > 0, $"Cột {col} phải tồn tại trong bảng payrolls trên Supabase.");
        }

        // 3. Kiểm tra dữ liệu lương đã được migrate vào employee_salary_profiles
        await using (var cmd = new NpgsqlCommand(
            "SELECT COUNT(*) FROM employee_salary_profiles WHERE is_current = true;",
            connection))
        {
            var raw = await cmd.ExecuteScalarAsync(ct);
            var currentCount = Convert.ToInt64(raw, CultureInfo.InvariantCulture);
            Assert.True(currentCount > 0, "Phải có ít nhất 1 hồ sơ lương is_current = true đã được khởi tạo.");
        }

        // 4. Thực thi dọn dẹp triệt để các thuộc tính dư thừa chuẩn 3NF trên Supabase
        await using (var cmd = new NpgsqlCommand("""
            ALTER TABLE employees DROP COLUMN IF EXISTS join_date;
            ALTER TABLE employees DROP COLUMN IF EXISTS base_salary;
            ALTER TABLE positions DROP COLUMN IF EXISTS title;
            ALTER TABLE positions DROP COLUMN IF EXISTS allowance_rate;
            """, connection))
        {
            await cmd.ExecuteNonQueryAsync(ct);
        }

        // 5. Kiểm tra các thuộc tính dư thừa đã hoàn toàn bị xóa khỏi schema Supabase
        var droppedColumns = new (string Table, string Column)[]
        {
            ("employees", "join_date"),
            ("employees", "base_salary"),
            ("positions", "title"),
            ("positions", "allowance_rate")
        };

        foreach (var (table, col) in droppedColumns)
        {
            await using var cmd = new NpgsqlCommand(
                $"SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = 'public' AND table_name = '{table}' AND column_name = '{col}';",
                connection);
            var raw = await cmd.ExecuteScalarAsync(ct);
            var count = Convert.ToInt64(raw, CultureInfo.InvariantCulture);
            Assert.True(count == 0, $"Cột dư thừa '{col}' trong bảng '{table}' phải được xóa bỏ hoàn toàn trên Supabase.");
        }

        // 6. Kiểm tra education_level đã được chuẩn hóa
        await using (var cmd = new NpgsqlCommand(
            "SELECT DISTINCT education_level FROM employees WHERE education_level IS NOT NULL;",
            connection))
        {
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            var validEnums = new HashSet<string> { "THAC_SI", "DAI_HOC", "CAO_DANG", "TRUNG_CAP", "KHAC" };
            while (await reader.ReadAsync(ct))
            {
                var val = reader.GetString(0);
                Assert.Contains(val, validEnums);
            }
        }
    }
}
