using System.Globalization;
using System.Text.Json;
using Hrm.Contracts;
using Hrm.Modules.Auth.Contracts;
using Hrm.Modules.Auth.Domain;
using Hrm.Modules.Auth.Infrastructure.Persistence;
using Hrm.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hrm.Modules.Employees.Application;

public interface IEmployeeService
{
    Task<IReadOnlyList<EmployeeDto>> GetEmployeesAsync(
        string? keyword = null,
        long? departmentId = null,
        string? status = null,
        bool includeSensitiveInfo = true,
        CancellationToken cancellationToken = default);

    Task<EmployeeDto?> GetEmployeeByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<EmployeeDto> CreateEmployeeAsync(
        CreateEmployeeRequest request,
        long actorUserId,
        CancellationToken cancellationToken = default);

    Task<EmployeeDto> UpdateEmployeeAsync(
        long id,
        UpdateEmployeeRequest request,
        long actorUserId,
        bool hasTransferPermission = true,
        CancellationToken cancellationToken = default);

    Task<EmployeeDto> TransferDepartmentAsync(
        long id,
        TransferDepartmentRequest request,
        long actorUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DepartmentDto>> GetDepartmentsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PositionDto>> GetPositionsAsync(CancellationToken cancellationToken = default);
}

public sealed class EmployeeService(
    AuthDbContext dbContext,
    TimeProvider timeProvider) : IEmployeeService
{
    public async Task<IReadOnlyList<EmployeeDto>> GetEmployeesAsync(
        string? keyword = null,
        long? departmentId = null,
        string? status = null,
        bool includeSensitiveInfo = true,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Employees
            .Include(e => e.Department)
            .Include(e => e.Position)
            .Include(e => e.SalaryProfiles)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var trimmed = keyword.Trim();
            query = query.Where(e =>
                EF.Functions.ILike(e.FullName, $"%{trimmed}%") ||
                EF.Functions.ILike(e.Code, $"%{trimmed}%") ||
                (e.Email != null && EF.Functions.ILike(e.Email, $"%{trimmed}%")) ||
                (e.Phone != null && EF.Functions.ILike(e.Phone, $"%{trimmed}%")));
        }

        if (departmentId.HasValue && departmentId.Value > 0)
        {
            query = query.Where(e => e.DepartmentId == departmentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(e => EF.Functions.ILike(e.Status, status.Trim()));
        }

        var employees = await query
            .OrderByDescending(e => e.Id)
            .ToListAsync(cancellationToken);

        var employeeIds = employees.Select(e => e.Id).ToHashSet();
        var accounts = await dbContext.Users
            .AsNoTracking()
            .Where(u => employeeIds.Contains(u.EmployeeId))
            .Select(u => u.EmployeeId)
            .ToHashSetAsync(cancellationToken);

        return employees.Select(e => MapToDto(e, accounts.Contains(e.Id), includeSensitiveInfo)).ToList();
    }

    public async Task<EmployeeDto?> GetEmployeeByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var employee = await dbContext.Employees
            .Include(e => e.Department)
            .Include(e => e.Position)
            .Include(e => e.SalaryProfiles)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (employee is null) return null;

        var hasAccount = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(u => u.EmployeeId == employee.Id, cancellationToken);

        return MapToDto(employee, hasAccount);
    }

    public async Task<EmployeeDto> CreateEmployeeAsync(
        CreateEmployeeRequest request,
        long actorUserId,
        CancellationToken cancellationToken = default)
    {
        await ValidateManagerAsync(request.ManagerId, null, cancellationToken);
        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            throw new DomainException("INVALID_FULL_NAME", "Họ và tên nhân viên không được để trống.");
        }

        if (request.DepartmentId <= 0 || !await dbContext.Departments.AnyAsync(d => d.Id == request.DepartmentId, cancellationToken))
        {
            throw new DomainException("DEPARTMENT_NOT_FOUND", "Vui lòng chọn phòng ban hợp lệ.");
        }

        if (request.PositionId.HasValue && request.PositionId.Value > 0)
        {
            var pos = await dbContext.Positions.FirstOrDefaultAsync(p => p.Id == request.PositionId.Value, cancellationToken)
                ?? throw new DomainException("POSITION_NOT_FOUND", "Chức danh/vị trí công việc không tồn tại.");

            var dept = await dbContext.Departments.FirstOrDefaultAsync(d => d.Id == request.DepartmentId, cancellationToken);
            if (dept != null && !DepartmentPositionPolicy.IsCompatible(dept.Code, dept.Name, pos.Name, pos.Code))
            {
                throw new DomainException("INVALID_POSITION_FOR_DEPARTMENT",
                    $"Chức danh '{pos.Name}' không phù hợp với '{dept.Name}'. Vui lòng chọn chức vụ thuộc chuyên môn của phòng ban.");
            }
        }

