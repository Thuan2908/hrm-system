$ErrorActionPreference = 'Stop'

$projects = @(
    'tests/Hrm.UnitTests/Hrm.UnitTests.csproj',
    'tests/Hrm.ArchitectureTests/Hrm.ArchitectureTests.csproj',
    'tests/Hrm.ComponentTests/Hrm.ComponentTests.csproj',
    'tests/Hrm.IntegrationTests/Hrm.IntegrationTests.csproj',
    'tests/Hrm.E2ETests/Hrm.E2ETests.csproj'
)

foreach ($project in $projects) {
    dotnet test --project $project --no-restore
    if ($LASTEXITCODE -ne 0) {
        throw "Tests failed for $project."
    }
}
