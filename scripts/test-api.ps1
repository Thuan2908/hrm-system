[System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }

$loginBody = '{"userName":"nv_anhtuan","password":"User@123"}'
try {
    $loginRes = Invoke-RestMethod -Uri "https://localhost:7060/api/v1/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
    $token = $loginRes.data.accessToken
    Write-Host "Roles: $($loginRes.data.user.roles -join ',')"
    Write-Host "Permissions: $($loginRes.data.user.permissions -join ',')"
    
    $attRes = Invoke-RestMethod -Uri "https://localhost:7060/api/v1/attendance/today" -Method Get -Headers @{ Authorization = "Bearer $token" }
    Write-Host "Success! Today Data:"
    $attRes | ConvertTo-Json -Depth 3 | Write-Host
} catch {
    Write-Host "Error: $_"
    if ($_.Exception.Response) {
        $stream = $_.Exception.Response.GetResponseStream()
        if ($stream) {
            $reader = New-Object System.IO.StreamReader($stream)
            Write-Host "Body: $($reader.ReadToEnd())"
        }
    }
}