        if (request.BaseSalary < 0)
        {
            throw new DomainException("INVALID_BASE_SALARY", "Mức lương cơ bản phải lớn hơn hoặc bằng 0.");
        }

        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            var codeTrim = request.Code.Trim();
            if (await dbContext.Employees.AnyAsync(e => EF.Functions.ILike(e.Code, codeTrim), cancellationToken))
            {
                throw new DomainException("EMPLOYEE_CODE_EXISTS", $"Mã nhân viên '{codeTrim}' đã tồn tại.");
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var emailTrim = request.Email.Trim();
            if (await dbContext.Employees.AnyAsync(e => e.Email != null && EF.Functions.ILike(e.Email, emailTrim), cancellationToken))
            {
                throw new DomainException("EMAIL_EXISTS", $"Email '{emailTrim}' đã được sử dụng cho nhân viên khác.");
            }
        }

        if (request.CreateAccount)
        {
            if (string.IsNullOrWhiteSpace(request.UserName))
            {
                throw new DomainException("INVALID_USERNAME", "Vui lòng nhập tên đăng nhập cho tài khoản nhân viên.");
            }

            if (await dbContext.Users.AnyAsync(u => EF.Functions.ILike(u.UserName, request.UserName.Trim()), cancellationToken))
            {
                throw new DomainException("USERNAME_EXISTS", $"Tên đăng nhập '{request.UserName.Trim()}' đã tồn tại.");
            }

            if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            {
                throw new DomainException("WEAK_PASSWORD", "Mật khẩu tạm phải có ít nhất 6 ký tự.");
            }
        }

        var strategy = dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            var nextEmpId = (await dbContext.Employees.MaxAsync(e => (long?)e.Id, cancellationToken) ?? 0) + 1;
            var empCode = string.IsNullOrWhiteSpace(request.Code)
                ? $"NV{nextEmpId:D4}"
                : request.Code.Trim();

            var now = timeProvider.GetUtcNow();
            var hireDate = request.HireDate ?? DateOnly.FromDateTime(DateTime.Today);
            var normalizedEdu = EducationLevelCatalog.Normalize(request.EducationLevel);

            var employee = new Employee
            {
                Id = nextEmpId,
                Code = empCode,
                FullName = request.FullName.Trim(),
                DepartmentId = request.DepartmentId,
                ManagerId = request.ManagerId,
                PositionId = request.PositionId > 0 ? request.PositionId : null,
                DateOfBirth = request.DateOfBirth,
                Gender = string.IsNullOrWhiteSpace(request.Gender) ? null : request.Gender.Trim(),
                Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
                Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
                Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim(),
                EducationLevel = normalizedEdu,
                HireDate = hireDate,
                Status = "ACTIVE",
                CreatedAt = now
            };

            dbContext.Employees.Add(employee);

            // Ghi nhận hồ sơ lương ban đầu (chuẩn 3NF & lưu vết lịch sử lương)
            var initialSalaryProfile = new EmployeeSalaryProfile
            {
                EmployeeId = employee.Id,
                BaseSalary = request.BaseSalary,
                PositionAllowance = 0m,
                OtherAllowance = 0m,
                EffectiveDate = hireDate,
                IsCurrent = true,
                CreatedAt = now,
                Note = "Khởi tạo cùng hồ sơ nhân sự"
            };
            dbContext.SalaryProfiles.Add(initialSalaryProfile);

            await dbContext.SaveChangesAsync(cancellationToken);

            var hasCreatedAccount = false;
            if (request.CreateAccount)
            {
                var roleName = string.IsNullOrWhiteSpace(request.Role) ? "EMPLOYEE" : request.Role.Trim().ToUpperInvariant();
                var role = await dbContext.Roles.FirstOrDefaultAsync(r => r.Name == roleName, cancellationToken)
                    ?? await dbContext.Roles.FirstOrDefaultAsync(r => r.Name == "EMPLOYEE", cancellationToken)
                    ?? throw new DomainException("ROLE_NOT_FOUND", "Không tìm thấy vai trò hệ thống hợp lệ.");

                var nextUserId = (await dbContext.Users.MaxAsync(u => (long?)u.Id, cancellationToken) ?? 0) + 1;
                var user = new ApplicationUser
                {
                    Id = nextUserId,
                    EmployeeId = employee.Id,
                    RoleId = role.Id,
                    UserName = request.UserName!.Trim(),
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password!.Trim(), 12),
                    IsActive = true,
                    Role = role,
                    Employee = employee
                };

