# 内存分析指南脚本
Write-Host "FatFullVersion 内存分析指南" -ForegroundColor Cyan
Write-Host "============================" -ForegroundColor Cyan
Write-Host ""

# 检查是否安装了必要的工具
$dotMemoryInstalled = Test-Path "C:\Program Files\JetBrains\dotMemory\dotMemory.exe" -ErrorAction SilentlyContinue
$perfViewInstalled = Test-Path "C:\PerfView\PerfView.exe" -ErrorAction SilentlyContinue

Write-Host "第一步：基础内存使用检查" -ForegroundColor Green
Write-Host "--------------------"
Write-Host "通过以下命令检查基础内存使用情况:" -ForegroundColor Yellow
Write-Host "   1. 运行 .\AnalyzeMemory.ps1 进行基础内存监控"
Write-Host "   2. 使用程序一段时间，特别是执行可能导致内存增长的操作"
Write-Host "   3. 查看生成的CSV日志文件，分析内存增长趋势"
Write-Host ""

Write-Host "第二步：代码优化建议" -ForegroundColor Green
Write-Host "--------------------"
Write-Host "检查以下常见的内存占用问题:" -ForegroundColor Yellow
Write-Host "   1. 大型集合未释放：" -ForegroundColor White
Write-Host "      - 检查DataEditViewModel中的ObservableCollection<ChannelMapping>是否过大"
Write-Host "      - 确保不在静态集合中持续添加数据"
Write-Host ""
Write-Host "   2. 资源释放：" -ForegroundColor White
Write-Host "      - 确保ExcelPointDataService和ApiPointDataService中的资源正确释放"
Write-Host "      - 检查HttpClient是否作为单例使用"
Write-Host ""
Write-Host "   3. WPF绑定：" -ForegroundColor White
Write-Host "      - 减少DataEditView.xaml中的数据绑定"
Write-Host "      - 考虑使用虚拟化容器(VirtualizingStackPanel)显示大量数据"
Write-Host ""
Write-Host "   4. 图像资源：" -ForegroundColor White
Write-Host "      - 减小图像资源大小，确保它们被正确释放"
Write-Host "      - 考虑使用图像缓存策略"
Write-Host ""

# 推荐专业工具
Write-Host "第三步：使用专业内存分析工具" -ForegroundColor Green
Write-Host "------------------------"

if ($dotMemoryInstalled) {
    Write-Host "已检测到JetBrains dotMemory，推荐使用此工具进行详细分析:" -ForegroundColor Yellow
    Write-Host "   1. 启动dotMemory并附加到FatFullVersion进程"
    Write-Host "   2. 获取内存快照并分析对象保留情况"
} else {
    Write-Host "推荐使用JetBrains dotMemory进行详细分析:" -ForegroundColor Yellow
    Write-Host "   1. 从https://www.jetbrains.com/dotmemory/下载并安装dotMemory"
    Write-Host "   2. 使用dotMemory附加到FatFullVersion进程"
}

Write-Host ""
if ($perfViewInstalled) {
    Write-Host "已检测到PerfView，可使用此工具进行GC分析:" -ForegroundColor Yellow
    Write-Host "   1. 运行PerfView并收集GC事件"
    Write-Host "   2. 分析GC暂停和内存压力"
} else {
    Write-Host "可使用微软PerfView工具进行GC分析:" -ForegroundColor Yellow
    Write-Host "   1. 从https://github.com/Microsoft/perfview/releases下载PerfView"
    Write-Host "   2. 使用PerfView收集GC事件进行分析"
}

Write-Host ""
Write-Host "第四步：应用优化方案" -ForegroundColor Green
Write-Host "--------------------"
Write-Host "1. 运行优化编译脚本创建优化版本:" -ForegroundColor Yellow
Write-Host "   运行 .\OptimizeFatFullVersion.ps1"
Write-Host ""
Write-Host "2. 手动代码优化建议:" -ForegroundColor Yellow
Write-Host "   - 添加内存清理逻辑，尤其是在切换视图时"
Write-Host "   - 实现数据分页加载而非一次性加载全部数据"
Write-Host "   - 使用弱引用缓存策略"
Write-Host "   - 在不使用的UI控件上设置x:Shared='False'"
Write-Host "   - 考虑使用内存映射文件处理大型数据集"
Write-Host ""

# 添加定位内存泄漏的代码片段
Write-Host "定位内存泄漏的代码片段(可添加到App.xaml.cs):" -ForegroundColor Green
Write-Host @'
// 在适当的地方添加以下代码进行内存使用监控
private System.Threading.Timer _memoryMonitorTimer;

private void StartMemoryMonitoring()
{
    _memoryMonitorTimer = new System.Threading.Timer(state => 
    {
        var proc = System.Diagnostics.Process.GetCurrentProcess();
        var memoryMB = proc.PrivateMemorySize64 / (1024 * 1024);
        System.Diagnostics.Debug.WriteLine($"内存使用: {memoryMB} MB");
        
        // 如果内存超过阈值，执行清理
        if (memoryMB > 500)
        {
            System.Diagnostics.Debug.WriteLine("执行内存清理...");
            GC.Collect(2, GCCollectionMode.Forced, true);
            GC.WaitForPendingFinalizers();
        }
    }, null, 0, 10000); // 每10秒检查一次
}
'@ -ForegroundColor Yellow

Write-Host ""
Write-Host "需要更多帮助请详细描述内存占用发生的具体场景，以便提供更有针对性的优化建议。" -ForegroundColor Cyan 