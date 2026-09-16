# .NET Requirements

Tài liệu này là danh mục dependency để một developer mới clone/pull repository có thể chuẩn bị môi trường và restore toàn bộ thư viện .NET của Saigon Retail Management System.

Phiên bản NuGet được quản lý tập trung trong `Directory.Packages.props`. Không ghi version trực tiếp trong từng `.csproj`.

## 1. Phần mềm bắt buộc

### .NET SDK

- .NET 10 SDK, dùng bản patch ổn định mới nhất tương thích với package catalog.
- Target framework của solution: `net10.0`.
- Kiểm tra cài đặt:

```powershell
dotnet --info
dotnet --list-sdks
```

Khi `global.json` được tạo ở Foundation milestone, SDK được chọn phải thỏa version/roll-forward trong file đó.

### Docker

Docker Desktop hoặc Docker Engine cần cho integration tests sử dụng PostgreSQL Testcontainers.

Kiểm tra:

```powershell
docker version
```

Không cần cài PostgreSQL local nếu integration tests dùng Testcontainers và môi trường dev dùng Supabase.

### NuGet source

Nguồn package mặc định:

```text
https://api.nuget.org/v3/index.json
```

Kiểm tra:

```powershell
dotnet nuget list source
```

Không commit credential của private feed vào repository.

## 2. Restore sau khi clone/pull

Khi solution và project files đã được scaffold, chạy tại repository root:

```powershell
dotnet tool restore
dotnet restore Hrm.slnx
dotnet build Hrm.slnx --no-restore -c Release
dotnet test --solution Hrm.slnx --no-build -c Release
```

Khi repository đã có NuGet lock files:

```powershell
dotnet restore --locked-mode
```

Không cần chạy `dotnet add package` cho từng máy. `PackageReference` trong các `.csproj` và version trong `Directory.Packages.props` là nguồn restore chính thức.

## 3. Central Package Management

File `Directory.Packages.props` bật:

```xml
<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
<CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
```

Project chỉ khai báo package cần sử dụng và không ghi version:

```xml
<ItemGroup>
  <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" />
</ItemGroup>
```

Không reference tất cả package vào mọi project. Mỗi layer/module chỉ được dùng dependency phù hợp với boundary của nó.

## 4. Backend packages

| Package | Version | Mục đích | Project dự kiến |
|---|---:|---|---|
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 10.0.12 | Xác thực JWT bearer cho API | `Hrm.Api` |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | 10.0.12 | User, role, password hashing, lockout và Identity persistence | Auth Infrastructure |
| `Microsoft.AspNetCore.OpenApi` | 10.0.12 | Sinh OpenAPI document | `Hrm.Api` |
| `System.IdentityModel.Tokens.Jwt` | 8.22.0 | Phát hành và xử lý JWT | Auth Application/Infrastructure |
| `FluentValidation` | 12.1.1 | Strongly typed request/use-case validation | Application modules |
| `FluentValidation.DependencyInjectionExtensions` | 12.1.1 | Đăng ký validators qua DI | API composition root |

Không dùng `FluentValidation.AspNetCore` vì package integration cũ không cần thiết; validators được gọi qua pipeline/application integration do solution kiểm soát.

## 5. Persistence packages

| Package | Version | Mục đích | Project dự kiến |
|---|---:|---|---|
| `Microsoft.EntityFrameworkCore` | 10.0.12 | ORM core | Module Infrastructure |
| `Microsoft.EntityFrameworkCore.Design` | 10.0.12 | Design-time migration services | Migration host/API, `PrivateAssets=all` |
| `Microsoft.EntityFrameworkCore.Tools` | 10.0.12 | Visual Studio Package Manager tooling | Migration host, `PrivateAssets=all` |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 10.0.3 | EF Core provider cho PostgreSQL/Supabase | Module Infrastructure/API |
| `EFCore.NamingConventions` | 10.0.1 | `snake_case` naming convention cho PostgreSQL | Persistence configuration |
| `AspNetCore.HealthChecks.NpgSql` | 9.0.0 | PostgreSQL readiness health check | `Hrm.Api` |

