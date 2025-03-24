# Simple Memory Monitor Script
Write-Host "Starting memory monitoring for FatFullVersion program..." -ForegroundColor Green

# Specify program path
$exePath = "D:\GIT\Git\code\FAT_TEST\FAT_TEST\FatFullVersion\FatFullVersion\bin\Debug\net8.0-windows7.0\FatFullVersion.exe"

# Start program
Start-Process -FilePath $exePath
Start-Sleep -Seconds 2

# Get process ID
$process = Get-Process -Name "FatFullVersion" -ErrorAction SilentlyContinue

if ($null -eq $process) {
    Write-Host "Could not find FatFullVersion process, make sure the program is running" -ForegroundColor Red
    exit
}

Write-Host "Monitoring process ID: $($process.Id)" -ForegroundColor Yellow
Write-Host "Press Ctrl+C to stop monitoring" -ForegroundColor Yellow
Write-Host "--------------------------------------------------"
Write-Host "Time`t`tMemory Usage(MB)`tWorking Set(MB)`tVirtual Memory(MB)"
Write-Host "--------------------------------------------------"

# Create CSV log file
$logFile = "FatFullVersion_MemoryLog_$(Get-Date -Format 'yyyyMMdd_HHmmss').csv"
"Time,MemoryUsage(MB),WorkingSet(MB),PrivateWorkingSet(MB),VirtualMemory(MB),GCCount" | Out-File -FilePath $logFile

# Start monitoring loop
try {
    while ($true) {
        # Refresh process info
        $process = Get-Process -Id $process.Id -ErrorAction SilentlyContinue
        
        if ($null -eq $process) {
            Write-Host "Process has terminated" -ForegroundColor Red
            break
        }
        
        # Get memory info
        $memoryMB = [math]::Round($process.PrivateMemorySize64 / 1MB, 2)
        $workingSetMB = [math]::Round($process.WorkingSet64 / 1MB, 2)
        $privateMB = [math]::Round($process.PrivateMemorySize64 / 1MB, 2)
        $virtualMB = [math]::Round($process.VirtualMemorySize64 / 1MB, 2)
        $gcCount = [System.GC]::CollectionCount(0) + [System.GC]::CollectionCount(1) + [System.GC]::CollectionCount(2)
        
        # Output to console
        $time = Get-Date -Format "HH:mm:ss"
        Write-Host "$time`t$memoryMB MB`t`t$workingSetMB MB`t$virtualMB MB"
        
        # Record to CSV
        "$time,$memoryMB,$workingSetMB,$privateMB,$virtualMB,$gcCount" | Out-File -FilePath $logFile -Append
        
        # Wait 5 seconds
        Start-Sleep -Seconds 5
    }
}
catch {
    Write-Host "Monitoring stopped: $_" -ForegroundColor Red
}
finally {
    Write-Host "--------------------------------------------------"
    Write-Host "Memory monitoring has ended, log saved to: $logFile" -ForegroundColor Green
} 