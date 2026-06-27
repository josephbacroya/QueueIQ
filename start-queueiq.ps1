$ErrorActionPreference = "Stop"

Write-Host "===========================================" -ForegroundColor Cyan
Write-Host "      Starting QueueIQ Local Dev           " -ForegroundColor Cyan
Write-Host "===========================================" -ForegroundColor Cyan
Write-Host "Press Ctrl+C to stop both applications gracefully." -ForegroundColor Yellow
Write-Host ""

# Jobs list to keep track of background tasks
$jobs = @()

# Function to start a dotnet project and prefix its output
function Start-Project {
    param(
        [string]$ProjectName,
        [string]$ProjectPath,
        [ConsoleColor]$PrefixColor
    )

    $startInfo = New-Object System.Diagnostics.ProcessStartInfo
    $startInfo.FileName = "dotnet"
    $startInfo.Arguments = "run --project $ProjectPath"
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true

    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $startInfo

    $job = [PSCustomObject]@{
        Name = $ProjectName
        Process = $process
        Color = $PrefixColor
    }
    
    $process.Start() | Out-Null
    
    # We use a runspace or simple read loop to print the output asynchronously,
    # but for simplicity in a PS script, we'll just track the process objects
    # and read from their streams in a main loop.
    return $job
}

$apiJob = Start-Project -ProjectName "API" -ProjectPath "src\QueueIQ.Api\QueueIQ.Api.csproj" -PrefixColor Magenta
$webJob = Start-Project -ProjectName "WEB" -ProjectPath "src\QueueIQ.Web\QueueIQ.Web.csproj" -PrefixColor Green

$jobs += $apiJob
$jobs += $webJob

# Main execution block
try {
    # Infinite loop to read output and keep script alive
    while ($true) {
        $anyActive = $false
        
        foreach ($job in $jobs) {
            if (!$job.Process.HasExited) {
                $anyActive = $true
                
                # Read Output
                while ($job.Process.StandardOutput.Peek() -gt -1) {
                    $line = $job.Process.StandardOutput.ReadLine()
                    Write-Host "[$($job.Name)] " -ForegroundColor $job.Color -NoNewline
                    Write-Host $line
                }
                
                # Read Error
                while ($job.Process.StandardError.Peek() -gt -1) {
                    $line = $job.Process.StandardError.ReadLine()
                    Write-Host "[$($job.Name) ERR] " -ForegroundColor Red -NoNewline
                    Write-Host $line -ForegroundColor Red
                }
            }
        }
        
        if (!$anyActive) {
            Write-Host "All processes have exited." -ForegroundColor Yellow
            break
        }
        
        Start-Sleep -Milliseconds 100
    }
}
finally {
    Write-Host ""
    Write-Host "Shutting down QueueIQ..." -ForegroundColor Cyan
    foreach ($job in $jobs) {
        if (!$job.Process.HasExited) {
            Write-Host "Stopping $($job.Name) (PID: $($job.Process.Id))..." -ForegroundColor Yellow
            Stop-Process -Id $job.Process.Id -Force -ErrorAction SilentlyContinue
        }
    }
    Write-Host "Shutdown complete." -ForegroundColor Green
}
