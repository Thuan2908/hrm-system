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

    Task<EmployeeDto> PromoteEmployeeAsync(
        long id,
        PromoteEmployeeRequest request,
        long actorUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DepartmentDto>> GetDepartmentsAsync(bool includeInactive = false, CancellationToken cancellationToken = default);

    Task<DepartmentDto> CreateDepartmentAsync(CreateDepartmentRequest request, long actorUserId, CancellationToken cancellationToken = default);

    Task<DepartmentDto> UpdateDepartmentAsync(long id, UpdateDepartmentRequest request, long actorUserId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PositionDto>> GetPositionsAsync(bool includeInactive = false, CancellationToken cancellationToken = default);

    Task<PositionDto> CreatePositionAsync(CreatePositionRequest request, long actorUserId, CancellationToken cancellationToken = default);

    Task<PositionDto> UpdatePositionAsync(long id, UpdatePositionRequest request, long actorUserId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SalaryProfileDto>> GetSalaryHistoryAsync(long employeeId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EmployeeTimelineEventDto>> GetEmployeeTimelineAsync(long employeeId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetAvailableRolesAsync(CancellationToken cancellationToken = default);
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
            .Where(u => u.EmployeeId.HasValue && employeeIds.Contains(u.EmployeeId.Value))
            .Select(u => u.EmployeeId!.Value)
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

        var dept = await dbContext.Departments.FirstOrDefaultAsync(d => d.Id == request.DepartmentId, cancellationToken)
            ?? throw new DomainException("DEPARTMENT_NOT_FOUND", "Vui lòng chọn phòng ban hợp lệ.");

        Position? pos = null;
        if (request.PositionId.HasValue && request.PositionId.Value > 0)
        {
            pos = await dbContext.Positions.FirstOrDefaultAsync(p => p.Id == request.PositionId.Value, cancellationToken)
                ?? throw new DomainException("POSITION_NOT_FOUND", "Chức danh/vị trí công việc không tồn tại.");

            if (!DepartmentPositionPolicy.IsCompatible(dept.Id, pos.DepartmentId, dept.Code, dept.Name, pos.Name, pos.Code))
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

            if (await dbContext.Users.IgnoreQueryFilters().AnyAsync(u => EF.Functions.ILike(u.UserName, request.UserName.Trim()), cancellationToken))
            {
                throw new DomainException("USERNAME_EXISTS", $"Tên đăng nhập '{request.UserName.Trim()}' đã tồn tại.");
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                throw new DomainException("PASSWORD_REQUIRED", "Vui lòng nhập mật khẩu cho tài khoản nhân viên.");
            }
        }

        var strategy = dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            var now = timeProvider.GetUtcNow();
            var hireDate = request.HireDate ?? DateOnly.FromDateTime(DateTime.Today);
            var normalizedEdu = EducationLevelCatalog.Normalize(request.EducationLevel);
            var normalizedGender = GenderCatalog.Normalize(request.Gender);

            var isAutoCode = string.IsNullOrWhiteSpace(request.Code);
            var empCode = isAutoCode
                ? $"TMP{Random.Shared.Next(10000000, 99999999)}"
                : request.Code!.Trim();

            var employee = new Employee
            {
                Code = empCode,
                FullName = request.FullName.Trim(),
                DepartmentId = request.DepartmentId,
                ManagerId = request.ManagerId,
                PositionId = request.PositionId > 0 ? request.PositionId : null,
                DateOfBirth = request.DateOfBirth,
                Gender = normalizedGender,
                Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
                Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
                Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim(),
                EducationLevel = normalizedEdu,
                HireDate = hireDate,
                Status = "ACTIVE",
                CreatedAt = now,
                DependentCount = Math.Max(0, request.DependentCount),
                BankName = string.IsNullOrWhiteSpace(request.BankName) ? null : request.BankName.Trim(),
                BankAccountNumber = string.IsNullOrWhiteSpace(request.BankAccountNumber) ? null : request.BankAccountNumber.Trim()
            };

            dbContext.Employees.Add(employee);
            await dbContext.SaveChangesAsync(cancellationToken);

            if (isAutoCode)
            {
                employee.Code = $"NV{employee.Id:D4}";
            }

            // Ghi nhận hồ sơ lương ban đầu (chuẩn 3NF & lưu vết lịch sử lương)
            var posAllowance = request.PositionAllowance > 0m ? request.PositionAllowance : (pos?.DefaultPositionAllowance ?? 0m);
            var otherAllowance = Math.Max(0m, request.OtherAllowance);
            var insuranceSalary = request.InsuranceSalary ?? request.BaseSalary;

            var initialSalaryProfile = new EmployeeSalaryProfile
            {
                EmployeeId = employee.Id,
                BaseSalary = request.BaseSalary,
                PositionAllowance = posAllowance,
                OtherAllowance = otherAllowance,
                InsuranceSalary = insuranceSalary,
                EffectiveDate = hireDate,
                IsCurrent = true,
                CreatedAt = now,
                Note = "Khởi tạo cùng hồ sơ nhân sự"
            };
            dbContext.SalaryProfiles.Add(initialSalaryProfile);

            // Ghi nhận sự kiện Onboarding vào timeline
            var actorName = await GetActorNameAsync(actorUserId, cancellationToken);
            dbContext.EmployeeTimelineEvents.Add(new EmployeeTimelineEvent
            {
                EmployeeId = employee.Id,
                EventType = "ONBOARDING",
                Title = "Tiếp nhận nhân sự mới",
                Description = $"Tiếp nhận nhân sự vào phòng ban '{dept.Name}' với chức vụ '{pos?.Name ?? "Chưa bổ nhiệm"}'. Lương khởi điểm: {request.BaseSalary:N0} VNĐ, Phụ cấp chức vụ: {posAllowance:N0} VNĐ.",
                Timestamp = now,
                CreatedByUserId = actorUserId > 0 ? actorUserId : null,
                CreatedByName = actorName
            });

            await dbContext.SaveChangesAsync(cancellationToken);

            var hasCreatedAccount = false;
            if (request.CreateAccount)
            {
                var roleName = string.IsNullOrWhiteSpace(request.Role) ? "EMPLOYEE" : request.Role.Trim().ToUpperInvariant();
                var role = await dbContext.Roles.FirstOrDefaultAsync(r => r.Name == roleName, cancellationToken)
                    ?? await dbContext.Roles.FirstOrDefaultAsync(r => r.Name == "EMPLOYEE", cancellationToken)
                    ?? throw new DomainException("ROLE_NOT_FOUND", "Không tìm thấy vai trò hệ thống hợp lệ.");

                var nextUserId = (await dbContext.Users.IgnoreQueryFilters().MaxAsync(u => (long?)u.Id, cancellationToken) ?? 0) + 1;
                var user = new ApplicationUser
                {
                    EmployeeId = employee.Id,
                    RoleId = role.Id,
                    UserName = request.UserName!.Trim(),
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password!.Trim(), PasswordPolicy.BcryptWorkFactor),
                    IsActive = true,
                    Role = role,
                    Employee = employee
                };

                dbContext.Users.Add(user);
                await dbContext.SaveChangesAsync(cancellationToken);

                user.Metadata = new UserAccountMetadata { UserId = user.Id, User = user, CreatedAt = now };
                dbContext.UserAccountMetadata.Add(user.Metadata);
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

        var dept = await dbContext.Departments.FirstOrDefaultAsync(d => d.Id == request.DepartmentId, cancellationToken)
            ?? throw new DomainException("DEPARTMENT_NOT_FOUND", "Vui lòng chọn phòng ban hợp lệ.");

        // Kiểm tra quyền điều chuyển nếu có sự thay đổi phòng ban
        if (employee.DepartmentId != request.DepartmentId && !hasTransferPermission)
        {
            throw new DomainException("PERMISSION_DENIED", "Bạn không có quyền 'Điều chuyển phòng ban' (employee.transfer) để thay đổi phòng ban của nhân sự.");
        }

        Position? pos = null;
        if (request.PositionId.HasValue && request.PositionId.Value > 0)
        {
            pos = await dbContext.Positions.FirstOrDefaultAsync(p => p.Id == request.PositionId.Value, cancellationToken)
                ?? throw new DomainException("POSITION_NOT_FOUND", "Chức danh/vị trí công việc không tồn tại.");

            if (!DepartmentPositionPolicy.IsCompatible(dept.Id, pos.DepartmentId, dept.Code, dept.Name, pos.Name, pos.Code))
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
        var normalizedGender = GenderCatalog.Normalize(request.Gender);
        var effectiveHireDate = request.HireDate ?? employee.HireDate ?? DateOnly.FromDateTime(DateTime.Today);
        var actorName = await GetActorNameAsync(actorUserId, cancellationToken);

        // Kiểm tra biến động chức danh
        var newPositionId = request.PositionId > 0 ? request.PositionId : null;
        if (employee.PositionId != newPositionId)
        {
            var oldPosName = employee.Position?.Name ?? "Chưa bổ nhiệm";
            var newPosName = pos?.Name ?? "Chưa bổ nhiệm";
            dbContext.EmployeeTimelineEvents.Add(new EmployeeTimelineEvent
            {
                EmployeeId = employee.Id,
                EventType = "PROMOTION",
                Title = "Bổ nhiệm / Thay đổi chức danh",
                Description = $"Thay đổi chức danh từ '{oldPosName}' sang '{newPosName}'.",
                Timestamp = now,
                CreatedByUserId = actorUserId > 0 ? actorUserId : null,
                CreatedByName = actorName
            });
        }

        // Lưu vết lịch sử biến động lương nếu bất kỳ trường lương/phụ cấp nào thay đổi (chuẩn 3NF)
        var currentSalaryProfile = await dbContext.SalaryProfiles
            .Where(sp => sp.EmployeeId == employee.Id && sp.IsCurrent)
            .OrderByDescending(sp => sp.EffectiveDate)
            .FirstOrDefaultAsync(cancellationToken);

        var targetPosAllowance = Math.Max(0m, request.PositionAllowance);
        var targetOtherAllowance = Math.Max(0m, request.OtherAllowance);
        var targetInsuranceSalary = request.InsuranceSalary ?? request.BaseSalary;

        var isSalaryChanged = currentSalaryProfile is null ||
            currentSalaryProfile.BaseSalary != request.BaseSalary ||
            currentSalaryProfile.PositionAllowance != targetPosAllowance ||
            currentSalaryProfile.OtherAllowance != targetOtherAllowance ||
            currentSalaryProfile.InsuranceSalary != targetInsuranceSalary;

        if (isSalaryChanged)
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

            var note = string.IsNullOrWhiteSpace(request.SalaryChangeNote)
                ? "Cập nhật qua chỉnh sửa hồ sơ nhân sự"
                : request.SalaryChangeNote.Trim();

            dbContext.SalaryProfiles.Add(new EmployeeSalaryProfile
            {
                EmployeeId = employee.Id,
                BaseSalary = request.BaseSalary,
                PositionAllowance = targetPosAllowance,
                OtherAllowance = targetOtherAllowance,
                InsuranceSalary = targetInsuranceSalary,
                EffectiveDate = todayDate,
                IsCurrent = true,
                CreatedAt = now,
                Note = note
            });

            dbContext.EmployeeTimelineEvents.Add(new EmployeeTimelineEvent
            {
                EmployeeId = employee.Id,
                EventType = "SALARY_CHANGE",
                Title = "Điều chỉnh chế độ lương & đãi ngộ",
                Description = $"Lương cơ bản: {request.BaseSalary:N0} VNĐ, Phụ cấp CV: {targetPosAllowance:N0} VNĐ, Phụ cấp khác: {targetOtherAllowance:N0} VNĐ. Ghi chú: {note}",
                Timestamp = now,
                CreatedByUserId = actorUserId > 0 ? actorUserId : null,
                CreatedByName = actorName
            });
        }

        employee.FullName = request.FullName.Trim();
        employee.DepartmentId = request.DepartmentId;
        employee.ManagerId = request.ManagerId;
        employee.PositionId = newPositionId;
        employee.DateOfBirth = request.DateOfBirth;
        employee.Gender = normalizedGender;
        employee.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        employee.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        employee.Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim();
        employee.EducationLevel = normalizedEdu;
        employee.HireDate = effectiveHireDate;
        employee.DependentCount = Math.Max(0, request.DependentCount);
        employee.BankName = string.IsNullOrWhiteSpace(request.BankName) ? null : request.BankName.Trim();
        employee.BankAccountNumber = string.IsNullOrWhiteSpace(request.BankAccountNumber) ? null : request.BankAccountNumber.Trim();

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
                PositionAllowance = targetPosAllowance,
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

        Position? targetPos = null;
        if (request.TargetPositionId.HasValue && request.TargetPositionId.Value > 0)
        {
            targetPos = await dbContext.Positions.FirstOrDefaultAsync(p => p.Id == request.TargetPositionId.Value, cancellationToken)
                ?? throw new DomainException("POSITION_NOT_FOUND", "Chức danh/vị trí công việc mới không tồn tại.");

            if (!DepartmentPositionPolicy.IsCompatible(targetDepartment.Id, targetPos.DepartmentId, targetDepartment.Code, targetDepartment.Name, targetPos.Name, targetPos.Code))
            {
                throw new DomainException("INVALID_POSITION_FOR_DEPARTMENT",
                    $"Chức danh '{targetPos.Name}' không phù hợp với '{targetDepartment.Name}'. Vui lòng chọn chức vụ thuộc chuyên môn của phòng ban tiếp nhận.");
            }
        }

        var oldDepartmentName = employee.Department?.Name ?? "Chưa phân bổ";
        var oldPositionName = employee.Position?.Name ?? "Chưa phân chức vụ";
        var now = timeProvider.GetUtcNow();
        var actorName = await GetActorNameAsync(actorUserId, cancellationToken);

        employee.DepartmentId = request.TargetDepartmentId;
        if (request.TargetPositionId.HasValue && request.TargetPositionId.Value > 0)
        {
            employee.PositionId = request.TargetPositionId.Value;
        }

        dbContext.EmployeeTimelineEvents.Add(new EmployeeTimelineEvent
        {
            EmployeeId = employee.Id,
            EventType = "TRANSFER",
            Title = "Điều chuyển phòng ban / vị trí",
            Description = $"Điều chuyển từ '{oldDepartmentName}' ({oldPositionName}) sang '{targetDepartment.Name}' ({targetPos?.Name ?? oldPositionName}). Lý do: {request.Reason ?? "Theo quyết định tổ chức"}",
            Timestamp = now,
            CreatedByUserId = actorUserId > 0 ? actorUserId : null,
            CreatedByName = actorName
        });

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

    public async Task<EmployeeDto> PromoteEmployeeAsync(
        long id,
        PromoteEmployeeRequest request,
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
            throw new DomainException("INVALID_STATE", "Không thể thăng chức/bổ nhiệm cho nhân sự đã thôi việc.");
        }

        var targetPos = await dbContext.Positions
            .Include(p => p.Department)
            .FirstOrDefaultAsync(p => p.Id == request.TargetPositionId, cancellationToken)
            ?? throw new DomainException("POSITION_NOT_FOUND", "Chức danh/vị trí bổ nhiệm mới không tồn tại.");

        if (!DepartmentPositionPolicy.IsCompatible(employee.DepartmentId, targetPos.DepartmentId, employee.Department?.Code, employee.Department?.Name, targetPos.Name, targetPos.Code))
        {
            throw new DomainException("INVALID_POSITION_FOR_DEPARTMENT",
                $"Chức danh '{targetPos.Name}' không phù hợp với phòng ban '{employee.Department?.Name}'. Vui lòng chọn chức vụ thuộc chuyên môn của phòng ban hiện tại.");
        }

        // 1. Xác định cấp bậc và thẩm quyền của người thực hiện (Actor)
        var actorUser = await dbContext.Users
            .Include(u => u.Role)
            .Include(u => u.Employee)
                .ThenInclude(e => e!.Position)
            .Include(u => u.Employee)
                .ThenInclude(e => e!.Department)
            .FirstOrDefaultAsync(u => u.Id == actorUserId, cancellationToken);

        var isActorAdminOrBoard = actorUser?.Role?.Name == "ADMIN" ||
            string.Equals(actorUser?.Employee?.Department?.Code, "BGD", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(actorUser?.Employee?.Department?.Code, "BOD", StringComparison.OrdinalIgnoreCase) ||
            (actorUser?.Employee?.Department?.Name?.Contains("Ban Giám Đốc", StringComparison.OrdinalIgnoreCase) ?? false);

        var actorRank = isActorAdminOrBoard
            ? 5
            : PositionHierarchyPolicy.GetPositionRank(actorUser?.Employee?.Position?.Name, actorUser?.Employee?.Position?.Code);

        var currentTargetRank = PositionHierarchyPolicy.GetPositionRank(employee.Position?.Name, employee.Position?.Code);
        var newTargetRank = PositionHierarchyPolicy.GetPositionRank(targetPos.Name, targetPos.Code);
        var actorDeptId = actorUser?.Employee?.DepartmentId ?? 0;

        // 2. Kiểm tra thẩm quyền bổ nhiệm theo quy tắc phân cấp và phạm vi phòng ban
        if (!PositionHierarchyPolicy.ValidatePromotion(
            actorRank,
            isActorAdminOrBoard,
            actorDeptId,
            employee.DepartmentId,
            currentTargetRank,
            newTargetRank,
            out var validationError))
        {
            throw new DomainException("PROMOTION_DENIED", validationError!);
        }

        var strategy = dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            var oldPositionName = employee.Position?.Name ?? "Chưa phân chức vụ";
            var now = timeProvider.GetUtcNow();
            var actorName = await GetActorNameAsync(actorUserId, cancellationToken);
            var effectiveDate = request.EffectiveDate ?? DateOnly.FromDateTime(DateTime.Today);

            // Cập nhật chức danh mới
            employee.PositionId = targetPos.Id;

            // Đồng bộ hồ sơ lương & phụ cấp (nếu có cập nhật)
            var currentProfile = employee.SalaryProfiles.FirstOrDefault(sp => sp.IsCurrent);
            var targetBaseSalary = request.NewBaseSalary ?? currentProfile?.BaseSalary ?? 0m;
            var targetPositionAllowance = request.NewPositionAllowance ?? targetPos.DefaultPositionAllowance;
            var otherAllowance = currentProfile?.OtherAllowance ?? 0m;
            var insuranceSalary = currentProfile?.InsuranceSalary;

            var isSalaryChanged = currentProfile == null ||
                currentProfile.BaseSalary != targetBaseSalary ||
                currentProfile.PositionAllowance != targetPositionAllowance;

            if (isSalaryChanged)
            {
                if (currentProfile != null)
                {
                    currentProfile.IsCurrent = false;
                    currentProfile.ExpiryDate = effectiveDate;
                }

                dbContext.SalaryProfiles.Add(new EmployeeSalaryProfile
                {
                    EmployeeId = employee.Id,
                    BaseSalary = targetBaseSalary,
                    PositionAllowance = targetPositionAllowance,
                    OtherAllowance = otherAllowance,
                    InsuranceSalary = insuranceSalary,
                    EffectiveDate = effectiveDate,
                    IsCurrent = true,
                    CreatedAt = now,
                    Note = $"Điều chỉnh đãi ngộ theo quyết định thăng chức '{targetPos.Name}'. Căn cứ: {request.Reason ?? "Quyết định bổ nhiệm"}"
                });
            }

            // Đồng bộ Vai trò (Role) tài khoản nếu nhân sự đã có User
            var targetUser = await dbContext.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.EmployeeId == employee.Id, cancellationToken);

            string? assignedRoleName = null;
            if (targetUser != null)
            {
                var desiredRoleName = !string.IsNullOrWhiteSpace(request.NewRoleName)
                    ? request.NewRoleName.Trim().ToUpperInvariant()
                    : PositionHierarchyPolicy.SuggestRoleForPosition(targetPos.Name, employee.Department?.Code);

                var newRole = await dbContext.Roles.FirstOrDefaultAsync(r => r.Name == desiredRoleName, cancellationToken);
                if (newRole != null && targetUser.RoleId != newRole.Id)
                {
                    targetUser.RoleId = newRole.Id;
                    assignedRoleName = newRole.Name;
                }
            }

            // Ghi nhận sự kiện Timeline
            var roleSyncNotice = assignedRoleName != null ? $" (Đồng bộ Role hệ thống: {assignedRoleName})" : string.Empty;
            dbContext.EmployeeTimelineEvents.Add(new EmployeeTimelineEvent
            {
                EmployeeId = employee.Id,
                EventType = "PROMOTION",
                Title = $"Bổ nhiệm chức vụ: {targetPos.Name}",
                Description = $"Bổ nhiệm từ '{oldPositionName}' ({PositionHierarchyPolicy.GetRankDisplayName(currentTargetRank)}) lên '{targetPos.Name}' ({PositionHierarchyPolicy.GetRankDisplayName(newTargetRank)}){roleSyncNotice}. Căn cứ: {request.Reason ?? "Quyết định bổ nhiệm"}",
                Timestamp = now,
                CreatedByUserId = actorUserId > 0 ? actorUserId : null,
                CreatedByName = actorName
            });

            // Ghi nhận Audit Log
            dbContext.AuditLogs.Add(new AuditLogEntry
            {
                Id = Guid.NewGuid(),
                ActorUserId = actorUserId,
                Action = "employee.promoted",
                EntityType = "Employee",
                EntityId = employee.Id.ToString(CultureInfo.InvariantCulture),
                AfterJson = JsonSerializer.Serialize(new
                {
                    EmployeeCode = employee.Code,
                    EmployeeName = employee.FullName,
                    FromPosition = oldPositionName,
                    ToPosition = targetPos.Name,
                    NewRank = newTargetRank,
                    AssignedRole = assignedRoleName,
                    NewBaseSalary = targetBaseSalary,
                    NewPositionAllowance = targetPositionAllowance,
                    Reason = request.Reason,
                    EffectiveDate = effectiveDate
                }),
                CreatedAt = now
            });

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var saved = await dbContext.Employees
                .Include(e => e.Department)
                .Include(e => e.Position)
                .Include(e => e.SalaryProfiles)
                .FirstAsync(e => e.Id == employee.Id, cancellationToken);

            var hasAccount = targetUser != null;
            return MapToDto(saved, hasAccount);
        });
    }

    public async Task<IReadOnlyList<DepartmentDto>> GetDepartmentsAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Departments.AsNoTracking().AsQueryable();
        if (!includeInactive)
        {
            query = query.Where(d => d.IsActive);
        }

        var depts = await query.OrderBy(d => d.Code).ToListAsync(cancellationToken);
        var deptIds = depts.Select(d => d.Id).ToList();

        var counts = await dbContext.Employees
            .AsNoTracking()
            .Where(e => deptIds.Contains(e.DepartmentId) && !EF.Functions.ILike(e.Status, "RESIGNED"))
            .GroupBy(e => e.DepartmentId)
            .Select(g => new { DepartmentId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.DepartmentId, x => x.Count, cancellationToken);

        return depts.Select(d => new DepartmentDto(
            d.Id,
            d.Code,
            d.Name,
            d.Description,
            d.IsActive,
            counts.GetValueOrDefault(d.Id, 0))).ToList();
    }

    public async Task<DepartmentDto> CreateDepartmentAsync(CreateDepartmentRequest request, long actorUserId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            throw new DomainException("INVALID_CODE", "Mã phòng ban không được để trống.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new DomainException("INVALID_NAME", "Tên phòng ban không được để trống.");
        }

        var codeTrim = request.Code.Trim().ToUpperInvariant();
        if (await dbContext.Departments.AnyAsync(d => EF.Functions.ILike(d.Code, codeTrim), cancellationToken))
        {
            throw new DomainException("DEPARTMENT_CODE_EXISTS", $"Mã phòng ban '{codeTrim}' đã tồn tại.");
        }

        var now = timeProvider.GetUtcNow();
        var dept = new Department
        {
            Code = codeTrim,
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            IsActive = true
        };

        dbContext.Departments.Add(dept);
        await dbContext.SaveChangesAsync(cancellationToken);

        dbContext.AuditLogs.Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            Action = "department.created",
            EntityType = "Department",
            EntityId = dept.Id.ToString(CultureInfo.InvariantCulture),
            AfterJson = JsonSerializer.Serialize(new { dept.Code, dept.Name, dept.Description }),
            CreatedAt = now
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        return new DepartmentDto(dept.Id, dept.Code, dept.Name, dept.Description, dept.IsActive, 0);
    }

    public async Task<DepartmentDto> UpdateDepartmentAsync(long id, UpdateDepartmentRequest request, long actorUserId, CancellationToken cancellationToken = default)
    {
        var dept = await dbContext.Departments.FirstOrDefaultAsync(d => d.Id == id, cancellationToken)
            ?? throw new DomainException("DEPARTMENT_NOT_FOUND", "Không tìm thấy phòng ban.");

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            throw new DomainException("INVALID_CODE", "Mã phòng ban không được để trống.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new DomainException("INVALID_NAME", "Tên phòng ban không được để trống.");
        }

        var codeTrim = request.Code.Trim().ToUpperInvariant();
        if (await dbContext.Departments.AnyAsync(d => d.Id != id && EF.Functions.ILike(d.Code, codeTrim), cancellationToken))
        {
            throw new DomainException("DEPARTMENT_CODE_EXISTS", $"Mã phòng ban '{codeTrim}' đã tồn tại.");
        }

        if (!request.IsActive)
        {
            var hasActiveEmployees = await dbContext.Employees
                .AnyAsync(e => e.DepartmentId == id && EF.Functions.ILike(e.Status, "ACTIVE"), cancellationToken);

            if (hasActiveEmployees)
            {
                throw new DomainException("DEPARTMENT_IN_USE", "Không thể vô hiệu hóa phòng ban đang có nhân sự làm việc.");
            }
        }

        var now = timeProvider.GetUtcNow();
        dept.Code = codeTrim;
        dept.Name = request.Name.Trim();
        dept.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        dept.IsActive = request.IsActive;

        dbContext.AuditLogs.Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            Action = "department.updated",
            EntityType = "Department",
            EntityId = dept.Id.ToString(CultureInfo.InvariantCulture),
            AfterJson = JsonSerializer.Serialize(new { dept.Code, dept.Name, dept.Description, dept.IsActive }),
            CreatedAt = now
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        var employeeCount = await dbContext.Employees
            .CountAsync(e => e.DepartmentId == id && !EF.Functions.ILike(e.Status, "RESIGNED"), cancellationToken);

        return new DepartmentDto(dept.Id, dept.Code, dept.Name, dept.Description, dept.IsActive, employeeCount);
    }

    public async Task<IReadOnlyList<PositionDto>> GetPositionsAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Positions
            .Include(p => p.Department)
            .AsNoTracking()
            .AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(p => p.IsActive);
        }

        return await query
            .OrderBy(p => p.Code)
            .Select(p => new PositionDto(
                p.Id,
                p.Code,
                p.Name,
                p.DepartmentId,
                p.Department != null ? p.Department.Name : null,
                p.DefaultPositionAllowance,
                p.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<PositionDto> CreatePositionAsync(CreatePositionRequest request, long actorUserId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            throw new DomainException("INVALID_CODE", "Mã chức danh không được để trống.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new DomainException("INVALID_NAME", "Tên chức danh không được để trống.");
        }

        if (request.DefaultPositionAllowance < 0)
        {
            throw new DomainException("INVALID_ALLOWANCE", "Phụ cấp chức vụ mặc định phải lớn hơn hoặc bằng 0.");
        }

        var codeTrim = request.Code.Trim().ToUpperInvariant();
        if (await dbContext.Positions.AnyAsync(p => p.Code != null && EF.Functions.ILike(p.Code, codeTrim), cancellationToken))
        {
            throw new DomainException("POSITION_CODE_EXISTS", $"Mã chức danh '{codeTrim}' đã tồn tại.");
        }

        string? departmentName = null;
        if (request.DepartmentId.HasValue && request.DepartmentId.Value > 0)
        {
            var dept = await dbContext.Departments.FirstOrDefaultAsync(d => d.Id == request.DepartmentId.Value, cancellationToken)
                ?? throw new DomainException("DEPARTMENT_NOT_FOUND", "Phòng ban liên kết không tồn tại.");
            departmentName = dept.Name;
        }

        var now = timeProvider.GetUtcNow();
        var position = new Position
        {
            Code = codeTrim,
            Name = request.Name.Trim(),
            DepartmentId = request.DepartmentId > 0 ? request.DepartmentId : null,
            DefaultPositionAllowance = request.DefaultPositionAllowance,
            IsActive = true
        };

        dbContext.Positions.Add(position);
        await dbContext.SaveChangesAsync(cancellationToken);

        dbContext.AuditLogs.Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            Action = "position.created",
            EntityType = "Position",
            EntityId = position.Id.ToString(CultureInfo.InvariantCulture),
            AfterJson = JsonSerializer.Serialize(new { position.Code, position.Name, position.DepartmentId, position.DefaultPositionAllowance }),
            CreatedAt = now
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        return new PositionDto(
            position.Id,
            position.Code,
            position.Name,
            position.DepartmentId,
            departmentName,
            position.DefaultPositionAllowance,
            position.IsActive);
    }

    public async Task<PositionDto> UpdatePositionAsync(long id, UpdatePositionRequest request, long actorUserId, CancellationToken cancellationToken = default)
    {
        var position = await dbContext.Positions
            .Include(p => p.Department)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new DomainException("POSITION_NOT_FOUND", "Không tìm thấy chức danh.");

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            throw new DomainException("INVALID_CODE", "Mã chức danh không được để trống.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new DomainException("INVALID_NAME", "Tên chức danh không được để trống.");
        }

        if (request.DefaultPositionAllowance < 0)
        {
            throw new DomainException("INVALID_ALLOWANCE", "Phụ cấp chức vụ mặc định phải lớn hơn hoặc bằng 0.");
        }

        var codeTrim = request.Code.Trim().ToUpperInvariant();
        if (await dbContext.Positions.AnyAsync(p => p.Id != id && p.Code != null && EF.Functions.ILike(p.Code, codeTrim), cancellationToken))
        {
            throw new DomainException("POSITION_CODE_EXISTS", $"Mã chức danh '{codeTrim}' đã tồn tại.");
        }

        if (!request.IsActive)
        {
            var hasActiveEmployees = await dbContext.Employees
                .AnyAsync(e => e.PositionId == id && EF.Functions.ILike(e.Status, "ACTIVE"), cancellationToken);

            if (hasActiveEmployees)
            {
                throw new DomainException("POSITION_IN_USE", "Không thể vô hiệu hóa chức danh đang có nhân sự làm việc.");
            }
        }

        string? departmentName = null;
        if (request.DepartmentId.HasValue && request.DepartmentId.Value > 0)
        {
            var dept = await dbContext.Departments.FirstOrDefaultAsync(d => d.Id == request.DepartmentId.Value, cancellationToken)
                ?? throw new DomainException("DEPARTMENT_NOT_FOUND", "Phòng ban liên kết không tồn tại.");
            departmentName = dept.Name;
        }

        var now = timeProvider.GetUtcNow();
        position.Code = codeTrim;
        position.Name = request.Name.Trim();
        position.DepartmentId = request.DepartmentId > 0 ? request.DepartmentId : null;
        position.DefaultPositionAllowance = request.DefaultPositionAllowance;
        position.IsActive = request.IsActive;

        dbContext.AuditLogs.Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            Action = "position.updated",
            EntityType = "Position",
            EntityId = position.Id.ToString(CultureInfo.InvariantCulture),
            AfterJson = JsonSerializer.Serialize(new { position.Code, position.Name, position.DepartmentId, position.DefaultPositionAllowance, position.IsActive }),
            CreatedAt = now
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return new PositionDto(
            position.Id,
            position.Code,
            position.Name,
            position.DepartmentId,
            departmentName,
            position.DefaultPositionAllowance,
            position.IsActive);
    }

    public async Task<IReadOnlyList<SalaryProfileDto>> GetSalaryHistoryAsync(long employeeId, CancellationToken cancellationToken = default)
    {
        var profiles = await dbContext.SalaryProfiles
            .AsNoTracking()
            .Where(sp => sp.EmployeeId == employeeId)
            .OrderByDescending(sp => sp.IsCurrent)
            .ThenByDescending(sp => sp.EffectiveDate)
            .ToListAsync(cancellationToken);

        return profiles.Select(p => new SalaryProfileDto(
            p.Id,
            p.EmployeeId,
            p.BaseSalary,
            p.PositionAllowance,
            p.OtherAllowance,
            p.InsuranceSalary,
            p.EffectiveDate,
            p.ExpiryDate,
            p.IsCurrent,
            p.Note)).ToList();
    }

    public async Task<IReadOnlyList<EmployeeTimelineEventDto>> GetEmployeeTimelineAsync(long employeeId, CancellationToken cancellationToken = default)
    {
        var events = await dbContext.EmployeeTimelineEvents
            .AsNoTracking()
            .Where(t => t.EmployeeId == employeeId)
            .OrderByDescending(t => t.Timestamp)
            .ToListAsync(cancellationToken);

        return events.Select(t => new EmployeeTimelineEventDto(
            t.Id,
            t.EmployeeId,
            t.EventType,
            t.Title,
            t.Description,
            t.Timestamp,
            t.CreatedByUserId,
            t.CreatedByName)).ToList();
    }

    public async Task<IReadOnlyList<string>> GetAvailableRolesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Roles
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .Select(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    private async Task<string> GetActorNameAsync(long actorUserId, CancellationToken cancellationToken)
    {
        if (actorUserId <= 0) return "Hệ thống";

        var user = await dbContext.Users
            .Include(u => u.Employee)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == actorUserId, cancellationToken);

        if (user is null) return $"User #{actorUserId}";
        return !string.IsNullOrWhiteSpace(user.Employee?.FullName) ? user.Employee.FullName : user.UserName;
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
        var currentProfile = e.SalaryProfiles.FirstOrDefault(sp => sp.IsCurrent);
        var currentSalary = currentProfile?.BaseSalary ?? 0m;
        var posAllowance = currentProfile?.PositionAllowance ?? 0m;
        var otherAllowance = currentProfile?.OtherAllowance ?? 0m;
        var insSalary = currentProfile?.InsuranceSalary ?? currentSalary;

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
            PositionAllowance: includeSensitiveInfo ? posAllowance : 0m,
            OtherAllowance: includeSensitiveInfo ? otherAllowance : 0m,
            InsuranceSalary: includeSensitiveInfo ? insSalary : null,
            DependentCount: includeSensitiveInfo ? e.DependentCount : 0,
            BankName: includeSensitiveInfo ? e.BankName : null,
            BankAccountNumber: includeSensitiveInfo ? e.BankAccountNumber : null,
            ManagerId: e.ManagerId);
    }
}
