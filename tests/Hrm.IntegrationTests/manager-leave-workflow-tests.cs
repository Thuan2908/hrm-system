using Hrm.Contracts;
using Hrm.Modules.Auth.Application;
using Hrm.Modules.Auth.Contracts;
using Hrm.Modules.Auth.Infrastructure.Persistence;
using Hrm.Modules.Leave.Application;
using Hrm.Modules.Leave.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Hrm.IntegrationTests;

public sealed class ManagerLeaveWorkflowTests
{
    [Fact]
    public async Task AutomaticApprovalPersistsDecisionAndAuditButResignationStaysPending()
    {
        await using var fixture = await Fixture.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var ordinary = await fixture.Service.CreateLeaveRequestAsync(2, Request(LeaveTypes.Annual), ct);
        Assert.Equal(LeaveStatuses.Approved, ordinary.Status);
        Assert.True(ordinary.IsAutoApproved);
        Assert.Equal(2, ordinary.ApprovedByUserId);
        Assert.NotNull(ordinary.ApprovedAt);
        Assert.Equal(1, await fixture.Auth.AuditLogs.CountAsync(a => a.Action == "leave.auto-approved", ct));

        var resignation = await fixture.Service.CreateLeaveRequestAsync(2,
            Request(LeaveTypes.Resignation) with { StartDate = new DateOnly(2027, 1, 12), EndDate = new DateOnly(2027, 1, 12) }, ct);
        Assert.Equal(LeaveStatuses.Pending, resignation.Status);
        Assert.False(resignation.IsAutoApproved);
        Assert.Null(resignation.ApprovedByUserId);
        Assert.Equal("ACTIVE", (await fixture.Auth.Employees.SingleAsync(e => e.Id == 20, ct)).Status);
        Assert.True((await fixture.Auth.Users.SingleAsync(u => u.Id == 2, ct)).IsActive);
    }

