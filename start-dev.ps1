# start-dev.ps1 - builds the solution, creates C:\tmp, starts all three services and opens the portal.
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

# Folder used by PersistKeysToFileSystem("/tmp/vulindlela-keys") - resolves to C:\tmp on Windows
New-Item -ItemType Directory -Force -Path 'C:\tmp' | Out-Null

# Build once so each service starts quickly
dotnet build (Join-Path $root 'Vulindlela.slnx')
if ($LASTEXITCODE -ne 0) { throw 'Build failed. Fix the errors above and run again.' }

$services = @(
    @{ Name = 'Booking';   Project = 'src\Vulindlela.Booking.Api';   Port = 5101 },
    @{ Name = 'Clearance'; Project = 'src\Vulindlela.Clearance.Api'; Port = 5102 },
    @{ Name = 'Portal';    Project = 'src\Vulindlela.Portal';        Port = 5100 }
)

foreach ($s in $services) {
    $path = Join-Path $root $s.Project
    Start-Process -FilePath 'dotnet' -ArgumentList @('run', '--no-build', '--project', $path) -WindowStyle Minimized
    Write-Host "Started $($s.Name) on port $($s.Port)"
}

# Wait until every port is listening (up to 60 seconds)
$deadline = (Get-Date).AddSeconds(60)
foreach ($s in $services) {
    while (-not (Test-NetConnection -ComputerName localhost -Port $s.Port -InformationLevel Quiet -WarningAction SilentlyContinue)) {
        if ((Get-Date) -gt $deadline) { throw "$($s.Name) did not start on port $($s.Port). Check its window for errors." }
        Start-Sleep -Seconds 2
    }
    Write-Host "$($s.Name) is listening on port $($s.Port)"
}

# Open the sign-in page for testing
Start-Process 'http://localhost:5100/Account/SignIn'
Write-Host '-----'
Write-Host '          Test sign-in: thabo / Passw0rd!  (also naledi, sipho; official / Passw0rd!)' -Fore Green
Write-Host '          To stop everything: Get-Process Vulindlela* | Stop-Process -Force' -Fore Green
Write-Host '-----'