using System.Data;
using System.Globalization;
using System.Text.Json;
using Hrm.Contracts;
using Hrm.Modules.Auth.Domain;
using Hrm.Modules.Auth.Infrastructure.Persistence;
using Hrm.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hrm.Modules.Auth.Application;

public interface IAdminService
{
    Task<PagedResult<AdminUserDto>> SearchUsersAsync(string? keyword, string? department, string? role, string? status,
        string sortBy, bool descending, int page, int pageSize, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<EmployeeAccountOptionDto>> GetEmployeeOptionsAsync(CancellationToken cancellationToken);
    Task<AdminUserDto> CreateUserAsync(CreateAdminUserRequest request, long actorUserId, CancellationToken cancellationToken);
    Task<AdminUserDto> UpdateUserAsync(long userId, UpdateAdminUserRequest request, long actorUserId, CancellationToken cancellationToken);
    Task SetStatusAsync(long userId, bool isActive, long actorUserId, CancellationToken cancellationToken);
    Task SetLockAsync(long userId, bool isLocked, long actorUserId, CancellationToken cancellationToken);
    Task SetRolesAsync(long userId, IReadOnlyCollection<string> roles, long actorUserId, CancellationToken cancellationToken);
    Task ResetPasswordAsync(long userId, string newPassword, long actorUserId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<RoleDto>> GetRolesAsync(CancellationToken cancellationToken);
    Task SetRolePermissionsAsync(long roleId, IReadOnlyCollection<string> permissions, long actorUserId, CancellationToken cancellationToken);
    Task<PagedResult<AuditLogDto>> SearchAuditAsync(string? action, int page, int pageSize, CancellationToken cancellationToken);
}

public sealed class AdminService(AuthDbContext dbContext, TimeProvider timeProvider) : IAdminService
{
    public async Task<PagedResult<AdminUserDto>> SearchUsersAsync(
        string? keyword, string? department, string? role, string? status, string sortBy, bool descending,
        int page, int pageSize, CancellationToken cancellationToken)
    {
        (page, pageSize) = NormalizePaging(page, pageSize);
        var query = dbContext.Users.AsNoTracking()
            .Include(user => user.Employee).ThenInclude(employee => employee.Department)
            .Include(user => user.Role).Include(user => user.SecurityState).Include(user => user.Metadata).AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var pattern = $"%{keyword.Trim()}%";
            query = query.Where(user => EF.Functions.ILike(user.UserName, pattern) ||
                EF.Functions.ILike(user.Employee.FullName, pattern) || EF.Functions.ILike(user.Employee.Code, pattern));
        }
        if (!string.IsNullOrWhiteSpace(department))
            query = query.Where(user => EF.Functions.ILike(user.Employee.Department.Code, department.Trim()) ||
                EF.Functions.ILike(user.Employee.Department.Name, department.Trim()));
        if (!string.IsNullOrWhiteSpace(role))
            query = query.Where(user => EF.Functions.ILike(user.Role.Name, role.Trim()));

        var now = timeProvider.GetUtcNow();
        query = status?.ToUpperInvariant() switch
        {
            "ACTIVE" => query.Where(user => user.IsActive &&
                (user.SecurityState == null || user.SecurityState.LockoutEnd == null || user.SecurityState.LockoutEnd <= now)),
            "LOCKED" => query.Where(user => user.SecurityState != null && user.SecurityState.LockoutEnd > now),
            "INACTIVE" => query.Where(user => !user.IsActive),
            _ => query
        };

        query = (sortBy.ToUpperInvariant(), descending) switch
        {
            ("LASTLOGIN", false) => query.OrderBy(user => user.LastLoginAt),
            ("LASTLOGIN", true) => query.OrderByDescending(user => user.LastLoginAt),
            ("USERNAME", false) => query.OrderBy(user => user.UserName),
            ("USERNAME", true) => query.OrderByDescending(user => user.UserName),
            ("CREATEDAT", false) => query.OrderBy(user => user.Metadata!.CreatedAt),
            ("CREATEDAT", true) => query.OrderByDescending(user => user.Metadata!.CreatedAt),
            (_, false) => query.OrderBy(user => user.Employee.FullName),
            _ => query.OrderByDescending(user => user.Metadata!.CreatedAt)
        };

        var total = await query.LongCountAsync(cancellationToken);
        var users = await query.Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(cancellationToken);
        return new PagedResult<AdminUserDto>(users.Select(user => MapUser(user, now)).ToArray(), page, pageSize, total);
    }

    public async Task<IReadOnlyCollection<EmployeeAccountOptionDto>> GetEmployeeOptionsAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Employees.AsNoTracking()
            .Include(employee => employee.Department)
            .OrderBy(employee => employee.FullName)
            .Select(employee => new EmployeeAccountOptionDto(
                employee.Id, employee.Code, employee.FullName, employee.Department.Name,
                dbContext.Users.Any(user => user.EmployeeId == employee.Id)))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<AdminUserDto> CreateUserAsync(
        CreateAdminUserRequest request, long actorUserId, CancellationToken cancellationToken)
    {
        EnsurePassword(request.Password);
        var role = await ResolveSingleRoleAsync(request.Roles, cancellationToken);
        var employee = await dbContext.Employees.Include(item => item.Department)
            .SingleOrDefaultAsync(item => item.Id == request.EmployeeId, cancellationToken)
            ?? throw new DomainException("EMPLOYEE_NOT_FOUND", "Không tìm thấy nhân viên.");
        if (await dbContext.Users.AnyAsync(user => user.EmployeeId == request.EmployeeId, cancellationToken))
            throw new DomainException("EMPLOYEE_ACCOUNT_EXISTS", "Nhân viên đã có tài khoản.");
        if (await dbContext.Users.AnyAsync(user => EF.Functions.ILike(user.UserName, request.UserName.Trim()), cancellationToken))
            throw new DomainException("USERNAME_EXISTS", "Tên đăng nhập đã tồn tại.");

        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync("LOCK TABLE users IN EXCLUSIVE MODE", cancellationToken);
        var user = new ApplicationUser
        {
            Id = (await dbContext.Users.MaxAsync(item => (long?)item.Id, cancellationToken) ?? 0) + 1,
            EmployeeId = employee.Id,
            RoleId = role.Id,
            UserName = request.UserName.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, 12),
            IsActive = true,
            Employee = employee,
            Role = role
        };
        dbContext.Users.Add(user);
        user.Metadata = new UserAccountMetadata { UserId = user.Id, CreatedAt = timeProvider.GetUtcNow() };
        await dbContext.SaveChangesAsync(cancellationToken);
        await AddAuditAsync(actorUserId, "admin.user.created", "User", IdText(user.Id), null,
            new { user.UserName, user.EmployeeId, Role = role.Name }, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return MapUser(user, timeProvider.GetUtcNow());
    }

    public async Task<AdminUserDto> UpdateUserAsync(
        long userId, UpdateAdminUserRequest request, long actorUserId, CancellationToken cancellationToken)
    {
        var user = await FindUserAsync(userId, cancellationToken);
        var employee = await dbContext.Employees.Include(item => item.Department)
            .SingleOrDefaultAsync(item => item.Id == request.EmployeeId, cancellationToken)
            ?? throw new DomainException("EMPLOYEE_NOT_FOUND", "Không tìm thấy nhân viên.");
        if (await dbContext.Users.AnyAsync(item => item.Id != userId && item.EmployeeId == request.EmployeeId, cancellationToken))
            throw new DomainException("EMPLOYEE_ACCOUNT_EXISTS", "Nhân viên đã có tài khoản.");
        var before = user.EmployeeId;
        user.EmployeeId = employee.Id;
        user.Employee = employee;
        await dbContext.SaveChangesAsync(cancellationToken);
        await AddAuditAsync(actorUserId, "admin.user.updated", "User", IdText(user.Id),
            new { EmployeeId = before }, new { user.EmployeeId }, cancellationToken);
        return MapUser(user, timeProvider.GetUtcNow());
    }

    public async Task SetStatusAsync(long userId, bool isActive, long actorUserId, CancellationToken cancellationToken)
    {
        var user = await FindUserAsync(userId, cancellationToken);
        var before = user.IsActive;
        user.IsActive = isActive;
        if (!isActive) await RevokeUserTokensAsync(user.Id, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await AddAuditAsync(actorUserId, isActive ? "admin.user.activated" : "admin.user.deactivated",
            "User", IdText(user.Id), new { IsActive = before }, new { user.IsActive }, cancellationToken);
    }

    public async Task SetLockAsync(long userId, bool isLocked, long actorUserId, CancellationToken cancellationToken)
    {
        var user = await FindUserAsync(userId, cancellationToken);
        user.SecurityState ??= new UserSecurityState { UserId = user.Id };
        var before = user.SecurityState.LockoutEnd;
        user.SecurityState.LockoutEnd = isLocked ? timeProvider.GetUtcNow().AddYears(10) : null;
        user.SecurityState.FailedAccessCount = 0;
        if (isLocked) await RevokeUserTokensAsync(user.Id, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await AddAuditAsync(actorUserId, isLocked ? "admin.user.locked" : "admin.user.unlocked",
            "User", IdText(user.Id), new { LockoutEnd = before }, new { user.SecurityState.LockoutEnd }, cancellationToken);
    }

    public async Task SetRolesAsync(long userId, IReadOnlyCollection<string> roles, long actorUserId, CancellationToken cancellationToken)
    {
        var user = await FindUserAsync(userId, cancellationToken);
        var role = await ResolveSingleRoleAsync(roles, cancellationToken);
        var before = user.Role.Name;
        user.RoleId = role.Id;
        user.Role = role;
        await dbContext.SaveChangesAsync(cancellationToken);
        await AddAuditAsync(actorUserId, "admin.user.roles.changed", "User", IdText(user.Id),
            new { Role = before }, new { Role = role.Name }, cancellationToken);
    }

    public async Task ResetPasswordAsync(long userId, string newPassword, long actorUserId, CancellationToken cancellationToken)
    {
        EnsurePassword(newPassword);
        var user = await FindUserAsync(userId, cancellationToken);
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, 12);
        await RevokeUserTokensAsync(user.Id, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await AddAuditAsync(actorUserId, "admin.user.password-reset", "User", IdText(user.Id), null,
            new { PasswordReset = true }, cancellationToken);
    }

    public async Task<IReadOnlyCollection<RoleDto>> GetRolesAsync(CancellationToken cancellationToken)
    {
        var roles = await dbContext.Roles.AsNoTracking().OrderBy(role => role.Name).ToArrayAsync(cancellationToken);
        var grants = await (from grant in dbContext.RolePermissions.AsNoTracking()
                            join permission in dbContext.Permissions.AsNoTracking() on grant.PermissionId equals permission.Id
                            select new { grant.RoleId, permission.Code }).ToArrayAsync(cancellationToken);
        return roles.Select(role => new RoleDto(role.Id, role.Name, role.Description, true,
            grants.Where(grant => grant.RoleId == role.Id).Select(grant => grant.Code).OrderBy(code => code).ToArray())).ToArray();
    }

    public async Task SetRolePermissionsAsync(
        long roleId, IReadOnlyCollection<string> permissions, long actorUserId, CancellationToken cancellationToken)
    {
        var role = await dbContext.Roles.SingleOrDefaultAsync(item => item.Id == roleId, cancellationToken)
            ?? throw new DomainException("ROLE_NOT_FOUND", "Không tìm thấy vai trò.");
        var requested = permissions.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var definitions = await dbContext.Permissions.Where(item => requested.Contains(item.Code)).ToArrayAsync(cancellationToken);
        if (definitions.Length != requested.Length)
            throw new DomainException("PERMISSION_INVALID", "Danh sách quyền chứa mã không hợp lệ.");

        var existing = await dbContext.RolePermissions.Where(item => item.RoleId == roleId).ToArrayAsync(cancellationToken);
        var before = existing.Select(item => item.PermissionId).ToArray();
        dbContext.RolePermissions.RemoveRange(existing);
        dbContext.RolePermissions.AddRange(definitions.Select(item => new RolePermissionGrant
        { RoleId = roleId, PermissionId = item.Id }));
        await dbContext.SaveChangesAsync(cancellationToken);
        await AddAuditAsync(actorUserId, "admin.role.permissions.changed", "Role", IdText(roleId),
            new { PermissionIds = before }, new { Permissions = requested, Role = role.Name }, cancellationToken);
    }

    public async Task<PagedResult<AuditLogDto>> SearchAuditAsync(
        string? action, int page, int pageSize, CancellationToken cancellationToken)
    {
        (page, pageSize) = NormalizePaging(page, pageSize);
        var query = dbContext.AuditLogs.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(action)) query = query.Where(entry => entry.Action.Contains(action));
        var total = await query.LongCountAsync(cancellationToken);
        var items = await query.OrderByDescending(entry => entry.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(entry => new AuditLogDto(entry.Id, entry.ActorUserId, entry.Action, entry.EntityType,
                entry.EntityId, entry.BeforeJson, entry.AfterJson, entry.CreatedAt)).ToArrayAsync(cancellationToken);
        return new PagedResult<AuditLogDto>(items, page, pageSize, total);
    }

    private async Task<ApplicationUser> FindUserAsync(long userId, CancellationToken cancellationToken) =>
        await dbContext.Users.Include(user => user.Employee).ThenInclude(employee => employee.Department)
            .Include(user => user.Role).Include(user => user.SecurityState)
            .Include(user => user.Metadata)
            .SingleOrDefaultAsync(user => user.Id == userId, cancellationToken)
        ?? throw new DomainException("USER_NOT_FOUND", "Không tìm thấy tài khoản.");

    private async Task<ApplicationRole> ResolveSingleRoleAsync(IReadOnlyCollection<string> roles, CancellationToken cancellationToken)
    {
        var requested = roles.Select(item => item.Trim()).Where(item => item.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (requested.Length != 1)
            throw new DomainException("ROLE_INVALID", "Mỗi tài khoản phải có đúng một vai trò.");
        return await dbContext.Roles.SingleOrDefaultAsync(
            role => EF.Functions.ILike(role.Name, requested[0]), cancellationToken)
            ?? throw new DomainException("ROLE_INVALID", $"Vai trò '{requested[0]}' không tồn tại.");
    }

    private async Task RevokeUserTokensAsync(long userId, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var tokens = await dbContext.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null && token.ExpiresAt > now)
            .ToArrayAsync(cancellationToken);
        foreach (var token in tokens) token.RevokedAt = now;
    }

    private async Task AddAuditAsync(long actorUserId, string action, string entityType, string entityId,
        object? before, object? after, CancellationToken cancellationToken)
    {
        dbContext.AuditLogs.Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            BeforeJson = before is null ? null : JsonSerializer.Serialize(before),
            AfterJson = after is null ? null : JsonSerializer.Serialize(after),
            CreatedAt = timeProvider.GetUtcNow()
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static AdminUserDto MapUser(ApplicationUser user, DateTimeOffset now) => new(
        user.Id, user.EmployeeId, user.UserName, user.Employee.FullName, user.Employee.Department.Name,
        user.IsActive, user.SecurityState?.LockoutEnd > now, user.Metadata?.CreatedAt ?? now,
        user.LastLoginAt is null ? null : new DateTimeOffset(DateTime.SpecifyKind(user.LastLoginAt.Value, DateTimeKind.Utc)),
        [user.Role.Name]);

    private static void EnsurePassword(string password)
    {
        if (password.Length < 3)
            throw new DomainException("PASSWORD_WEAK", "Mật khẩu cần ít nhất 3 ký tự.");
    }

    private static (int Page, int PageSize) NormalizePaging(int page, int pageSize) =>
        (Math.Max(1, page), Math.Clamp(pageSize, 1, 100));

    private static string IdText(long id) => id.ToString(CultureInfo.InvariantCulture);
}
