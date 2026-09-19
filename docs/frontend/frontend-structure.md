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
│   └── EmployeeLayout.razor
├── Pages/
│   ├── Admin/
│   ├── Hr/
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