    [Fact]
    public async Task OnlyCurrentManagerWithPermissionCanListApproveOrReject()
    {
        await using var fixture = await Fixture.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        await fixture.ExecuteAsync("DELETE FROM role_permissions WHERE role_id = 2");
        var request = await fixture.Service.CreateLeaveRequestAsync(2, Request(LeaveTypes.Annual), ct);
        Assert.Equal(LeaveStatuses.Pending, request.Status);
        Assert.Single(await fixture.Service.GetPendingRequestsAsync(1, ct));
        Assert.Empty(await fixture.Service.GetPendingRequestsAsync(3, ct));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => fixture.Service.ApproveLeaveRequestAsync(request.Id, 3, ct));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => fixture.Service.RejectLeaveRequestAsync(request.Id, "No", 3, ct));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => fixture.Service.ApproveLeaveRequestAsync(request.Id, 2, ct));

        // Permission revocation takes effect without waiting for a token refresh.
        await fixture.ExecuteAsync("DELETE FROM role_permissions WHERE role_id = 1");
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => fixture.Service.GetPendingRequestsAsync(1, ct));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => fixture.Service.ApproveLeaveRequestAsync(request.Id, 1, ct));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => fixture.Service.RejectLeaveRequestAsync(request.Id, "No", 1, ct));

        await fixture.ExecuteAsync("INSERT INTO role_permissions VALUES (1, 1); UPDATE employees SET manager_id = 30 WHERE emp_id = 20");
        Assert.Empty(await fixture.Service.GetPendingRequestsAsync(1, ct));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => fixture.Service.ApproveLeaveRequestAsync(request.Id, 1, ct));
        Assert.Single(await fixture.Service.GetPendingRequestsAsync(3, ct));
        await fixture.Service.ApproveLeaveRequestAsync(request.Id, 3, ct);
        var approved = Assert.Single(await fixture.Service.GetMyRequestsAsync(2, ct));
        Assert.Equal(LeaveStatuses.Approved, approved.Status);
        Assert.Equal(3, approved.ApprovedByUserId);
        Assert.False(approved.IsAutoApproved);
    }

    [Fact]
    public async Task MissingManagerAndInactiveManagerDoNotAllowReview()
    {
        await using var fixture = await Fixture.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        await fixture.ExecuteAsync("DELETE FROM role_permissions WHERE role_id = 2; UPDATE employees SET manager_id = NULL WHERE emp_id = 20");
        var request = await fixture.Service.CreateLeaveRequestAsync(2, Request(LeaveTypes.Sick), ct);
        Assert.Empty(await fixture.Service.GetPendingRequestsAsync(1, ct));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => fixture.Service.RejectLeaveRequestAsync(request.Id, "No", 1, ct));
        await fixture.ExecuteAsync("UPDATE employees SET manager_id = 10 WHERE emp_id = 20; UPDATE users SET is_active = false WHERE user_id = 1");
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => fixture.Service.ApproveLeaveRequestAsync(request.Id, 1, ct));
    }

    [Fact]
    public async Task GlobalOnlyApproverCanFilterSortAndReviewAcrossDepartmentsButNotSelf()
    {
        await using var fixture = await Fixture.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        await fixture.ExecuteAsync("""
            INSERT INTO permissions VALUES (3, 'leave.approve.all', 'Global');
            DELETE FROM role_permissions WHERE role_id IN (2, 3);
            INSERT INTO role_permissions VALUES (3, 3);
            INSERT INTO departments VALUES (2, 'OTHER', 'Other Department');
            UPDATE employees SET dept_id = 2 WHERE emp_id = 20;
            """);
        var employeeRequest = await fixture.Service.CreateLeaveRequestAsync(2, Request(LeaveTypes.Annual), ct);
        var managerRequest = await fixture.Service.CreateLeaveRequestAsync(1, Request(LeaveTypes.Annual), ct);
        var ownRequest = await fixture.Service.CreateLeaveRequestAsync(3, Request(LeaveTypes.Annual), ct);
        var options = await fixture.Service.GetApprovalOptionsAsync(3, ct);
        Assert.True(options.CanApproveAll);
        Assert.Equal(2, options.Departments.Count);
        Assert.Equal(2, (await fixture.Service.GetPendingRequestsAsync(3, ct)).Count);
        var departmentRows = await fixture.Service.GetPendingRequestsAsync(3, 2, "employeeName", false, ct);
        Assert.Equal(employeeRequest.Id, Assert.Single(departmentRows).Id);
        var sorted = await fixture.Service.GetPendingRequestsAsync(3, null, "employeeName", false, ct);
        Assert.Collection(sorted, r => Assert.Equal("Employee", r.EmployeeName), r => Assert.Equal("Manager", r.EmployeeName));
        var reverse = await fixture.Service.GetPendingRequestsAsync(3, null, "employeeName", true, ct);
        Assert.Collection(reverse, r => Assert.Equal("Manager", r.EmployeeName), r => Assert.Equal("Employee", r.EmployeeName));
        // Filters must never broaden the direct manager's scope.
        Assert.Empty(await fixture.Service.GetPendingRequestsAsync(1, 1, "createdAt", true, ct));
        Assert.Equal(employeeRequest.Id, Assert.Single(await fixture.Service.GetPendingRequestsAsync(1, ct)).Id);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => fixture.Service.ApproveLeaveRequestAsync(ownRequest.Id, 3, ct));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => fixture.Service.RejectLeaveRequestAsync(ownRequest.Id, "No", 3, ct));
        await fixture.Service.ApproveLeaveRequestAsync(employeeRequest.Id, 3, ct);
        await fixture.Service.RejectLeaveRequestAsync(managerRequest.Id, "Rejected by HR", 3, ct);
        Assert.Empty(await fixture.Service.GetPendingRequestsAsync(3, ct));
        Assert.Equal(2, await fixture.Auth.AuditLogs.CountAsync(ct));
    }

    [Fact]
    public async Task GlobalPermissionRevocationAppliesWithoutRefreshingToken()
    {
        await using var fixture = await Fixture.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        await fixture.ExecuteAsync("""
            INSERT INTO permissions VALUES (3, 'leave.approve.all', 'Global');
            DELETE FROM role_permissions WHERE role_id IN (2, 3);
            INSERT INTO role_permissions VALUES (3, 3);
            """);
        var request = await fixture.Service.CreateLeaveRequestAsync(2, Request(LeaveTypes.Annual), ct);
        Assert.Single(await fixture.Service.GetPendingRequestsAsync(3, ct));
        await fixture.ExecuteAsync("DELETE FROM role_permissions WHERE role_id = 3");
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => fixture.Service.GetPendingRequestsAsync(3, ct));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => fixture.Service.GetApprovalOptionsAsync(3, ct));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => fixture.Service.ApproveLeaveRequestAsync(request.Id, 3, ct));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => fixture.Service.RejectLeaveRequestAsync(request.Id, "No", 3, ct));
    }

    [Fact]
    public async Task GlobalAndDirectManagerCannotOverwriteEachOthersDecision()
    {
        await using var fixture = await Fixture.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        await fixture.ExecuteAsync("""
            INSERT INTO permissions VALUES (3, 'leave.approve.all', 'Global');
            DELETE FROM role_permissions WHERE role_id IN (2, 3);
            INSERT INTO role_permissions VALUES (3, 3);
            """);
        var request = await fixture.Service.CreateLeaveRequestAsync(2, Request(LeaveTypes.Annual), ct);
        await using var staleContext = fixture.CreateLeaveContext();
        await staleContext.LeaveRequests.SingleAsync(r => r.Id == request.Id, ct);
        var competingService = new LeaveService(staleContext, new EmployeeLeaveAccess(fixture.Auth, TimeProvider.System), new UnexpectedRevoker(), TimeProvider.System);
        await fixture.Service.ApproveLeaveRequestAsync(request.Id, 1, ct);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => competingService.RejectLeaveRequestAsync(request.Id, "Stale", 3, ct));
        Assert.Equal(1, await fixture.Auth.AuditLogs.CountAsync(ct));
    }

    private static CreateLeaveRequestDto Request(string type) => new(type, new DateOnly(2027, 1, 4), new DateOnly(2027, 1, 4), "Test");

    private sealed class Fixture(NpgsqlConnection connection, AuthDbContext auth, LeaveDbContext leave) : IAsyncDisposable
    {
        public AuthDbContext Auth { get; } = auth;
        public LeaveDbContext CreateLeaveContext() => new(new DbContextOptionsBuilder<LeaveDbContext>().UseNpgsql(connection).Options);
        public LeaveService Service { get; } = new(leave, new EmployeeLeaveAccess(auth, TimeProvider.System), new UnexpectedRevoker(), TimeProvider.System);

        public static async Task<Fixture> CreateAsync()
        {
            var connectionString = Environment.GetEnvironmentVariable("HRM_TEST_POSTGRES_CONNECTION");
            Assert.SkipWhen(string.IsNullOrWhiteSpace(connectionString), "Requires HRM_TEST_POSTGRES_CONNECTION for a separate disposable test database; never uses the application cloud connection.");
            var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            try
            {
                // TEMP tables shadow the public schema on this connection; no real employee/user/leave rows are touched.
                await using var command = new NpgsqlCommand("""
                    CREATE TEMP TABLE departments (dept_id bigint PRIMARY KEY, dept_code text, dept_name text);
                    CREATE TEMP TABLE employees (emp_id bigint PRIMARY KEY, dept_id bigint, position_id bigint, emp_code text,
                        full_name text, dob date, gender text, phone text, email text, address text, education_level text,
                        hire_date date, status text, created_at timestamptz, manager_id bigint);
                    CREATE TEMP TABLE roles (role_id bigint PRIMARY KEY, role_name text, description text);
                    CREATE TEMP TABLE users (user_id bigint PRIMARY KEY, emp_id bigint, role_id bigint, username text,
                        password_hash text, is_active boolean, last_login timestamp);
                    CREATE TEMP TABLE user_security_states (user_id bigint PRIMARY KEY, failed_access_count integer, lockout_end timestamptz);
                    CREATE TEMP TABLE permissions (perm_id bigint PRIMARY KEY, permission_code text, description text);
                    CREATE TEMP TABLE role_permissions (role_id bigint, perm_id bigint, PRIMARY KEY(role_id, perm_id));
                    CREATE TEMP TABLE audit_logs (id uuid PRIMARY KEY, actor_user_id bigint, action text, entity_type text,
                        entity_id text, before_json jsonb, after_json jsonb, created_at timestamptz);
                    CREATE TEMP TABLE leave_requests (id uuid PRIMARY KEY, emp_id bigint, leave_type text, start_date date,
                        end_date date, days_count numeric, reason text, status text, rejection_reason text, attachment_file_id uuid,
                        attachment_file_name text, approved_by_user_id bigint, approved_at timestamptz,
                        is_auto_approved boolean NOT NULL DEFAULT false, created_at timestamptz, updated_at timestamptz);
                    INSERT INTO departments VALUES (1, 'TEST', 'Test');
                    INSERT INTO employees (emp_id, dept_id, emp_code, full_name, status, created_at, manager_id)
                        VALUES (10, 1, 'M1', 'Manager', 'ACTIVE', now(), NULL),
                               (20, 1, 'E1', 'Employee', 'ACTIVE', now(), 10),
                               (30, 1, 'M2', 'Other Manager', 'ACTIVE', now(), NULL);
                    INSERT INTO roles VALUES (1, 'MANAGER', NULL), (2, 'EMPLOYEE', NULL), (3, 'ADMIN', NULL);
                    INSERT INTO users VALUES (1, 10, 1, 'm1', 'unused', true, NULL),
                        (2, 20, 2, 'e1', 'unused', true, NULL), (3, 30, 3, 'm2', 'unused', true, NULL);
                    INSERT INTO permissions VALUES (1, 'leave.approve', 'Approve'), (2, 'self_approved', 'Automatic');
                    INSERT INTO role_permissions VALUES (1, 1), (2, 2), (3, 1);
                    """, connection);
                await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
                return new Fixture(connection,
                    new AuthDbContext(new DbContextOptionsBuilder<AuthDbContext>().UseNpgsql(connection).Options),
                    new LeaveDbContext(new DbContextOptionsBuilder<LeaveDbContext>().UseNpgsql(connection).Options));
            }
            catch
            {
                await connection.DisposeAsync();
                throw;
            }
        }

        public async Task ExecuteAsync(string sql)
        {
            await using var command = new NpgsqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }

        public async ValueTask DisposeAsync()
        {
            await leave.DisposeAsync();
            await Auth.DisposeAsync();
            await connection.DisposeAsync();
        }
    }

    private sealed class UnexpectedRevoker : IEmployeeAccessRevoker
    {
        public Task RevokeAsync(long employeeId, long actorUserId, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Automatic approval must never revoke resignation access.");
    }
}
