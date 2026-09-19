# Backend Structure

Backend dùng **ASP.NET Core Web API + C# + Entity Framework Core + PostgreSQL (Npgsql)**.

```text
apps/api/
├── Hrm.sln
├── src/
│   ├── Hrm.Api/
│   │   ├── Program.cs
│   │   ├── Middleware/
│   │   └── Extensions/
│   ├── Hrm.SharedKernel/
│   └── Modules/
│       ├── Auth/
│       ├── Employees/
│       ├── Departments/
│       ├── Positions/
│       ├── Attendance/
│       ├── Leave/
│       ├── Payroll/
│       ├── Reports/
│       ├── Files/
│       └── Audit/
└── tests/
    ├── Hrm.UnitTests/
    ├── Hrm.IntegrationTests/
    └── Hrm.E2ETests/
```

## Module structure

```text
Employees/
├── Domain/
├── Application/
├── Infrastructure/
│   ├── Persistence/
│   │   ├── EmployeeDbContext.cs
│   │   ├── Configurations/
│   │   └── Migrations/
│   └── Repositories/
└── Presentation/
    └── Controllers/
```

## Rules
- Controller chỉ xử lý HTTP concern.
- Application xử lý use case.
- Domain giữ business rule.
- Infrastructure xử lý EF Core, PostgreSQL qua Npgsql và external services.
- Không gọi `DbContext` của module khác.

## Retail Modules

Bổ sung:

```text
Modules/
├── Products/
├── Suppliers/
├── Warehouses/
├── Inventory/
├── Procurement/
└── Sales/
```

Cross-module rule:
- `Sales` gọi `IInventoryService`/contract để xuất kho.
- `Procurement` gọi `IInventoryService`/contract để nhập kho.
- Không module nào truy cập `InventoryDbContext` trực tiếp nếu không thuộc Inventory module.
