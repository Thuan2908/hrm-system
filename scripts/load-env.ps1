$environmentFile = Join-Path $PSScriptRoot "..\.env"

if (-not (Test-Path -LiteralPath $environmentFile)) {
    throw "Missing local environment file: $environmentFile"
}

foreach ($line in Get-Content -LiteralPath $environmentFile) {
    $trimmedLine = $line.Trim()

    if ([string]::IsNullOrWhiteSpace($trimmedLine) -or $trimmedLine.StartsWith('#')) {
        continue
    }

    $separatorIndex = $trimmedLine.IndexOf('=')
    if ($separatorIndex -le 0) {
        throw "Invalid .env entry. Expected KEY=VALUE."
    }

    $name = $trimmedLine.Substring(0, $separatorIndex).Trim()
    $value = $trimmedLine.Substring($separatorIndex + 1).Trim()
    [Environment]::SetEnvironmentVariable($name, $value, 'Process')
}

Write-Host "Loaded local environment variables from .env for this PowerShell process."
