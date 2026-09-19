# Database Design Baseline

## Tables từ PRD
- Departments
- Positions
- Employees
- Users
- Roles
- Permissions
- RolePermissions
- TimeAttendances
- LeaveRequests
- Payrolls

## Bổ sung kỹ thuật đề xuất
- RefreshTokens
- AuditLogs
- Attachments
- PayrollAdjustments
- ApprovalHistories
- BackupJobs

## Lưu ý
Các bảng bổ sung là quyết định triển khai, không phải nội dung nguyên bản của PRD.

## PostgreSQL / Supabase implementation notes
- .NET `Guid` maps to PostgreSQL `uuid` through Npgsql.
- EF Core provider is Npgsql.
- Use PostgreSQL indexes/constraints via EF Core configuration and migrations.
- Supabase API/Auth/Storage are optional platform capabilities and require explicit tasks/ADRs before adoption.
