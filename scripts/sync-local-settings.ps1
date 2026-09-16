$ErrorActionPreference = "Stop"
$environmentFile = Join-Path $PSScriptRoot "..\.env"

if (-not (Test-Path -LiteralPath $environmentFile)) {
    throw "Missing .env file at $environmentFile"
}

$conn = ""
$jwt = ""

foreach ($line in Get-Content -LiteralPath $environmentFile) {
    $trimmed = $line.Trim()
    if ($trimmed.StartsWith("ConnectionStrings__DefaultConnection=")) {
        $conn = $trimmed.Substring("ConnectionStrings__DefaultConnection=".Length).Trim()
    }
    elseif ($trimmed.StartsWith("Jwt__SigningKey=")) {
        $jwt = $trimmed.Substring("Jwt__SigningKey=".Length).Trim()
    }
}

if ([string]::IsNullOrWhiteSpace($conn) -or [string]::IsNullOrWhiteSpace($jwt)) {
    throw "ConnectionStrings__DefaultConnection or Jwt__SigningKey not found in .env"
}

$localSettings = [ordered]@{
    ConnectionStrings = [ordered]@{
        DefaultConnection = $conn
    }
    Jwt = [ordered]@{
        SigningKey = $jwt
    }
}

$outputPath = Join-Path $PSScriptRoot "..\apps\api\Hrm.Api\appsettings.Development.Local.json"
$localSettings | ConvertTo-Json -Depth 4 | Set-Content -Path $outputPath -Encoding utf8
Write-Host "Created $outputPath successfully."
