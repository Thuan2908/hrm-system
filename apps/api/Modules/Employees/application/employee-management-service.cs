using Hrm.Contracts;
using Hrm.Modules.Employees.Domain;
using Hrm.Modules.Employees.Infrastructure.Persistence;
using Hrm.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hrm.Modules.Employees.Application;

public interface IEmployeeManagementService
{
    Task<PagedResult<EmployeeDto>> SearchEmployeesAsync(
        string? keyword,
        long? departmentId,
        long? positionId,
        string? status,
        string? sortBy,
        bool descending,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<EmployeeDto?> GetEmployeeByIdAsync(long id, CancellationToken cancellationToken);
    Task<EmployeeDto> CreateEmployeeAsync(CreateEmployeeRequest request, long? actorUserId, CancellationToken cancellationToken);
    Task<EmployeeDto> UpdateEmployeeAsync(long id, UpdateEmployeeRequest request, long? actorUserId, CancellationToken cancellationToken);
    Task TransferEmployeeAsync(long id, TransferEmployeeRequest request, long? actorUserId, CancellationToken cancellationToken);
    Task PromoteEmployeeAsync(long id, PromoteEmployeeRequest request, long? actorUserId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<EmployeeTimelineEventDto>> GetEmployeeTimelineAsync(long id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<DepartmentDto>> GetDepartmentsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<PositionDto>> GetPositionsAsync(CancellationToken cancellationToken);
}

public sealed class EmployeeManagementService(EmployeesDbContext dbContext) : IEmployeeManagementService
{
    public async Task<IReadOnlyCollection<DepartmentDto>> GetDepartmentsAsync(CancellationToken cancellationToken)
    {
        var entities = await dbContext.Departments
            .AsNoTracking()
            .OrderBy(d => d.Code)
            .ToListAsync(cancellationToken);

        return entities.Select(d => new DepartmentDto(
            d.Id,
            d.Code ?? $"DEPT{d.Id:D3}",
            d.Name ?? "Chưa đặt tên",
            d.Description,
            d.IsActive,
            d.CreatedAt
        )).ToList();
    }

    public async Task<IReadOnlyCollection<PositionDto>> GetPositionsAsync(CancellationToken cancellationToken)
    {
        var entities = await dbContext.Positions
            .AsNoTracking()
            .OrderBy(p => p.Code)
            .ToListAsync(cancellationToken);

        return entities.Select(p => new PositionDto(
            p.Id,
            p.Code ?? $"POS{p.Id:D3}",
            p.Title ?? "Chưa đặt tên",
            p.BaseSalary,
            p.Allowance,
            p.Description,
            p.IsActive,
            p.CreatedAt
        )).ToList();
    }

    public async Task<PagedResult<EmployeeDto>> SearchEmployeesAsync(
        string? keyword,
        long? departmentId,
        long? positionId,
        string? status,
        string? sortBy,
        bool descending,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .Include(e => e.Position)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var pattern = $"%{keyword.Trim()}%";
            query = query.Where(e =>
                (e.Code != null && EF.Functions.ILike(e.Code, pattern)) ||
                (e.FullName != null && EF.Functions.ILike(e.FullName, pattern)) ||
                (e.Email != null && EF.Functions.ILike(e.Email, pattern)) ||
                (e.Phone != null && EF.Functions.ILike(e.Phone, pattern)));
        }

        if (departmentId.HasValue && departmentId.Value > 0)
        {
            query = query.Where(e => e.DepartmentId == departmentId.Value);
        }

        if (positionId.HasValue && positionId.Value > 0)
        {
            query = query.Where(e => e.PositionId == positionId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var targetStatus = status.Trim().ToUpperInvariant();
            query = query.Where(e => e.Status != null && e.Status == targetStatus);
        }

        query = sortBy?.ToUpperInvariant() switch
        {
            "NAME" => descending ? query.OrderByDescending(e => e.FullName) : query.OrderBy(e => e.FullName),
            "CODE" => descending ? query.OrderByDescending(e => e.Code) : query.OrderBy(e => e.Code),
            "HIREDATE" => descending ? query.OrderByDescending(e => e.HireDate) : query.OrderBy(e => e.HireDate),
            _ => descending ? query.OrderByDescending(e => e.CreatedAt) : query.OrderBy(e => e.CreatedAt)
        };

        var total = await query.CountAsync(cancellationToken);
        var pageNumber = Math.Max(1, page);
        var size = Math.Clamp(pageSize, 1, 100);

        var items = await query
            .Skip((pageNumber - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(MapToDto).ToList();
        return new PagedResult<EmployeeDto>(dtos, pageNumber, size, total);
    }

    public async Task<EmployeeDto?> GetEmployeeByIdAsync(long id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .Include(e => e.Position)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        return entity is null ? null : MapToDto(entity);
    }

    public async Task<EmployeeDto> CreateEmployeeAsync(CreateEmployeeRequest request, long? actorUserId, CancellationToken cancellationToken)
    {
        var count = await dbContext.Employees.CountAsync(cancellationToken);
        var nextCode = $"EMP{(count + 1):D5}";

        var department = await dbContext.Departments.FindAsync([request.DepartmentId], cancellationToken)
            ?? throw new DomainException("DEPARTMENT_NOT_FOUND", "Phòng ban được chọn không tồn tại.");

        var position = await dbContext.Positions.FindAsync([request.PositionId], cancellationToken)
            ?? throw new DomainException("POSITION_NOT_FOUND", "Chức danh được chọn không tồn tại.");

        var hireDateUtc = DateTime.SpecifyKind(request.HireDate.Date, DateTimeKind.Utc);
        var entity = new EmployeeEntity
        {
            Code = nextCode,
            FullName = request.FullName.Trim(),
            DepartmentId = request.DepartmentId,
            Department = department,
            PositionId = request.PositionId,
            Position = position,
            Email = request.Email.Trim().ToLowerInvariant(),
            Phone = request.Phone.Trim(),
            Address = request.Address?.Trim(),
            HireDate = hireDateUtc,
            JoinDate = hireDateUtc,
            BaseSalary = position.BaseSalary,
            Status = "ACTIVE",
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Employees.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        dbContext.TimelineEvents.Add(new EmployeeTimelineEvent
        {
            EmployeeId = entity.Id,
            EventType = "ONBOARDED",
            Title = "Tiếp nhận nhân sự mới",
            Description = $"Nhân sự {entity.FullName} ({entity.Code}) được tiếp nhận vào {department.Name} với chức danh {position.Title}.",
            Timestamp = DateTimeOffset.UtcNow,
            CreatedByUserId = actorUserId,
            CreatedByName = "Hệ thống / HR"
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return MapToDto(entity);
    }

    public async Task<EmployeeDto> UpdateEmployeeAsync(long id, UpdateEmployeeRequest request, long? actorUserId, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Employees
            .Include(e => e.Department)
            .Include(e => e.Position)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
            ?? throw new DomainException("EMPLOYEE_NOT_FOUND", $"Không tìm thấy nhân viên có ID {id}.");

        var hireDateUtc = DateTime.SpecifyKind(request.HireDate.Date, DateTimeKind.Utc);
        entity.FullName = request.FullName.Trim();
        entity.DepartmentId = request.DepartmentId;
        entity.PositionId = request.PositionId;
        entity.Email = request.Email.Trim().ToLowerInvariant();
        entity.Phone = request.Phone.Trim();
        entity.Address = request.Address?.Trim();
        entity.HireDate = hireDateUtc;
        entity.JoinDate = hireDateUtc;
        if (entity.Position != null && entity.Position.BaseSalary > 0)
        {
            entity.BaseSalary = entity.Position.BaseSalary;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return MapToDto(entity);
    }

    public async Task TransferEmployeeAsync(long id, TransferEmployeeRequest request, long? actorUserId, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Employees
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
            ?? throw new DomainException("EMPLOYEE_NOT_FOUND", $"Không tìm thấy nhân viên có ID {id}.");

        var oldDeptName = entity.Department?.Name ?? "N/A";
        var targetDept = await dbContext.Departments.FindAsync([request.TargetDepartmentId], cancellationToken)
            ?? throw new DomainException("DEPARTMENT_NOT_FOUND", "Phòng ban đích không tồn tại.");

        entity.DepartmentId = targetDept.Id;
        entity.Department = targetDept;

        dbContext.TimelineEvents.Add(new EmployeeTimelineEvent
        {
            EmployeeId = entity.Id,
            EventType = "TRANSFERRED",
            Title = "Điều chuyển phòng ban",
            Description = $"Điều chuyển từ '{oldDeptName}' sang '{targetDept.Name}'. Lý do: {request.Reason}",
            Timestamp = DateTimeOffset.UtcNow,
            CreatedByUserId = actorUserId,
            CreatedByName = "HR Manager"
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task PromoteEmployeeAsync(long id, PromoteEmployeeRequest request, long? actorUserId, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Employees
            .Include(e => e.Position)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
            ?? throw new DomainException("EMPLOYEE_NOT_FOUND", $"Không tìm thấy nhân viên có ID {id}.");

        var oldPosTitle = entity.Position?.Title ?? "N/A";
        var targetPos = await dbContext.Positions.FindAsync([request.TargetPositionId], cancellationToken)
            ?? throw new DomainException("POSITION_NOT_FOUND", "Chức danh đích không tồn tại.");

        entity.PositionId = targetPos.Id;
        entity.Position = targetPos;

        dbContext.TimelineEvents.Add(new EmployeeTimelineEvent
        {
            EmployeeId = entity.Id,
            EventType = "PROMOTED",
            Title = "Bổ nhiệm / Thay đổi chức danh",
            Description = $"Thay đổi chức danh từ '{oldPosTitle}' sang '{targetPos.Title}'. Lý do: {request.Reason}",
            Timestamp = DateTimeOffset.UtcNow,
            CreatedByUserId = actorUserId,
            CreatedByName = "HR Manager"
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<EmployeeTimelineEventDto>> GetEmployeeTimelineAsync(long id, CancellationToken cancellationToken)
    {
        var events = await dbContext.TimelineEvents
            .AsNoTracking()
            .Where(t => t.EmployeeId == id)
            .OrderByDescending(t => t.Timestamp)
            .ToListAsync(cancellationToken);

        return events.Select(t => new EmployeeTimelineEventDto(
            t.Id,
            t.EmployeeId,
            t.EventType,
            t.Title,
            t.Description,
            t.Timestamp,
            t.CreatedByName
        )).ToList();
    }

    private static EmployeeDto MapToDto(EmployeeEntity e)
    {
        var code = e.Code ?? $"EMP{e.Id:D5}";
        return new(
            e.Id,
            code,
            e.FullName ?? "N/A",
            e.DepartmentId ?? 0,
            e.Department?.Name ?? "Chưa phân công",
            e.PositionId ?? 0,
            e.Position?.Title ?? "Chưa bổ nhiệm",
            e.Email ?? $"{code.ToLowerInvariant()}@saigonretail.vn",
            e.Phone ?? "",
            e.Address,
            e.HireDate ?? DateTime.Today,
            e.Status ?? "ACTIVE",
            e.Department?.Code,
            e.Position?.Code,
            e.CreatedAt
        );
    }
}
