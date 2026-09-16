# Frontend Rules

Framework: Blazor WebAssembly
Language: C#

- Dùng Razor Components.
- Không gọi PostgreSQL trực tiếp.
- API calls qua typed HttpClient services.
- Tách layout theo Admin/HR/Warehouse/Procurement/Sales/Employee.
- Component không chứa business rule.
- Backend là authority cho authorization.
- Không hardcode secrets.
- Không đưa Supabase service-role key vào frontend.