Ví dụ reference design-time an toàn:

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.Design">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
```

`SUPABASE_ANON_KEY` không phải database credential. EF Core kết nối bằng backend-only `ConnectionStrings:DefaultConnection`.

Không cài Supabase .NET client chỉ để EF Core kết nối database. Npgsql là provider bắt buộc.

## 6. Frontend packages

| Package | Version | Mục đích | Project dự kiến |
|---|---:|---|---|
| `Microsoft.AspNetCore.Components.Authorization` | 10.0.12 | Authorization state và route authorization cho Blazor | `Hrm.Web` |
| `Microsoft.AspNetCore.Components.Web` | 10.0.12 | Razor component primitives cho shared UI library | `Hrm.UI` |
| `Microsoft.AspNetCore.Components.WebAssembly` | 10.0.12 | Blazor WebAssembly runtime | `Hrm.Web` |
| `Microsoft.AspNetCore.Components.WebAssembly.DevServer` | 10.0.12 | Local development server | `Hrm.Web`, `PrivateAssets=all` khi phù hợp |

Không đưa database provider, EF Core, database password, service-role key hoặc JWT signing key vào frontend project.

Không thêm UI component framework trước khi có quyết định UI/design system. Blazor built-in components là baseline.

## 7. Observability packages

| Package | Version | Mục đích | Project dự kiến |
|---|---:|---|---|
| `OpenTelemetry.Extensions.Hosting` | 1.18.0 | Tích hợp OpenTelemetry với host/DI | `Hrm.Api` |
| `OpenTelemetry.Exporter.OpenTelemetryProtocol` | 1.18.0 | Gửi telemetry qua OTLP | `Hrm.Api` |
| `OpenTelemetry.Instrumentation.AspNetCore` | 1.18.0 | HTTP server traces/metrics | `Hrm.Api` |
| `OpenTelemetry.Instrumentation.Http` | 1.18.0 | Outgoing HTTP traces | API/external integration host |
| `OpenTelemetry.Instrumentation.Runtime` | 1.18.0 | .NET runtime metrics | `Hrm.Api` |

Exporter phải được bật bằng configuration. Không hard-code endpoint hoặc credential observability.

## 8. Data migration packages

| Package | Version | Mục đích | Project dự kiến |
|---|---:|---|---|
| `ClosedXML` | 0.105.1 | Đọc/ghi Excel `.xlsx` cho migration/export | Data Migration tool |
| `CsvHelper` | 33.1.0 | CSV import/export và bank export phù hợp | Data Migration/Payroll export |

Không reference hai package này vào Domain projects.

## 9. Testing packages

| Package | Version | Mục đích | Project dự kiến |
|---|---:|---|---|
| `Microsoft.NET.Test.Sdk` | 18.10.0 | Test host | Tất cả test projects |
| `xunit.v3` | 4.0.0 | Unit/integration test framework | Tất cả test projects |
| `xunit.runner.visualstudio` | 4.0.0 | Visual Studio/Test Explorer runner | Tất cả test projects |
| `coverlet.collector` | 10.0.1 | Code coverage collection | Tất cả test projects |
| `Microsoft.AspNetCore.Mvc.Testing` | 10.0.12 | API integration host/WebApplicationFactory | Integration tests |
| `Testcontainers.PostgreSql` | 4.15.0 | PostgreSQL thật trong Docker cho test | Integration tests |
| `bunit` | 2.9.0 | Blazor component tests | Component tests |
| `Microsoft.Playwright` | 1.62.0 | Browser E2E tests | E2E tests |
| `NetArchTest.Rules` | 1.3.2 | Kiểm tra modular-monolith boundaries | Architecture tests |

Test package references nên dùng private assets khi không cần truyền transitively:

```xml
<PackageReference Include="coverlet.collector">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>

