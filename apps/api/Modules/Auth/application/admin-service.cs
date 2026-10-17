using System.Data;
using System.Globalization;
using System.Text.Json;
using Hrm.Contracts;
using Hrm.Modules.Auth.Domain;
using Hrm.Modules.Auth.Infrastructure.Persistence;
using Hrm.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

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
    Task DeleteUserAsync(long userId, long actorUserId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<RoleDto>> GetRolesAsync(CancellationToken cancellationToken);
    Task<RoleDto> CreateRoleAsync(CreateRoleRequest request, long actorUserId, CancellationToken cancellationToken);
    Task DeleteRoleAsync(long roleId, long actorUserId, CancellationToken cancellationToken);
    Task SetRolePermissionsAsync(long roleId, IReadOnlyCollection<string> permissions, long actorUserId, CancellationToken cancellationToken);
    Task<PagedResult<AuditLogDto>> SearchAuditAsync(string? action, int page, int pageSize, CancellationToken cancellationToken);
}

public sealed class AdminService(
    AuthDbContext dbContext,
    TimeProvider timeProvider,
    IOptions<SecurityOptions> securityOptions) : IAdminService
{
    private readonly SecurityOptions secOptions = securityOptions.Value;
    public async Task<PagedResult<AdminUserDto>> SearchUsersAsync(
        string? keyword, string? department, string? role, string? status, string sortBy, bool descending,
        int page, int pageSize, CancellationToken cancellationToken)
    {
        (page, pageSize) = NormalizePaging(page, pageSize);
        var query = dbContext.Users.AsNoTracking()
            .Include(user => user.Employee).ThenInclude(employee => employee!.Department)
            .Include(user => user.Role).Include(user => user.SecurityState).Include(user => user.Metadata).AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var pattern = $"%{keyword.Trim()}%";
            query = query.Where(user => EF.Functions.ILike(user.UserName, pattern) ||
                (user.Employee != null && (EF.Functions.ILike(user.Employee.FullName, pattern) || EF.Functions.ILike(user.Employee.Code, pattern))) ||
                (user.Employee == null && EF.Functions.ILike("System Administrator", pattern)));
        }
        if (!string.IsNullOrWhiteSpace(department))
        {
            var deptTrim = department.Trim();
            query = query.Where(user =>
                (user.Employee != null && (EF.Functions.ILike(user.Employee.Department.Code, deptTrim) || EF.Functions.ILike(user.Employee.Department.Name, deptTrim))) ||
                (user.Employee == null && EF.Functions.ILike("Quản trị Hệ thống", $"%{deptTrim}%")));
        }
        if (!string.IsNullOrWhiteSpace(role))
            query = query.Where(user => EF.Functions.ILike(user.Role.Name, role.Trim()));

        var now = timeProvider.GetUtcNow();
        query = status?.ToUpperInvariant() switch
        {
            "ACTIVE" => query.Where(user => user.IsActive &&
                (user.SecurityState == null || user.SecurityState.LockoutEnd == null || user.SecurityState.LockoutEnd <= now)),
            "LOCKED" => query.Where(user => !user.IsActive || (user.SecurityState != null && user.SecurityState.LockoutEnd > now)),
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
            (_, false) => query.OrderBy(user => user.Employee != null ? user.Employee.FullName : user.UserName),
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
        var userName = request.UserName.Trim();
        if (string.IsNullOrWhiteSpace(userName))
            throw new DomainException("USERNAME_INVALID", "Vui lòng nhập tên đăng nhập.");

        Employee? employee = null;
        if (IsAdminRole(role.Name))
        {
            if (request.EmployeeId is not null and > 0)
                throw new DomainException("ADMIN_EMPLOYEE_LINK_FORBIDDEN", "Tài khoản Quản trị Hệ thống không thể gán vào nhân viên.");
        }
        else
        {
            if (request.EmployeeId is null or <= 0)
                throw new DomainException("EMPLOYEE_REQUIRED", "Tài khoản nhân viên bắt buộc phải chọn nhân sự.");

            employee = await dbContext.Employees.Include(item => item.Department)
                .SingleOrDefaultAsync(item => item.Id == request.EmployeeId.Value, cancellationToken)
                ?? throw new DomainException("EMPLOYEE_NOT_FOUND", "Không tìm thấy nhân viên.");
            if (await dbContext.Users.AnyAsync(user => user.EmployeeId == request.EmployeeId, cancellationToken))
                throw new DomainException("EMPLOYEE_ACCOUNT_EXISTS", "Nhân viên đã có tài khoản.");
        }

        // Tài khoản đã bị xóa mềm trước đó của chính nhân viên này (nếu có) sẽ được tái sử dụng
        // để không vi phạm ràng buộc duy nhất emp_id/username của schema hiện có.
        var deletedAccount = request.EmployeeId is not null and > 0
            ? await dbContext.Users.IgnoreQueryFilters()
                .Include(user => user.SecurityState).Include(user => user.Metadata)
                .FirstOrDefaultAsync(user => user.EmployeeId == request.EmployeeId && user.DeletedAt != null, cancellationToken)
            : null;
        var reusedId = deletedAccount?.Id ?? 0;
        if (await dbContext.Users.IgnoreQueryFilters()
                .AnyAsync(user => user.Id != reusedId && EF.Functions.ILike(user.UserName, userName), cancellationToken))
            throw new DomainException("USERNAME_EXISTS", "Tên đăng nhập đã tồn tại (kể cả tài khoản đã xóa).");

        var strategy = dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable, cancellationToken);
            await dbContext.Database.ExecuteSqlRawAsync("LOCK TABLE users IN EXCLUSIVE MODE", cancellationToken);
            var now = timeProvider.GetUtcNow();
            ApplicationUser user;
            if (deletedAccount is not null)
            {
                user = deletedAccount;
                user.UserName = userName;
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, 12);
                user.RoleId = role.Id;
                user.Role = role;
                user.IsActive = true;
                user.DeletedAt = null;
                user.LastLoginAt = null;
                user.Employee = employee;
                if (user.SecurityState is not null)
                {
                    user.SecurityState.LockoutEnd = null;
                    user.SecurityState.FailedAccessCount = 0;
                }
                if (user.Metadata is null) user.Metadata = new UserAccountMetadata { UserId = user.Id, CreatedAt = now };
                else user.Metadata.CreatedAt = now;
            }
            else
            {
                user = new ApplicationUser
                {
                    Id = (await dbContext.Users.IgnoreQueryFilters().MaxAsync(item => (long?)item.Id, cancellationToken) ?? 0) + 1,
                    EmployeeId = employee?.Id,
                    RoleId = role.Id,
                    UserName = userName,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, 12),
                    IsActive = true,
                    Employee = employee,
                    Role = role
                };
                dbContext.Users.Add(user);
                user.Metadata = new UserAccountMetadata { UserId = user.Id, CreatedAt = now };
            }
            await dbContext.SaveChangesAsync(cancellationToken);

            await AddAuditAsync(actorUserId, "admin.user.created", "User", IdText(user.Id), null,
                new { user.UserName, user.EmployeeId, Role = role.Name, ReusedDeletedAccount = deletedAccount is not null },
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return MapUser(user, timeProvider.GetUtcNow());
        });
    }

    public async Task<AdminUserDto> UpdateUserAsync(
        long userId, UpdateAdminUserRequest request, long actorUserId, CancellationToken cancellationToken)
    {
        var user = await FindUserAsync(userId, cancellationToken);
        if (IsAdminRole(user.Role.Name))
        {
            throw new DomainException("ADMIN_EMPLOYEE_LINK_FORBIDDEN", "Tài khoản Quản trị Hệ thống không thể gán vào nhân viên.");
        }
        var employee = await dbContext.Employees.Include(item => item.Department)
            .SingleOrDefaultAsync(item => item.Id == request.EmployeeId, cancellationToken)
            ?? throw new DomainException("EMPLOYEE_NOT_FOUND", "Không tìm thấy nhân viên.");
        if (await dbContext.Users.IgnoreQueryFilters().AnyAsync(item => item.Id != userId && item.EmployeeId == request.EmployeeId, cancellationToken))
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
        if (!isActive)
        {
            EnsureNotSelf(userId, actorUserId, "Bạn không thể tự vô hiệu hóa tài khoản của mình.");
            await EnsureNotLastAdminAsync(user, cancellationToken);
        }
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
        if (isLocked)
        {
            EnsureNotSelf(userId, actorUserId, "Bạn không thể tự khóa tài khoản của mình.");
            await EnsureNotLastAdminAsync(user, cancellationToken);
        }
        user.SecurityState ??= new UserSecurityState { UserId = user.Id };
        var beforeLock = user.SecurityState.LockoutEnd;
        var beforeActive = user.IsActive;
        user.SecurityState.LockoutEnd = isLocked ? timeProvider.GetUtcNow().AddYears(secOptions.AdminLockYears) : null;
        user.SecurityState.FailedAccessCount = 0;
        user.IsActive = !isLocked;
        if (isLocked) await RevokeUserTokensAsync(user.Id, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await AddAuditAsync(actorUserId, isLocked ? "admin.user.locked" : "admin.user.unlocked",
            "User", IdText(user.Id), new { LockoutEnd = beforeLock, IsActive = beforeActive },
            new { user.SecurityState.LockoutEnd, user.IsActive }, cancellationToken);
    }

    public async Task SetRolesAsync(long userId, IReadOnlyCollection<string> roles, long actorUserId, CancellationToken cancellationToken)
    {
        var user = await FindUserAsync(userId, cancellationToken);
        var role = await ResolveSingleRoleAsync(roles, cancellationToken);
        var before = user.Role.Name;
        if (IsAdminRole(before) && !IsAdminRole(role.Name))
        {
            EnsureNotSelf(userId, actorUserId, "Bạn không thể tự gỡ vai trò ADMIN của mình.");
            await EnsureNotLastAdminAsync(user, cancellationToken);
        }
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
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, secOptions.BcryptWorkFactor);
        await RevokeUserTokensAsync(user.Id, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await AddAuditAsync(actorUserId, "admin.user.password-reset", "User", IdText(user.Id), null,
            new { PasswordReset = true }, cancellationToken);
    }

    public async Task DeleteUserAsync(long userId, long actorUserId, CancellationToken cancellationToken)
    {
        EnsureNotSelf(userId, actorUserId, "Bạn không thể tự xóa tài khoản của mình.");
        var user = await FindUserAsync(userId, cancellationToken);
        await EnsureNotLastAdminAsync(user, cancellationToken);

        var now = timeProvider.GetUtcNow();
        user.DeletedAt = now;
        user.IsActive = false;
        await RevokeUserTokensAsync(user.Id, cancellationToken);
        var sessions = await dbContext.ActiveSessions.Where(item => item.UserId == user.Id).ToArrayAsync(cancellationToken);
        dbContext.ActiveSessions.RemoveRange(sessions);
        await dbContext.SaveChangesAsync(cancellationToken);
        await AddAuditAsync(actorUserId, "admin.user.deleted", "User", IdText(user.Id),
            new { user.UserName, user.EmployeeId, Role = user.Role.Name },
            new { DeletedAt = now, SoftDelete = true }, cancellationToken);
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

    public async Task<RoleDto> CreateRoleAsync(
        CreateRoleRequest request, long actorUserId, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("ROLE_NAME_INVALID", "Vui lòng nhập tên vai trò.");

        var description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        if (description?.Length > 250)
            throw new DomainException("ROLE_DESCRIPTION_INVALID", "Mô tả vai trò không được vượt quá 250 ký tự.");

        var requestedPermissions = request.Permissions.Select(PermissionCodes.Canonicalize).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (!name.Equals("ADMIN", StringComparison.OrdinalIgnoreCase) &&
            requestedPermissions.Any(p => p.StartsWith("admin.", StringComparison.OrdinalIgnoreCase)))
        {
            throw new DomainException("ADMIN_PERMISSIONS_RESTRICTED", "Các quyền quản trị hệ thống chỉ được phép gán cho vai trò ADMIN.");
        }

        var definitions = await ResolveAndEnsurePermissionsAsync(requestedPermissions, cancellationToken);

        var strategy = dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable, cancellationToken);
            if (await dbContext.Roles.AnyAsync(role => EF.Functions.ILike(role.Name, name), cancellationToken))
                throw new DomainException("ROLE_EXISTS", $"Vai trò '{name}' đã tồn tại.");

            var role = new ApplicationRole
            {
                Name = name,
                Description = description
            };
            dbContext.Roles.Add(role);
            await dbContext.SaveChangesAsync(cancellationToken);

            dbContext.RolePermissions.AddRange(definitions.Select(permission => new RolePermissionGrant
            {
                Role = role,
                RoleId = role.Id,
                Permission = permission,
                PermissionId = permission.Id
            }));
            await dbContext.SaveChangesAsync(cancellationToken);
            await AddAuditAsync(actorUserId, "admin.role.created", "Role", IdText(role.Id), null,
                new { role.Name, role.Description, Permissions = requestedPermissions }, cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new RoleDto(role.Id, role.Name, role.Description, true,
                requestedPermissions.OrderBy(code => code).ToArray());
        });
    }

    public async Task DeleteRoleAsync(long roleId, long actorUserId, CancellationToken cancellationToken)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable, cancellationToken);

            var role = await dbContext.Roles.SingleOrDefaultAsync(item => item.Id == roleId, cancellationToken)
                ?? throw new DomainException("ROLE_NOT_FOUND", "Không tìm thấy vai trò.");
            if (role.Name.Equals("ADMIN", StringComparison.OrdinalIgnoreCase))
                throw new DomainException("ROLE_ADMIN_PROTECTED", "Không thể xóa vai trò ADMIN.");
            if (await dbContext.Users.IgnoreQueryFilters().AnyAsync(user => user.RoleId == roleId, cancellationToken))
                throw new DomainException("ROLE_IN_USE", "Không thể xóa vai trò đang được gán cho tài khoản (kể cả tài khoản đã xóa).");

            var grants = await dbContext.RolePermissions
                .Where(item => item.RoleId == roleId)
                .ToArrayAsync(cancellationToken);
            dbContext.RolePermissions.RemoveRange(grants);
            dbContext.Roles.Remove(role);
            await dbContext.SaveChangesAsync(cancellationToken);
            await AddAuditAsync(actorUserId, "admin.role.deleted", "Role", IdText(role.Id),
                new { role.Name, role.Description, Permissions = grants.Select(item => item.PermissionId).ToArray() },
                null, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });
    }

    public async Task SetRolePermissionsAsync(
        long roleId, IReadOnlyCollection<string> permissions, long actorUserId, CancellationToken cancellationToken)
    {
        var role = await dbContext.Roles.SingleOrDefaultAsync(item => item.Id == roleId, cancellationToken)
            ?? throw new DomainException("ROLE_NOT_FOUND", "Không tìm thấy vai trò.");
        var requested = permissions.Select(PermissionCodes.Canonicalize).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var definitions = await ResolveAndEnsurePermissionsAsync(requested, cancellationToken);

        if (role.Name.Equals("ADMIN", StringComparison.OrdinalIgnoreCase) &&
            (!requested.Contains(PermissionCodes.AdminRbacManage, StringComparer.OrdinalIgnoreCase) ||
             !requested.Contains(PermissionCodes.AdminUserManage, StringComparer.OrdinalIgnoreCase)))
        {
            throw new DomainException("ADMIN_ESSENTIAL_PERMISSIONS_REQUIRED", "Vai trò ADMIN bắt buộc phải giữ các quyền quản trị tài khoản và vai trò.");
        }

        if (!role.Name.Equals("ADMIN", StringComparison.OrdinalIgnoreCase) &&
            requested.Any(p => p.StartsWith("admin.", StringComparison.OrdinalIgnoreCase)))
        {
            throw new DomainException("ADMIN_PERMISSIONS_RESTRICTED", "Các quyền quản trị hệ thống chỉ được phép gán cho vai trò ADMIN.");
        }

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
        await dbContext.Users.Include(user => user.Employee).ThenInclude(employee => employee!.Department)
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
        user.Id, user.EmployeeId, user.UserName,
        user.Employee?.FullName ?? "System Administrator",
        user.Employee?.Department?.Name ?? "Quản trị Hệ thống",
        user.IsActive, user.SecurityState?.LockoutEnd > now, user.Metadata?.CreatedAt ?? now,
        user.LastLoginAt is null ? null : new DateTimeOffset(DateTime.SpecifyKind(user.LastLoginAt.Value, DateTimeKind.Utc)),
        [user.Role.Name]);

    private static void EnsurePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new DomainException("PASSWORD_REQUIRED", "Vui lòng nhập mật khẩu.");
    }

    private static bool IsAdminRole(string roleName) =>
        roleName.Equals("ADMIN", StringComparison.OrdinalIgnoreCase);

    private static void EnsureNotSelf(long userId, long actorUserId, string message)
    {
        if (userId == actorUserId)
            throw new DomainException("ADMIN_SELF_ACTION_FORBIDDEN", message);
    }

    /// <summary>Không cho phép khóa/vô hiệu hóa/xóa/hạ quyền tài khoản ADMIN đang hoạt động cuối cùng.</summary>
    private async Task EnsureNotLastAdminAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        if (!IsAdminRole(user.Role.Name)) return;
        var now = timeProvider.GetUtcNow();
        var otherActiveAdmins = await dbContext.Users.AsNoTracking()
            .Where(item => item.Id != user.Id && item.IsActive && EF.Functions.ILike(item.Role.Name, "ADMIN"))
            .Where(item => item.SecurityState == null || item.SecurityState.LockoutEnd == null || item.SecurityState.LockoutEnd <= now)
            .AnyAsync(cancellationToken);
        if (!otherActiveAdmins)
            throw new DomainException("LAST_ADMIN_PROTECTED", "Không thể thực hiện: đây là tài khoản ADMIN đang hoạt động cuối cùng.");
    }

    private static (int Page, int PageSize) NormalizePaging(int page, int pageSize) =>
        (Math.Max(1, page), Math.Clamp(pageSize, 1, 100));

    private static string IdText(long id) => id.ToString(CultureInfo.InvariantCulture);

    private async Task<IReadOnlyList<Hrm.Modules.Auth.Domain.PermissionDefinition>> ResolveAndEnsurePermissionsAsync(
        string[] requested, CancellationToken cancellationToken)
    {
        var definitions = await dbContext.Permissions
            .Where(item => requested.Contains(item.Code))
            .ToListAsync(cancellationToken);

        var existingCodes = definitions.Select(d => d.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missingCodes = requested.Where(code => !existingCodes.Contains(code)).ToArray();

        if (missingCodes.Length > 0)
        {
            var catalogItems = PermissionCodes.Catalog
                .Where(c => missingCodes.Contains(c.Code, StringComparer.OrdinalIgnoreCase))
                .ToArray();

            if (catalogItems.Length > 0)
            {
                foreach (var item in catalogItems)
                {
                    var newPerm = new Hrm.Modules.Auth.Domain.PermissionDefinition
                    {
                        Code = item.Code,
                        Description = item.Description
                    };
                    dbContext.Permissions.Add(newPerm);
                    definitions.Add(newPerm);
                }

                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        if (definitions.Count != requested.Length)
        {
            throw new DomainException("PERMISSION_INVALID", "Danh sách quyền chứa mã không hợp lệ.");
        }

        return definitions;
    }
}
