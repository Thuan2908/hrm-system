# Frontend Structure

Frontend dùng **Blazor WebAssembly + C#**.

```text
apps/web/
├── Hrm.Web.csproj
├── Program.cs
├── App.razor
├── Routes.razor
├── Layout/
│   ├── AdminLayout.razor
│   ├── HrLayout.razor
│   ├── WarehouseLayout.razor
│   ├── ProcurementLayout.razor
│   ├── SalesLayout.razor
│   └── EmployeeLayout.razor
├── Pages/
│   ├── Admin/
│   ├── Hr/
│   ├── Warehouse/
│   ├── Procurement/
│   ├── Sales/
│   └── Employee/
├── Features/
├── Services/
│   └── ApiClient/
├── Authorization/
└── Shared/
```

Flow:

```text
Razor Component
    ↓
Typed API Service
    ↓
ASP.NET Core API
```

Không truy cập Supabase PostgreSQL trực tiếp từ Razor component.