<PackageReference Include="xunit.runner.visualstudio">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
```

Sau lần build đầu tiên của E2E project, cài browser binaries theo script Playwright sinh trong output directory, ví dụ:

```powershell
pwsh tests/Hrm.E2ETests/bin/Debug/net10.0/playwright.ps1 install --with-deps chromium
```

Đường dẫn thực tế phụ thuộc configuration và project output.

## 10. Local .NET tools

Foundation milestone phải tạo `.config/dotnet-tools.json` và pin ít nhất:

| Tool | Version policy | Mục đích |
|---|---|---|
| `dotnet-ef` | Cùng patch line với EF Core 10 | Tạo/list/script migrations và update database |

Khởi tạo tool manifest nếu chưa có:

```powershell
dotnet new tool-manifest
dotnet tool install dotnet-ef --version 10.0.12
```

Máy clone mới chỉ cần:

```powershell
dotnet tool restore
```

Không yêu cầu cài `dotnet-ef` global.

## 11. Package mapping mẫu

### API host

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" />
  <PackageReference Include="Microsoft.AspNetCore.OpenApi" />
  <PackageReference Include="FluentValidation.DependencyInjectionExtensions" />
  <PackageReference Include="AspNetCore.HealthChecks.NpgSql" />
  <PackageReference Include="OpenTelemetry.Extensions.Hosting" />
  <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" />
  <PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" />
  <PackageReference Include="OpenTelemetry.Instrumentation.Http" />
  <PackageReference Include="OpenTelemetry.Instrumentation.Runtime" />
</ItemGroup>
```

### Persistence/Infrastructure project

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.EntityFrameworkCore" />
  <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" />
  <PackageReference Include="EFCore.NamingConventions" />
</ItemGroup>
```

### Blazor WebAssembly project

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.AspNetCore.Components.Authorization" />
  <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly" />
  <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly.DevServer" PrivateAssets="all" />
</ItemGroup>
```

### Integration test project

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.NET.Test.Sdk" />
  <PackageReference Include="xunit.v3" />
  <PackageReference Include="xunit.runner.visualstudio" PrivateAssets="all" />
  <PackageReference Include="coverlet.collector" PrivateAssets="all" />
  <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" />
  <PackageReference Include="Testcontainers.PostgreSql" />
</ItemGroup>
```

## 12. Dependency rules

- Không thêm package chỉ vì phổ biến; phải gắn với task/use case cụ thể.
- Không dùng AutoMapper hoặc MediatR mặc định. Chỉ thêm qua quyết định có lý do rõ nếu complexity thực tế cần.
- Không dùng EF Core InMemory để thay PostgreSQL integration tests.
- Không để Domain project phụ thuộc EF Core, ASP.NET Core, Blazor hoặc file-processing packages.
- Không để frontend phụ thuộc persistence packages.
- Không reference test packages từ production projects.
- Review license và security advisory trước khi thêm dependency mới.
- Chạy vulnerability scan định kỳ:

```powershell
dotnet list package --vulnerable --include-transitive
dotnet list package --outdated
```

- Mọi thay đổi version phải sửa `Directory.Packages.props`, restore, build và chạy test trước khi merge.

## 13. Cấu hình không được commit

Không đặt các giá trị sau trong requirements, `.csproj`, `Directory.Packages.props`, source hoặc Blazor assets:

- Supabase PostgreSQL password.
- Production connection string.
- Supabase service-role key.
- JWT signing key.
- Access token hoặc refresh token.
- Private NuGet feed credential.

Local development dùng `dotnet user-secrets`:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<Supabase PostgreSQL connection string>" --project apps/api/Hrm.Api
dotnet user-secrets set "Jwt:SigningKey" "<local development secret>" --project apps/api/Hrm.Api
```

## 14. Trạng thái hiện tại

Repository hiện vẫn là documentation baseline và chưa có `.slnx`/`.csproj`, vì vậy `dotnet restore` chưa thể tải package ngay lúc tài liệu này được tạo.

`Directory.Packages.props` đã là package/version catalog chính thức. Khi Foundation milestone scaffold project files và thêm versionless `PackageReference`, developer mới pull về chỉ cần cài .NET 10 SDK, Docker và chạy các lệnh restore ở mục 2.
