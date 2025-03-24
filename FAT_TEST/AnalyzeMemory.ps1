# 内存监控脚本
Write-Host "开始监控FatFullVersion程序的内存使用..." -ForegroundColor Green

# 指定程序路径
$exePath = "D:\GIT\Git\code\FAT_TEST\FAT_TEST\FatFullVersion\FatFullVersion\bin\Debug\net8.0-windows7.0\FatFullVersion.exe"

# 启动程序
Start-Process -FilePath $exePath
Start-Sleep -Seconds 2

# 获取进程ID
$process = Get-Process -Name "FatFullVersion" -ErrorAction SilentlyContinue

if ($null -eq $process) {
    Write-Host "无法找到FatFullVersion进程，请确保程序已启动" -ForegroundColor Red
    exit
}

Write-Host "正在监控进程ID: $($process.Id)" -ForegroundColor Yellow
Write-Host "按Ctrl+C停止监控" -ForegroundColor Yellow
Write-Host "--------------------------------------------------"
Write-Host "时间`t`t内存使用(MB)`t工作集(MB)`t虚拟内存(MB)"
Write-Host "--------------------------------------------------"

# 创建CSV日志文件
$logFile = "FatFullVersion_MemoryLog_$(Get-Date -Format 'yyyyMMdd_HHmmss').csv"
"时间,内存使用(MB),工作集(MB),私有工作集(MB),虚拟内存(MB),GC数" | Out-File -FilePath $logFile

# 开始监控循环
try {
    while ($true) {
        # 刷新进程信息
        $process = Get-Process -Id $process.Id -ErrorAction SilentlyContinue
        
        if ($null -eq $process) {
            Write-Host "进程已终止" -ForegroundColor Red
            break
        }
        
        # 获取内存信息
        $memoryMB = [math]::Round($process.PrivateMemorySize64 / 1MB, 2)
        $workingSetMB = [math]::Round($process.WorkingSet64 / 1MB, 2)
        $privateMB = [math]::Round($process.PrivateMemorySize64 / 1MB, 2)
        $virtualMB = [math]::Round($process.VirtualMemorySize64 / 1MB, 2)
        $gcCount = [System.GC]::CollectionCount(0) + [System.GC]::CollectionCount(1) + [System.GC]::CollectionCount(2)
        
        # 输出到控制台
        $time = Get-Date -Format "HH:mm:ss"
        Write-Host "$time`t$memoryMB MB`t`t$workingSetMB MB`t$virtualMB MB"
        
        # 记录到CSV
        "$time,$memoryMB,$workingSetMB,$privateMB,$virtualMB,$gcCount" | Out-File -FilePath $logFile -Append
        
        # 等待5秒
        Start-Sleep -Seconds 5
    }
}
catch {
    Write-Host "监控停止: $_" -ForegroundColor Red
}
finally {
    Write-Host "--------------------------------------------------"
    Write-Host "内存监控已结束，日志已保存至: $logFile" -ForegroundColor Green
} 