                dbContext.Users.Add(user);
                user.Metadata = new UserAccountMetadata { UserId = user.Id, CreatedAt = now };
                await dbContext.SaveChangesAsync(cancellationToken);
                hasCreatedAccount = true;
            }

            dbContext.AuditLogs.Add(new AuditLogEntry
            {
                Id = Guid.NewGuid(),
                ActorUserId = actorUserId,
                Action = "employee.created",
                EntityType = "Employee",
                EntityId = employee.Id.ToString(CultureInfo.InvariantCulture),
                AfterJson = JsonSerializer.Serialize(new
                {
                    employee.Code,
                    employee.FullName,
                    employee.DepartmentId,
                    employee.ManagerId,
                    BaseSalary = request.BaseSalary,
                    CreatedAccount = hasCreatedAccount
                }),
                CreatedAt = now
            });
            await dbContext.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            // Re-fetch with nav properties
            var saved = await dbContext.Employees
                .Include(e => e.Department)
                .Include(e => e.Position)
                .Include(e => e.SalaryProfiles)
                .FirstAsync(e => e.Id == employee.Id, cancellationToken);

            return MapToDto(saved, hasCreatedAccount);
        });
    }

    public async Task<EmployeeDto> UpdateEmployeeAsync(
        long id,
        UpdateEmployeeRequest request,
        long actorUserId,
        bool hasTransferPermission = true,
        CancellationToken cancellationToken = default)
    {
        var employee = await dbContext.Employees
            .Include(e => e.Department)
            .Include(e => e.Position)
            .Include(e => e.SalaryProfiles)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
            ?? throw new DomainException("EMPLOYEE_NOT_FOUND", "Không tìm thấy nhân viên.");

        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            throw new DomainException("INVALID_FULL_NAME", "Họ và tên nhân viên không được để trống.");
        }

        if (request.DepartmentId <= 0 || !await dbContext.Departments.AnyAsync(d => d.Id == request.DepartmentId, cancellationToken))
        {
            throw new DomainException("DEPARTMENT_NOT_FOUND", "Vui lòng chọn phòng ban hợp lệ.");
        }

        // Kiểm tra quyền điều chuyển nếu có sự thay đổi phòng ban
        if (employee.DepartmentId != request.DepartmentId && !hasTransferPermission)
        {
            throw new DomainException("PERMISSION_DENIED", "Bạn không có quyền 'Điều chuyển phòng ban' (employee.transfer) để thay đổi phòng ban của nhân sự.");
        }

        if (request.PositionId.HasValue && request.PositionId.Value > 0)
        {
            var pos = await dbContext.Positions.FirstOrDefaultAsync(p => p.Id == request.PositionId.Value, cancellationToken)
                ?? throw new DomainException("POSITION_NOT_FOUND", "Chức danh/vị trí công việc không tồn tại.");

            var dept = await dbContext.Departments.FirstOrDefaultAsync(d => d.Id == request.DepartmentId, cancellationToken);
            if (dept != null && !DepartmentPositionPolicy.IsCompatible(dept.Code, dept.Name, pos.Name, pos.Code))
            {
                throw new DomainException("INVALID_POSITION_FOR_DEPARTMENT",
                    $"Chức danh '{pos.Name}' không phù hợp với '{dept.Name}'. Vui lòng chọn chức vụ thuộc chuyên môn của phòng ban.");
            }
        }

        if (request.BaseSalary < 0)
        {
            throw new DomainException("INVALID_BASE_SALARY", "Mức lương cơ bản phải lớn hơn hoặc bằng 0.");
        }

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var emailTrim = request.Email.Trim();
            if (await dbContext.Employees.AnyAsync(e => e.Id != id && e.Email != null && EF.Functions.ILike(e.Email, emailTrim), cancellationToken))
            {
                throw new DomainException("EMAIL_EXISTS", $"Email '{emailTrim}' đã được sử dụng cho nhân viên khác.");
            }
        }

        var previousStatus = employee.Status;
        await ValidateManagerAsync(request.ManagerId, employee.Id, cancellationToken);
        var previousManagerId = employee.ManagerId;
        // Chặn chuyển trạng thái sang RESIGNED từ form sửa thông thường
        if (!string.Equals(previousStatus, "RESIGNED", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(request.Status, "RESIGNED", StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException("INVALID_OPERATION", "Không thể chuyển trạng thái thôi việc tại form cập nhật hồ sơ thông thường. Vui lòng sử dụng tính năng 'Thôi việc' chuyên biệt để đảm bảo quy trình thu hồi quyền truy cập an toàn.");
        }

        var now = timeProvider.GetUtcNow();
        var normalizedEdu = EducationLevelCatalog.Normalize(request.EducationLevel);
        var effectiveHireDate = request.HireDate ?? employee.HireDate ?? DateOnly.FromDateTime(DateTime.Today);

        // Lưu vết lịch sử biến động lương nếu mức lương thay đổi (chuẩn 3NF)
        var currentSalaryProfile = await dbContext.SalaryProfiles
            .Where(sp => sp.EmployeeId == employee.Id && sp.IsCurrent)
            .OrderByDescending(sp => sp.EffectiveDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (currentSalaryProfile is null || currentSalaryProfile.BaseSalary != request.BaseSalary)
        {
            var activeProfiles = await dbContext.SalaryProfiles
                .Where(sp => sp.EmployeeId == employee.Id && sp.IsCurrent)
                .ToListAsync(cancellationToken);

            var todayDate = DateOnly.FromDateTime(DateTime.Today);
            foreach (var p in activeProfiles)
            {
                p.IsCurrent = false;
                p.ExpiryDate = todayDate;
            }

            dbContext.SalaryProfiles.Add(new EmployeeSalaryProfile
            {
                EmployeeId = employee.Id,
                BaseSalary = request.BaseSalary,
                PositionAllowance = currentSalaryProfile?.PositionAllowance ?? 0m,
                OtherAllowance = currentSalaryProfile?.OtherAllowance ?? 0m,
                EffectiveDate = todayDate,
                IsCurrent = true,
                CreatedAt = now,
                Note = "Cập nhật qua chỉnh sửa hồ sơ nhân sự"
            });
        }

        employee.FullName = request.FullName.Trim();
        employee.DepartmentId = request.DepartmentId;
        employee.ManagerId = request.ManagerId;
        employee.PositionId = request.PositionId > 0 ? request.PositionId : null;
        employee.DateOfBirth = request.DateOfBirth;
        employee.Gender = string.IsNullOrWhiteSpace(request.Gender) ? null : request.Gender.Trim();
        employee.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        employee.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        employee.Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim();
        employee.EducationLevel = normalizedEdu;
        employee.HireDate = effectiveHireDate;
        if (!string.Equals(previousStatus, "RESIGNED", StringComparison.OrdinalIgnoreCase))
        {
            employee.Status = string.IsNullOrWhiteSpace(request.Status) ? "ACTIVE" : request.Status.Trim().ToUpperInvariant();
        }

        dbContext.AuditLogs.Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            Action = "employee.updated",
            EntityType = "Employee",
            EntityId = employee.Id.ToString(CultureInfo.InvariantCulture),
            BeforeJson = JsonSerializer.Serialize(new { ManagerId = previousManagerId }),
            AfterJson = JsonSerializer.Serialize(new
            {
                employee.FullName,
                employee.ManagerId,
                employee.DepartmentId,
                BaseSalary = request.BaseSalary,
                employee.Status
            }),
            CreatedAt = now
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        var hasAccount = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(u => u.EmployeeId == employee.Id, cancellationToken);

        return MapToDto(employee, hasAccount);
    }

    public async Task<EmployeeDto> TransferDepartmentAsync(
        long id,
        TransferDepartmentRequest request,
        long actorUserId,
        CancellationToken cancellationToken = default)
    {
        var employee = await dbContext.Employees
            .Include(e => e.Department)
            .Include(e => e.Position)
            .Include(e => e.SalaryProfiles)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
            ?? throw new DomainException("EMPLOYEE_NOT_FOUND", "Không tìm thấy nhân viên.");

        if (string.Equals(employee.Status, "RESIGNED", StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException("INVALID_STATE", "Không thể điều chuyển nhân sự đã thôi việc.");
        }

        if (employee.DepartmentId == request.TargetDepartmentId)
        {
            throw new DomainException("INVALID_TRANSFER", "Phòng ban tiếp nhận phải khác phòng ban hiện tại.");
        }

        var targetDepartment = await dbContext.Departments
            .FirstOrDefaultAsync(d => d.Id == request.TargetDepartmentId, cancellationToken)
            ?? throw new DomainException("DEPARTMENT_NOT_FOUND", "Phòng ban tiếp nhận không tồn tại.");

        if (request.TargetPositionId.HasValue && request.TargetPositionId.Value > 0)
        {
            var targetPos = await dbContext.Positions.FirstOrDefaultAsync(p => p.Id == request.TargetPositionId.Value, cancellationToken)
                ?? throw new DomainException("POSITION_NOT_FOUND", "Chức danh/vị trí công việc mới không tồn tại.");

            if (!DepartmentPositionPolicy.IsCompatible(targetDepartment.Code, targetDepartment.Name, targetPos.Name, targetPos.Code))
            {
                throw new DomainException("INVALID_POSITION_FOR_DEPARTMENT",
                    $"Chức danh '{targetPos.Name}' không phù hợp với '{targetDepartment.Name}'. Vui lòng chọn chức vụ thuộc chuyên môn của phòng ban tiếp nhận.");
            }
        }

        var oldDepartmentName = employee.Department?.Name ?? "Chưa phân bổ";
        var oldPositionName = employee.Position?.Name ?? "Chưa phân chức vụ";
        var now = timeProvider.GetUtcNow();

        employee.DepartmentId = request.TargetDepartmentId;
        if (request.TargetPositionId.HasValue && request.TargetPositionId.Value > 0)
        {
            employee.PositionId = request.TargetPositionId.Value;
        }

        dbContext.AuditLogs.Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            Action = "employee.transferred",
            EntityType = "Employee",
            EntityId = employee.Id.ToString(CultureInfo.InvariantCulture),
            AfterJson = JsonSerializer.Serialize(new
            {
                EmployeeCode = employee.Code,
                EmployeeName = employee.FullName,
                FromDepartment = oldDepartmentName,
                ToDepartment = targetDepartment.Name,
                Reason = request.Reason,
                EffectiveDate = request.EffectiveDate
            }),
            CreatedAt = now
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        var saved = await dbContext.Employees
            .Include(e => e.Department)
            .Include(e => e.Position)
            .FirstAsync(e => e.Id == employee.Id, cancellationToken);

        var hasAccount = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(u => u.EmployeeId == employee.Id, cancellationToken);

        return MapToDto(saved, hasAccount);
    }

    public async Task<IReadOnlyList<DepartmentDto>> GetDepartmentsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Departments
            .AsNoTracking()
            .OrderBy(d => d.Code)
            .Select(d => new DepartmentDto(d.Id, d.Code, d.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PositionDto>> GetPositionsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Positions
            .AsNoTracking()
            .OrderBy(p => p.Code)
            .Select(p => new PositionDto(p.Id, p.Code, p.Name))
            .ToListAsync(cancellationToken);
    }

    private async Task ValidateManagerAsync(long? managerId, long? employeeId, CancellationToken cancellationToken)
    {
        if (managerId is null) return;
        var visited = new HashSet<long>();
        var currentId = managerId;
        while (currentId is not null)
        {
            if (currentId == employeeId || !visited.Add(currentId.Value))
                throw new DomainException("INVALID_MANAGER", "Không thể chọn chính nhân viên hoặc tạo vòng lặp quản lý.");
            var manager = await dbContext.Employees.AsNoTracking()
                .SingleOrDefaultAsync(e => e.Id == currentId.Value, cancellationToken)
                ?? throw new DomainException("MANAGER_NOT_FOUND", "Không tìm thấy quản lý.");
            if (currentId == managerId && manager.Status != "ACTIVE")
                throw new DomainException("MANAGER_INACTIVE", "Quản lý phải là nhân viên đang hoạt động.");
            currentId = manager.ManagerId;
        }
    }
    private static EmployeeDto MapToDto(Employee e, bool hasAccount, bool includeSensitiveInfo = true)
    {
        var currentSalary = e.SalaryProfiles.FirstOrDefault(sp => sp.IsCurrent)?.BaseSalary ?? 0m;
        return new(
            Id: e.Id,
            Code: e.Code,
            FullName: e.FullName,
            DepartmentId: e.DepartmentId,
            DepartmentName: e.Department?.Name ?? "Chưa phân bổ",
            PositionId: e.PositionId,
            PositionName: e.Position?.Name,
            DateOfBirth: includeSensitiveInfo ? e.DateOfBirth : null,
            Gender: e.Gender,
            Phone: e.Phone,
            Email: e.Email,
            Address: includeSensitiveInfo ? e.Address : null,
            EducationLevel: includeSensitiveInfo ? e.EducationLevel : null,
            BaseSalary: includeSensitiveInfo ? currentSalary : 0,
            HireDate: includeSensitiveInfo ? e.HireDate : null,
            Status: e.Status,
            HasAccount: includeSensitiveInfo && hasAccount,
            CreatedAt: e.CreatedAt,
            ManagerId: e.ManagerId);
    }
}
