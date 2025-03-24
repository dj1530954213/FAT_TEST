using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace FatFullVersion.Diagnostics
{
    /// <summary>
    /// 内存泄漏检测工具类，用于诊断和监控应用程序内存使用情况
    /// </summary>
    public static class MemoryLeakDetector
    {
        private static Timer _memoryMonitorTimer;
        private static long _baselineMemory;
        private static readonly List<MemorySnapshot> _memoryHistory = new List<MemorySnapshot>();
        private static readonly Dictionary<string, WeakReference> _objectTracker = new Dictionary<string, WeakReference>();
        private static bool _isRecording = false;
        private static string _logFilePath;
        
        /// <summary>
        /// 内存检测选项
        /// </summary>
        public class MemoryMonitorOptions
        {
            /// <summary>监测间隔（毫秒）</summary>
            public int MonitorIntervalMs { get; set; } = 30000;
            
            /// <summary>内存增长警告阈值（MB）</summary>
            public int WarningThresholdMB { get; set; } = 100;
            
            /// <summary>是否记录详细日志</summary>
            public bool EnableDetailedLogging { get; set; } = true;
            
            /// <summary>日志文件路径</summary>
            public string LogFilePath { get; set; } = "memory_leak_log.txt";
            
            /// <summary>内存快照最大数量</summary>
            public int MaxSnapshots { get; set; } = 20;
        }
        
        /// <summary>
        /// 内存快照
        /// </summary>
        public class MemorySnapshot
        {
            /// <summary>捕获时间</summary>
            public DateTime Timestamp { get; set; }
            
            /// <summary>私有内存使用（字节）</summary>
            public long PrivateMemoryBytes { get; set; }
            
            /// <summary>工作集大小（字节）</summary>
            public long WorkingSetBytes { get; set; }
            
            /// <summary>虚拟内存大小（字节）</summary>
            public long VirtualMemoryBytes { get; set; }
            
            /// <summary>GC堆大小（字节）</summary>
            public long GCHeapBytes { get; set; }
            
            /// <summary>GC代数</summary>
            public int[] GCCollectionCounts { get; set; }
            
            /// <summary>GC实际代数引用次数</summary>
            public long[] GCHandleCounts { get; set; }
            
            /// <summary>活动视图计数</summary>
            public int ActiveViewCount { get; set; }
        }
        
        /// <summary>
        /// 开始监控内存使用情况
        /// </summary>
        /// <param name="options">监控选项</param>
        public static void StartMonitoring(MemoryMonitorOptions options = null)
        {
            options ??= new MemoryMonitorOptions();
            _logFilePath = options.LogFilePath;
            
            // 建立基准线内存使用量
            _baselineMemory = Process.GetCurrentProcess().PrivateMemorySize64;
            LogMessage($"内存监控启动。基准内存: {_baselineMemory / 1024 / 1024} MB");
            
            // 清理之前的数据
            _memoryHistory.Clear();
            _isRecording = true;
            
            // 捕获初始快照
            TakeMemorySnapshot();
            
            // 启动定时监控
            _memoryMonitorTimer = new Timer(_ => 
            {
                if (!_isRecording) return;
                
                try
                {
                    var snapshot = TakeMemorySnapshot();
                    ProcessMemorySnapshot(snapshot, options);
                }
                catch (Exception ex)
                {
                    LogMessage($"内存监控错误: {ex.Message}");
                }
            }, null, options.MonitorIntervalMs, options.MonitorIntervalMs);
            
            // 添加应用程序退出处理
            Application.Current.Exit += (s, e) => 
            {
                StopMonitoring();
            };
        }
        
        /// <summary>
        /// 停止内存监控
        /// </summary>
        public static void StopMonitoring()
        {
            _isRecording = false;
            _memoryMonitorTimer?.Dispose();
            _memoryMonitorTimer = null;
            
            // 生成报告
            GenerateMemoryReport();
        }
        
        /// <summary>
        /// 手动强制执行内存清理
        /// </summary>
        public static void ForceCleanup()
        {
            LogMessage("强制内存清理开始");
            
            var before = Process.GetCurrentProcess().PrivateMemorySize64;
            
            // 多代收集
            GC.Collect(2, GCCollectionMode.Forced, true, true);
            GC.WaitForPendingFinalizers();
            GC.Collect(2, GCCollectionMode.Forced, true, true);
            
            // 回收物理内存
            NativeMethods.SetProcessWorkingSetSize(
                Process.GetCurrentProcess().Handle, 
                new IntPtr(-1), 
                new IntPtr(-1));
            
            var after = Process.GetCurrentProcess().PrivateMemorySize64;
            LogMessage($"强制内存清理完成。清理前: {before / 1024 / 1024} MB, 清理后: {after / 1024 / 1024} MB, 释放: {(before - after) / 1024 / 1024} MB");
        }
        
        /// <summary>
        /// 跟踪对象以检测内存泄漏
        /// </summary>
        /// <param name="obj">要跟踪的对象</param>
        /// <param name="tag">对象标签</param>
        public static void TrackObject(object obj, string tag)
        {
            if (obj == null) return;
            
            var key = $"{tag}_{Guid.NewGuid()}";
            _objectTracker[key] = new WeakReference(obj);
            LogMessage($"开始跟踪对象: {tag} ({obj.GetType().FullName})");
        }
        
        /// <summary>
        /// 检查当前跟踪的对象
        /// </summary>
        /// <returns>泄漏对象列表</returns>
        public static Dictionary<string, int> CheckTrackedObjects()
        {
            Dictionary<string, int> leakCounts = new Dictionary<string, int>();
            var keysToRemove = new List<string>();
            
            foreach (var pair in _objectTracker)
            {
                if (!pair.Value.IsAlive)
                {
                    keysToRemove.Add(pair.Key);
                    continue;
                }
                
                // 提取标签
                string tag = pair.Key.Split('_')[0];
                
                if (!leakCounts.ContainsKey(tag))
                {
                    leakCounts[tag] = 0;
                }
                
                leakCounts[tag]++;
            }
            
            // 清理不再存活的引用
            foreach (var key in keysToRemove)
            {
                _objectTracker.Remove(key);
            }
            
            // 记录结果
            var sb = new StringBuilder("跟踪对象检查结果:\n");
            foreach (var pair in leakCounts)
            {
                sb.AppendLine($"  - {pair.Key}: {pair.Value} 个实例");
            }
            LogMessage(sb.ToString());
            
            return leakCounts;
        }
        
        /// <summary>
        /// 执行任务并测量内存使用
        /// </summary>
        /// <param name="taskName">任务名称</param>
        /// <param name="action">要执行的操作</param>
        public static void MeasureMemoryUsage(string taskName, Action action)
        {
            LogMessage($"开始测量任务内存使用: {taskName}");
            
            // 强制GC以获得更准确的基线
            GC.Collect(2, GCCollectionMode.Forced, true);
            GC.WaitForPendingFinalizers();
            GC.Collect(2, GCCollectionMode.Forced, true);
            
            // 记录起始内存
            long startMemory = Process.GetCurrentProcess().PrivateMemorySize64;
            long startGen0 = GC.CollectionCount(0);
            long startGen1 = GC.CollectionCount(1);
            long startGen2 = GC.CollectionCount(2);
            
            // 执行操作
            Stopwatch sw = Stopwatch.StartNew();
            action();
            sw.Stop();
            
            // 记录结束内存
            long endMemory = Process.GetCurrentProcess().PrivateMemorySize64;
            long endGen0 = GC.CollectionCount(0);
            long endGen1 = GC.CollectionCount(1);
            long endGen2 = GC.CollectionCount(2);
            
            // 计算差异
            long memoryDiff = endMemory - startMemory;
            long gen0Diff = endGen0 - startGen0;
            long gen1Diff = endGen1 - startGen1;
            long gen2Diff = endGen2 - startGen2;
            
            // 记录结果
            var sb = new StringBuilder();
            sb.AppendLine($"任务 '{taskName}' 内存使用报告:");
            sb.AppendLine($"  - 执行时间: {sw.ElapsedMilliseconds} ms");
            sb.AppendLine($"  - 内存变化: {memoryDiff / 1024 / 1024} MB ({startMemory / 1024 / 1024} MB -> {endMemory / 1024 / 1024} MB)");
            sb.AppendLine($"  - GC收集: Gen0={gen0Diff}, Gen1={gen1Diff}, Gen2={gen2Diff}");
            
            LogMessage(sb.ToString());
        }
        
        /// <summary>
        /// 分析指定对象的内存大小
        /// </summary>
        /// <param name="obj">要分析的对象</param>
        /// <param name="depth">最大递归深度</param>
        /// <returns>对象大小及内容信息</returns>
        public static Dictionary<string, long> AnalyzeObjectSize(object obj, int depth = 3)
        {
            var result = new Dictionary<string, long>();
            if (obj == null) return result;
            
            var visited = new HashSet<object>();
            AnalyzeObjectSizeRecursive(obj, result, visited, "", depth);
            
            return result;
        }
        
        #region 私有辅助方法
        
        /// <summary>
        /// 捕获当前内存快照
        /// </summary>
        private static MemorySnapshot TakeMemorySnapshot()
        {
            var process = Process.GetCurrentProcess();
            
            var snapshot = new MemorySnapshot
            {
                Timestamp = DateTime.Now,
                PrivateMemoryBytes = process.PrivateMemorySize64,
                WorkingSetBytes = process.WorkingSet64,
                VirtualMemoryBytes = process.VirtualMemorySize64,
                GCHeapBytes = GC.GetTotalMemory(false),
                GCCollectionCounts = new[] 
                { 
                    GC.CollectionCount(0),
                    GC.CollectionCount(1),
                    GC.CollectionCount(2)
                },
                // 视图对象计数可能需要自定义实现
                ActiveViewCount = 0
            };
            
            _memoryHistory.Add(snapshot);
            
            return snapshot;
        }
        
        /// <summary>
        /// 处理内存快照并检测异常情况
        /// </summary>
        private static void ProcessMemorySnapshot(MemorySnapshot snapshot, MemoryMonitorOptions options)
        {
            // 如果历史记录不足，无法分析趋势
            if (_memoryHistory.Count < 2) return;
            
            // 获取上一个快照
            var previousSnapshot = _memoryHistory[_memoryHistory.Count - 2];
            
            // 计算内存差异（MB）
            double memoryDiffMB = (snapshot.PrivateMemoryBytes - previousSnapshot.PrivateMemoryBytes) / (1024.0 * 1024.0);
            
            // 检查内存增长是否超过阈值
            if (memoryDiffMB > options.WarningThresholdMB)
            {
                string message = $"检测到内存急剧增长: {memoryDiffMB:F2} MB (从 {previousSnapshot.PrivateMemoryBytes / 1024 / 1024} MB 到 {snapshot.PrivateMemoryBytes / 1024 / 1024} MB)";
                LogMessage(message, true);
                
                // 可以在这里添加其他诊断操作，如创建转储文件等
            }
            
            // 限制历史记录数量以避免自身消耗太多内存
            if (_memoryHistory.Count > options.MaxSnapshots)
            {
                _memoryHistory.RemoveAt(0);
            }
        }
        
        /// <summary>
        /// 生成内存报告
        /// </summary>
        private static void GenerateMemoryReport()
        {
            if (_memoryHistory.Count < 2) return;
            
            var sb = new StringBuilder();
            sb.AppendLine("=== 内存使用报告 ===");
            sb.AppendLine($"记录开始时间: {_memoryHistory.First().Timestamp}");
            sb.AppendLine($"记录结束时间: {_memoryHistory.Last().Timestamp}");
            sb.AppendLine($"监控持续时间: {(_memoryHistory.Last().Timestamp - _memoryHistory.First().Timestamp).TotalMinutes:F1} 分钟");
            sb.AppendLine();
            
            // 计算总体统计信息
            var firstSnapshot = _memoryHistory.First();
            var lastSnapshot = _memoryHistory.Last();
            double totalGrowthMB = (lastSnapshot.PrivateMemoryBytes - firstSnapshot.PrivateMemoryBytes) / (1024.0 * 1024.0);
            double avgGrowthPerHourMB = totalGrowthMB * 60 / (_memoryHistory.Last().Timestamp - _memoryHistory.First().Timestamp).TotalMinutes;
            
            sb.AppendLine($"初始内存: {firstSnapshot.PrivateMemoryBytes / 1024 / 1024} MB");
            sb.AppendLine($"最终内存: {lastSnapshot.PrivateMemoryBytes / 1024 / 1024} MB");
            sb.AppendLine($"总增长: {totalGrowthMB:F2} MB");
            sb.AppendLine($"平均每小时增长: {avgGrowthPerHourMB:F2} MB/小时");
            sb.AppendLine();
            
            // 分析GC活动
            var gcGen0 = lastSnapshot.GCCollectionCounts[0] - firstSnapshot.GCCollectionCounts[0];
            var gcGen1 = lastSnapshot.GCCollectionCounts[1] - firstSnapshot.GCCollectionCounts[1];
            var gcGen2 = lastSnapshot.GCCollectionCounts[2] - firstSnapshot.GCCollectionCounts[2];
            
            sb.AppendLine($"GC活动: Gen0={gcGen0}, Gen1={gcGen1}, Gen2={gcGen2}");
            sb.AppendLine();
            
            // 记录最大内存使用峰值
            var peakSnapshot = _memoryHistory.OrderByDescending(s => s.PrivateMemoryBytes).First();
            sb.AppendLine($"峰值内存: {peakSnapshot.PrivateMemoryBytes / 1024 / 1024} MB (时间: {peakSnapshot.Timestamp})");
            
            // 分析内存泄漏可能性
            bool potentialLeak = totalGrowthMB > 100 && avgGrowthPerHourMB > 20;
            if (potentialLeak)
            {
                sb.AppendLine();
                sb.AppendLine("*** 潜在内存泄漏警告 ***");
                sb.AppendLine("内存使用呈持续增长趋势，可能存在内存泄漏。建议采取以下措施：");
                sb.AppendLine("1. 检查大型集合是否未被释放");
                sb.AppendLine("2. 查看是否存在未注销的事件处理程序");
                sb.AppendLine("3. 检查定时器和后台任务是否正确关闭");
                sb.AppendLine("4. 使用内存分析工具（如dotMemory）进行更深入分析");
            }
            
            // 保存报告
            string reportPath = "memory_report.txt";
            File.WriteAllText(reportPath, sb.ToString());
            
            LogMessage($"内存报告已生成: {reportPath}");
        }
        
        /// <summary>
        /// 记录日志消息
        /// </summary>
        private static void LogMessage(string message, bool isWarning = false)
        {
            string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {(isWarning ? "警告: " : "")}{message}";
            
            Debug.WriteLine(logEntry);
            
            try
            {
                if (_logFilePath != null)
                {
                    File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
                }
            }
            catch
            {
                // 忽略日志文件写入错误
            }
        }
        
        /// <summary>
        /// 递归分析对象大小
        /// </summary>
        private static void AnalyzeObjectSizeRecursive(object obj, Dictionary<string, long> result, HashSet<object> visited, string path, int depth)
        {
            if (obj == null || depth <= 0 || visited.Contains(obj))
                return;
                
            visited.Add(obj);
            
            Type type = obj.GetType();
            long size = 0;
            
            // 处理简单类型
            if (type.IsPrimitive || type == typeof(string) || type == typeof(DateTime))
            {
                if (type == typeof(string))
                {
                    size = 2 * ((string)obj).Length + 26; // 字符串标头和字符
                }
                else if (type.IsPrimitive)
                {
                    size = Marshal.SizeOf(type);
                }
                else
                {
                    size = 16; // 估计值
                }
                
                result[path + "(" + type.Name + ")"] = size;
                return;
            }
            
            // 处理数组和集合
            if (type.IsArray)
            {
                Array array = (Array)obj;
                int elementSize = 0;
                
                if (array.Length > 0 && array.GetValue(0) != null)
                {
                    Type elementType = array.GetValue(0).GetType();
                    if (elementType.IsPrimitive)
                    {
                        elementSize = Marshal.SizeOf(elementType);
                    }
                    else
                    {
                        elementSize = 16; // 估计值
                    }
                }
                
                size = 24 + array.Length * (elementSize + 4); // 数组标头 + 元素
                result[path + "(" + type.Name + ")"] = size;
                
                if (depth > 1)
                {
                    for (int i = 0; i < Math.Min(array.Length, 10); i++)
                    {
                        object item = array.GetValue(i);
                        if (item != null && !visited.Contains(item))
                        {
                            AnalyzeObjectSizeRecursive(item, result, visited, path + "[" + i + "]", depth - 1);
                        }
                    }
                }
                
                return;
            }
            
            if (obj is ICollection collection)
            {
                size = 24 + collection.Count * 16; // 集合标头 + 元素估计
                result[path + "(" + type.Name + ")"] = size;
                
                if (depth > 1 && collection is IEnumerable<object> enumerable)
                {
                    int i = 0;
                    foreach (var item in enumerable.Take(10))
                    {
                        if (item != null && !visited.Contains(item))
                        {
                            AnalyzeObjectSizeRecursive(item, result, visited, path + "[" + i + "]", depth - 1);
                        }
                        i++;
                    }
                }
                
                return;
            }
            
            // 处理普通对象
            size = 16; // 对象标头
            result[path + "(" + type.Name + ")"] = size;
            
            if (depth > 1)
            {
                // 分析字段
                var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (var field in fields)
                {
                    string fieldPath = path + "." + field.Name;
                    object fieldValue = field.GetValue(obj);
                    
                    if (fieldValue != null && !visited.Contains(fieldValue))
                    {
                        AnalyzeObjectSizeRecursive(fieldValue, result, visited, fieldPath, depth - 1);
                    }
                }
            }
        }
        
        /// <summary>
        /// 本机方法调用
        /// </summary>
        private static class NativeMethods
        {
            [System.Runtime.InteropServices.DllImport("kernel32.dll")]
            public static extern bool SetProcessWorkingSetSize(IntPtr process, IntPtr minSize, IntPtr maxSize);
        }
        
        #endregion
    }
} 