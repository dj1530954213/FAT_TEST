using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using FatFullVersion.Models;
using FatFullVersion.Entities;
using FatFullVersion.Entities.EntitiesEnum;
using FatFullVersion.IServices;
using FatFullVersion.Services.ChannelTask;
using DryIoc;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using Prism.Events;
using FatFullVersion.Events;
using FatFullVersion.ViewModels;

namespace FatFullVersion.Services
{
    /// <summary>
    /// 测试任务管理器，负责管理所有测试任务的创建、启动、停止和管理
    /// </summary>
    public class TestTaskManager : ITestTaskManager, IDisposable
    {
        #region 字段

        private readonly IChannelMappingService _channelMappingService;
        private readonly IPlcCommunication _testPlcCommunication;
        private readonly IPlcCommunication _targetPlcCommunication;
        private readonly IMessageService _messageService;
        private readonly IChannelStateManager _channelStateManager;
        private readonly IEventAggregator _eventAggregator;
        private readonly ConcurrentDictionary<string, TestTask> _activeTasks;
        private CancellationTokenSource _masterCancellationTokenSource;
        private readonly SemaphoreSlim _semaphore;
        private bool _isWiringCompleted;
        private FatFullVersion.ViewModels.BatchInfo _currentBatch;
        private Window _progressDialog;
        private readonly object _dialogLock = new object();
        private bool _isRunning;

        #endregion

        #region 属性

        /// <summary>
        /// 获取接线是否已完成的标志
        /// </summary>
        public bool IsWiringCompleted => _isWiringCompleted;

        #endregion

        #region 构造函数

        /// <summary>
        /// 创建测试任务管理器实例
        /// </summary>
        /// <param name="channelMappingService">通道映射服务</param>
        /// <param name="serviceLocator">服务定位器</param>
        /// <param name="messageService">消息服务</param>
        /// <param name="channelStateManager">通道状态管理器</param>
        /// <param name="eventAggregator">事件聚合器</param>
        /// <param name="maxConcurrentTasks">最大并发任务数量，默认为处理器核心数的2倍</param>
        public TestTaskManager(
            IChannelMappingService channelMappingService,
            IServiceLocator serviceLocator,
            IMessageService messageService,
            IChannelStateManager channelStateManager,
            IEventAggregator eventAggregator,
            int? maxConcurrentTasks = null)
        {
            _channelMappingService = channelMappingService ?? throw new ArgumentNullException(nameof(channelMappingService));
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
            _channelStateManager = channelStateManager ?? throw new ArgumentNullException(nameof(channelStateManager));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            //使用ServiceLocator这个单例服务来完成在APP的注册(使用同一个接口但是通过名称来区分实例)
            _testPlcCommunication = serviceLocator.ResolveNamed<IPlcCommunication>("TestPlcCommunication");
            _targetPlcCommunication = serviceLocator.ResolveNamed<IPlcCommunication>("TargetPlcCommunication");
            
            _activeTasks = new ConcurrentDictionary<string, TestTask>();
            _masterCancellationTokenSource = new CancellationTokenSource();
            
            // 设置并行任务的最大并发数量
            int concurrentTasks = maxConcurrentTasks ?? Environment.ProcessorCount * 2;
            _semaphore = new SemaphoreSlim(concurrentTasks, concurrentTasks);
            
            _isWiringCompleted = false;
            _currentBatch = null;
            _isRunning = false;
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 从通道映射集合创建测试任务
        /// </summary>
        /// <param name="channelMappings">需要测试的通道映射集合</param>
        /// <returns>创建的任务ID列表</returns>
        public async Task<IEnumerable<string>> CreateTestTasksAsync(IEnumerable<ChannelMapping> channelMappings)
        {
            if (channelMappings == null || !channelMappings.Any())
                return Enumerable.Empty<string>();

            List<string> taskIds = new List<string>();
            var tasksToCreate = new List<TestTask>();

            foreach (var cm in channelMappings)
            {
                if (cm == null) continue;
                string taskId = cm.Id.ToString(); 
                TestTask task = null;
                switch (cm.ModuleType?.ToUpper())
                {
                    case "AI":
                        task = new AITestTask(taskId, cm, _testPlcCommunication, _targetPlcCommunication);
                        break;
                    case "AO":
                        task = new AOTestTask(taskId, cm, _testPlcCommunication, _targetPlcCommunication);
                        break;
                    case "DI":
                        task = new DITestTask(taskId, cm, _testPlcCommunication, _targetPlcCommunication);
                        break;
                    case "DO":
                        task = new DOTestTask(taskId, cm, _testPlcCommunication, _targetPlcCommunication);
                        break;
                    default:
                        System.Diagnostics.Debug.WriteLine($"警告：不支持的模块类型 '{cm.ModuleType}'，无法为通道 '{cm.VariableName}' 创建测试任务。");
                        break;
                }
                if (task != null)
                {
                    tasksToCreate.Add(task);
                }
            }
            
            foreach(var tsk in tasksToCreate)
            {
                if (AddTask(tsk))
                {
                    taskIds.Add(tsk.Id);
                }
            }
            return taskIds;
        }

        /// <summary>
        /// 确认接线完成，准备通道状态，并可选择是否自动开始测试。
        /// </summary>
        public async Task<bool> ConfirmWiringCompleteAsync(FatFullVersion.ViewModels.BatchInfo batchInfo, bool isConfirmed, IEnumerable<ChannelMapping> testMap)
        {
            if (batchInfo == null) 
            {
                await _messageService.ShowAsync("错误", "批次信息不能为空。", MessageBoxButton.OK);
                return false;
            }
            _currentBatch = batchInfo;

            if (!await EnsurePlcConnectionsAsync()) return false;

            var confirmResult = await _messageService.ShowAsync("确认接线", $"是否确认批次 '{_currentBatch.BatchName}' 的所有硬件接线已准备就绪？", MessageBoxButton.YesNo);
            if (confirmResult != MessageBoxResult.Yes)
            {
                _isWiringCompleted = false;
                await _messageService.ShowAsync("提示", "接线未确认，测试流程已取消。", MessageBoxButton.OK);
                return false;
            }

            _isWiringCompleted = true;
            var channelMappingsInBatch = testMap.Where(c => c.TestBatch?.Equals(_currentBatch.BatchName) == true).ToList();

            if (!channelMappingsInBatch.Any())
            {
                await _messageService.ShowAsync("提示", $"批次 '{_currentBatch.BatchName}' 中没有找到任何通道。", MessageBoxButton.OK);
                return true; 
            }

            List<Guid> preparedChannelIds = new List<Guid>();
            foreach (var channel in channelMappingsInBatch)
            {
                if (channel.TestResultStatus != 3) 
                {
                    _channelStateManager.PrepareForWiringConfirmation(channel, DateTime.Now);
                    preparedChannelIds.Add(channel.Id);
                }
            }
            
            if (preparedChannelIds.Any())
            {
                NotifyTestResultsUpdated(preparedChannelIds);
                await _messageService.ShowAsync("提示", "接线已确认，相关通道已准备好等待测试。", MessageBoxButton.OK);
            }
            else
            {
                 await _messageService.ShowAsync("提示", "接线已确认，但当前批次中所有通道均已被跳过或无需准备。", MessageBoxButton.OK);
            }

            await ClearAllTasksAsyncInternal(); 
            await CreateTestTasksAsync(channelMappingsInBatch.Where(c => c.TestResultStatus != 3)); 

            return true;
        }

        /// <summary>
        /// 显示测试进度对话框
        /// </summary>
        /// <param name="isRetestMode">是否为复测模式，默认为false表示全自动测试</param>
        /// <param name="channelInfo">复测的通道信息（复测模式下使用）</param>
        /// <returns>异步任务</returns>
        public async Task ShowTestProgressDialogAsync(bool isRetestMode = false, ChannelMapping channelInfo = null) { 
            await Application.Current.Dispatcher.InvokeAsync(() => {
                 lock (_dialogLock)
                {
                    if (_progressDialog != null && _progressDialog.IsVisible) {
                        string title = isRetestMode ? "通道复测进行中" : "自动测试进行中";
                        _progressDialog.Title = title;
                        TextBlock messageTextBlock = null;
                        TextBlock batchOrChannelInfoTextBlock = null;

                        if (_progressDialog.Content is StackPanel sp)
                        {
                            messageTextBlock = sp.Children.OfType<TextBlock>().FirstOrDefault(tb => tb.Name == "ProgressMessageTextBlock");
                            batchOrChannelInfoTextBlock = sp.Children.OfType<TextBlock>().FirstOrDefault(tb => tb.Name == "BatchInfoTextBlock");
                        }
                        
                        if (messageTextBlock != null) {
                             messageTextBlock.Text = isRetestMode && channelInfo != null ? $"正在准备复测: {channelInfo.VariableName} ({channelInfo.ChannelTag})..." : "测试正在准备中...";
                        }
                        if (batchOrChannelInfoTextBlock != null) {
                            batchOrChannelInfoTextBlock.Text = !isRetestMode && _currentBatch != null ? $"批次: {_currentBatch.BatchName}" : (isRetestMode && channelInfo != null ? $"通道: {channelInfo.VariableName} ({channelInfo.ChannelTag})" : "");
                            batchOrChannelInfoTextBlock.Visibility = string.IsNullOrEmpty(batchOrChannelInfoTextBlock.Text) ? Visibility.Collapsed : Visibility.Visible;
                        }
                        return; 
                    }
                    string initialMessage = isRetestMode && channelInfo != null ? $"正在准备复测: {channelInfo.VariableName} ({channelInfo.ChannelTag})..." : "测试正在准备中...";
                    string dialogTitle = isRetestMode ? "通道复测进行中" : "自动测试进行中";

                    var mainMessageTb = new TextBlock { Name="ProgressMessageTextBlock", Text = initialMessage, Margin = new Thickness(10), VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
                    var progressBar = new ProgressBar { IsIndeterminate = true, Style = (Style)Application.Current.Resources["MaterialDesignCircularProgressBar"], Width = 50, Height = 50, Margin = new Thickness(10) };
                    var stackPanel = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
                    
                    TextBlock batchInfoTb = null; // 用于显示批次或通道信息
                    if (!isRetestMode && _currentBatch != null) { 
                        batchInfoTb = new TextBlock { Name="BatchInfoTextBlock", Text = $"批次: {_currentBatch.BatchName}", FontSize=12, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0,5,0,5) }; 
                        stackPanel.Children.Add(batchInfoTb); 
                    } else if (isRetestMode && channelInfo != null) { 
                         batchInfoTb = new TextBlock { Name="BatchInfoTextBlock", Text = $"通道: {channelInfo.VariableName} ({channelInfo.ChannelTag})", FontSize=12, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0,5,0,5) };
                        stackPanel.Children.Add(batchInfoTb); 
                    }

                    stackPanel.Children.Add(mainMessageTb); 
                    stackPanel.Children.Add(progressBar);

                    _progressDialog = new Window { 
                        Title = dialogTitle, 
                        Width = 380, Height = 180, 
                        Content = stackPanel, 
                        WindowStartupLocation = WindowStartupLocation.CenterScreen,
                        WindowStyle = WindowStyle.ToolWindow, 
                        ShowInTaskbar = false, 
                        Topmost = true 
                    };
                    _progressDialog.Show();
                }
            });
        }
        /// <summary>
        /// 关闭测试进度对话框
        /// </summary>
        private void CloseProgressDialog()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                lock (_dialogLock)
                {
                    if (_progressDialog != null)
                    {
                        if (_progressDialog.IsVisible)
                        {
                            _progressDialog.Close();
                        }
                        _progressDialog = null;
                    }
                }
            });
        }

        /// <summary>
        /// 并发启动并执行所有活动的、未跳过的测试任务，受信号量控制并发数。
        /// </summary>
        /// <returns>如果所有任务都无中断地启动，则为true；否则为false。</returns>
        public async Task<bool> StartAllTasksAsync(IEnumerable<ChannelMapping> channelsToTest)
        {
            if (_isRunning) 
            {
                await _messageService.ShowAsync("提示", "测试已在运行中。", MessageBoxButton.OK);
                return false;
            }
            if (!_isWiringCompleted)
            {
                await _messageService.ShowAsync("警告", "请先确认接线完成，然后才能开始测试。", MessageBoxButton.OK);
                return false;
            }

            _masterCancellationTokenSource?.Dispose();
            _masterCancellationTokenSource = new CancellationTokenSource();
            _isRunning = true;

            var tasksToRun = new List<TestTask>();
            if (channelsToTest != null)
            {
                foreach (var cm in channelsToTest)
                {
                    if (_activeTasks.TryGetValue(cm.Id.ToString(), out var task) && task.ChannelMapping.TestResultStatus != 3)
                    {
                        tasksToRun.Add(task);
                    }
                }
                tasksToRun = tasksToRun.OrderBy(t => t.ChannelMapping.TestId).ToList();
            }

            if (!tasksToRun.Any())
            {
                await _messageService.ShowAsync("提示", "当前沒有可執行的測試任務（可能所有通道均被跳过或未分配）。", MessageBoxButton.OK);
                _isRunning = false;
                CloseProgressDialog(); 
                return true; 
            }

            await ShowTestProgressDialogAsync(false, null);
            List<Task> executingTasks = new List<Task>();
            System.Diagnostics.Debug.WriteLine($"并发测试启动，任务数量: {tasksToRun.Count}, 批次: '{_currentBatch?.BatchName ?? "未知"}' @ {DateTime.Now}");

            foreach (var taskInstance in tasksToRun)
            {
                await _semaphore.WaitAsync(_masterCancellationTokenSource.Token); 
                if (_masterCancellationTokenSource.Token.IsCancellationRequested)
                {
                    _semaphore.Release();
                    break;
                }

                executingTasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        await UpdateProgressMessageAsync($"并发测试中: {taskInstance.ChannelMapping.VariableName}...");
                        _channelStateManager.BeginHardPointTest(taskInstance.ChannelMapping, DateTime.Now);
                        NotifyTestResultsUpdated(new[] { taskInstance.ChannelMapping.Id });

                        HardPointTestRawResult rawResult = await taskInstance.RunTestAsync(_masterCancellationTokenSource.Token);
                        System.Diagnostics.Debug.WriteLine($"通道 {taskInstance.ChannelMapping.VariableName} 并发测试完成. 成功: {rawResult.IsSuccess}. Detail: {rawResult.Detail?.Substring(0, Math.Min(rawResult.Detail.Length,100))}");
                        
                        _channelStateManager.SetHardPointTestOutcome(taskInstance.ChannelMapping, rawResult, DateTime.Now);
                        NotifyTestResultsUpdated(new[] { taskInstance.ChannelMapping.Id });
                    }
                    catch (OperationCanceledException)
                    {
                        System.Diagnostics.Debug.WriteLine($"通道 {taskInstance.ChannelMapping.VariableName} 并发测试被取消.");
                        var cancelResult = new HardPointTestRawResult(false, "任务执行被用户取消。");
                        _channelStateManager.SetHardPointTestOutcome(taskInstance.ChannelMapping, cancelResult, DateTime.Now);
                        NotifyTestResultsUpdated(new[] { taskInstance.ChannelMapping.Id });
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"通道 {taskInstance.ChannelMapping.VariableName} 并发测试发生错误: {ex.Message}");
                        var errorResult = new HardPointTestRawResult(false, $"任务 '{taskInstance.ChannelMapping.VariableName}' 执行时发生异常: {ex.Message}");
                        _channelStateManager.SetHardPointTestOutcome(taskInstance.ChannelMapping, errorResult, DateTime.Now);
                        NotifyTestResultsUpdated(new[] { taskInstance.ChannelMapping.Id });
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                }, _masterCancellationTokenSource.Token));
            }

            _ = Task.WhenAll(executingTasks).ContinueWith(async completedTaskAggregate =>
            {
                _isRunning = false;
                CloseProgressDialog();
                await UpdateBatchStatusAsync();
                System.Diagnostics.Debug.WriteLine($"并发测试全部任务执行尝试完毕. 批次: '{_currentBatch?.BatchName ?? "未知"}' @ {DateTime.Now}");
                
                if (completedTaskAggregate.IsFaulted)
                {
                    System.Diagnostics.Debug.WriteLine($"一个或多个并发测试任务在Task.WhenAll后检测到未处理的异常: {completedTaskAggregate.Exception.Flatten().InnerExceptions.FirstOrDefault()?.Message}");
                }
                else if (completedTaskAggregate.IsCanceled)
                {
                    System.Diagnostics.Debug.WriteLine("并发测试任务批次因CancellationToken被取消。");
                }

                await Application.Current.Dispatcher.InvokeAsync(() => 
                    _messageService.ShowAsync("测试完成", "所有硬点并发测试已执行完毕。请检查结果。", MessageBoxButton.OK)
                );
            }, TaskScheduler.Default); 

            return true; 
        }

        /// <summary>
        /// 更新进度对话框中的信息
        /// </summary>
        /// <param name="message">进度信息</param>
        private async Task UpdateProgressMessageAsync(string message) { 
             await Application.Current.Dispatcher.InvokeAsync(() => {
                if (_progressDialog != null && _progressDialog.IsVisible && _progressDialog.Content is StackPanel sp) {
                    TextBlock messageTextBlock = sp.Children.OfType<TextBlock>().FirstOrDefault(tb => tb.Name == "ProgressMessageTextBlock");
                    if (messageTextBlock != null) {
                        messageTextBlock.Text = message;
                    } 
                }
            });
        }

        /// <summary>
        /// 同步硬点测试结果到原始通道集合
        /// </summary>
        /// <param name="task">测试任务</param>
        // [Obsolete("此方法已弃用，状态更新应完全由 IChannelStateManager 处理，并通过事件通知UI更新")]
        // private void SyncHardPointTestResult(TestTask task) { /* 方法体已移除 */ }

        /// <summary>
        /// 评估模拟量测试结果
        /// </summary>
        private bool EvaluateAnalogTestResults(TestTask task)
        {
            try
            {
                // 确保有一些测试数据
                if (task.Result.Value0Percent == 0 && 
                    task.Result.Value25Percent == 0 && 
                    task.Result.Value50Percent == 0 && 
                    task.Result.Value75Percent == 0 && 
                    task.Result.Value100Percent == 0)
                {
                    // 如果所有值都为0，可能表示测试尚未完成
                    return false;
                }
                
                // 收集测试点
                Dictionary<string, (float Expected, float Actual)> testPoints = new Dictionary<string, (float Expected, float Actual)>();
                
                // 计算各个测试百分比的预期值
                if (!task.ChannelMapping.RangeLowerLimitValue.HasValue || !task.ChannelMapping.RangeUpperLimitValue.HasValue)
                {
                    if (string.IsNullOrEmpty(task.Result.ErrorMessage)) 
                        task.Result.ErrorMessage = "评估错误: 通道量程未定义。";
                    else 
                        task.Result.ErrorMessage += "; 评估错误: 通道量程未定义。";
                    return false; // Cannot evaluate if range is not defined
                }
                float minValue = task.ChannelMapping.RangeLowerLimitValue.Value;
                float maxValue = task.ChannelMapping.RangeUpperLimitValue.Value;
                float range = maxValue - minValue;
                
                // 添加所有测试点
                testPoints.Add("0%", (Expected: minValue, Actual: (float)task.Result.Value0Percent));
                testPoints.Add("25%", (Expected: minValue + (range * 25 / 100), Actual: (float)task.Result.Value25Percent));
                testPoints.Add("50%", (Expected: minValue + (range * 50 / 100), Actual: (float)task.Result.Value50Percent));
                testPoints.Add("75%", (Expected: minValue + (range * 75 / 100), Actual: (float)task.Result.Value75Percent));
                testPoints.Add("100%", (Expected: maxValue, Actual: (float)task.Result.Value100Percent));
                
                // 创建详细测试报告
                StringBuilder testReport = new StringBuilder();
                bool allPassed = true;
                
                // 允许的最大偏差百分比
                const float allowedDeviation = 1.0f;
                
                // 评估每个测试点
                foreach (var point in testPoints)
                {
                    float expected = point.Value.Expected;
                    float actual = point.Value.Actual;
                    
                    // 计算偏差
                    float deviation = Math.Abs(actual - expected);
                    float deviationPercent = (expected != 0) ? (deviation / Math.Abs(expected)) * 100 : 0;
                    
                    // 判断是否通过
                    bool pointPassed = deviationPercent <= allowedDeviation;
                    if (!pointPassed)
                        allPassed = false;
                        
                    // 添加到报告
                    testReport.AppendLine($"{point.Key}测试" + (pointPassed ? "通过" : $"失败：偏差{deviationPercent:F2}%超出范围"));
                }
                
                // 保存详细报告
                if (string.IsNullOrEmpty(task.Result.ErrorMessage))
                {
                    task.Result.ErrorMessage = testReport.ToString();
                }
                
                return allPassed;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"评估模拟量测试结果时出错: {ex.Message}");
                if (string.IsNullOrEmpty(task.Result.ErrorMessage))
                {
                    task.Result.ErrorMessage = $"评估测试结果时出错: {ex.Message}";
                }
                return false;
            }
        }

        /// <summary>
        /// 获取批次的整体测试状态
        /// </summary>
        /// <returns>批次状态</returns>
        private BatchTestStatus GetOverallBatchStatus()
        {
            int totalTasks = _activeTasks.Count;
            if (totalTasks == 0)
                return BatchTestStatus.NotStarted;

            int passedTasks = _activeTasks.Values.Count(t => t.Result?.Status == "通过");
            int failedTasks = _activeTasks.Values.Count(t => t.Result?.Status?.Contains("失败") == true);
            int cancelledTasks = _activeTasks.Values.Count(t => t.IsCancelled);

            // 硬点测试正在进行或刚完成时，总是显示为"测试中"
            // 这样可以提示用户进行后续的手动测试
            if (passedTasks > 0 || failedTasks > 0)
            {
                return BatchTestStatus.InProgress;
            }
            
            if (cancelledTasks > 0)
                return BatchTestStatus.Canceled;
                
            return BatchTestStatus.InProgress;
        }

        /// <summary>
        /// 更新批次状态为全部已完成
        /// 只有在所有手动测试完成后才调用此方法
        /// </summary>
        [Obsolete("CompleteAllTestsAsync 的逻辑应由ViewModel或IChannelStateManager在评估所有通道状态后处理，此方法不再安全调用")]
        public async Task<bool> CompleteAllTestsAsync()
        { 
            await Task.CompletedTask; 
            System.Diagnostics.Debug.WriteLine("警告: TestTaskManager.CompleteAllTestsAsync 被调用，但其功能已废弃或应移至ViewModel。");
            return false; 
        }

        /// <summary>
        /// 停止所有测试任务
        /// </summary>
        /// <returns>操作是否成功</returns>
        public async Task<bool> StopAllTasksAsync()
        {
            if (!_isRunning)
                return false;

            try
            {
                // 请求取消所有任务
                _masterCancellationTokenSource.Cancel();
                
                // 等待所有任务完成
                await Task.WhenAll(_activeTasks.Values.Select(t => t.StopAsync()));
                
                _isRunning = false;
                
                // 关闭进度对话框
                CloseProgressDialog();
                
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"停止任务时出错: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 暂停所有测试任务
        /// </summary>
        /// <returns>操作是否成功</returns>
        public async Task<bool> PauseAllTasksAsync() 
        { 
            bool onePausedOrAttempted = false;
            var tasksToPause = _activeTasks.Values.ToList(); 
            foreach(var task in tasksToPause) { 
                await task.PauseAsync(); 
                onePausedOrAttempted = true;
            }
            if (onePausedOrAttempted) await UpdateProgressMessageAsync("所有任务已请求暂停。");
            else await UpdateProgressMessageAsync("没有可暂停的任务。");
            return true; 
        }

        /// <summary>
        /// 恢复所有测试任务
        /// </summary>
        /// <returns>操作是否成功</returns>
        public async Task<bool> ResumeAllTasksAsync() 
        { 
            bool oneResumedOrAttempted = false;
            var tasksToResume = _activeTasks.Values.ToList();
            foreach(var task in tasksToResume) { 
                await task.ResumeAsync(); 
                oneResumedOrAttempted = true;
            }
            if (oneResumedOrAttempted) await UpdateProgressMessageAsync("所有任务已恢复执行。");
            else await UpdateProgressMessageAsync("没有可恢复的任务。");
            return true; 
        }

        /// <summary>
        /// 根据ID获取测试任务
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <returns>测试任务实例，如果不存在则返回null</returns>
        public TestTask GetTaskById(string taskId)
        {
            _activeTasks.TryGetValue(taskId, out var task); 
            return task; 
        }

        /// <summary>
        /// 根据通道映射获取测试任务
        /// </summary>
        /// <param name="channelMapping">通道映射实例</param>
        /// <returns>测试任务实例，如果不存在则返回null</returns>
        public TestTask GetTaskByChannel(ChannelMapping channelMapping) { 
            if (channelMapping == null) return null;
            if (!string.IsNullOrEmpty(channelMapping.Id.ToString()) && _activeTasks.TryGetValue(channelMapping.Id.ToString(), out var taskById))
            {
                return taskById;
            }
            return _activeTasks.Values.FirstOrDefault(t => t.ChannelMapping != null && t.ChannelMapping.Id == channelMapping.Id); 
        }

        /// <summary>
        /// 获取所有活跃的测试任务
        /// </summary>
        /// <returns>所有活跃的测试任务集合</returns>
        public IEnumerable<TestTask> GetAllTasks()
        {
            return _activeTasks.Values.ToList();
        }

        /// <summary>
        /// 删除特定ID的测试任务
        /// </summary>
        /// <param name="taskId">待删除的任务ID</param>
        /// <returns>操作是否成功</returns>
        public async Task<bool> RemoveTaskAsync(string taskId)
        {
            if(_activeTasks.TryRemove(taskId, out var task)) { 
                await task.StopAsync(); 
                task.Dispose(); 
                return true;
            } 
            return false; 
        }

        /// <summary>
        /// 添加新的测试任务
        /// </summary>
        /// <param name="task">要添加的测试任务</param>
        /// <returns>操作是否成功</returns>
        public bool AddTask(TestTask task) { 
            if (task == null || string.IsNullOrEmpty(task.Id)) 
            {
                System.Diagnostics.Debug.WriteLine("警告：尝试添加空任务或ID为空的任务。");
                return false;
            }
            if (_activeTasks.ContainsKey(task.Id)) {
                if (_activeTasks.TryRemove(task.Id, out var oldTask))
                {
                    oldTask.Dispose(); 
                    System.Diagnostics.Debug.WriteLine($"信息：已移除并清理了具有相同ID ({task.Id}) 的旧任务，准备添加新任务。");
                }
            }
            bool added = _activeTasks.TryAdd(task.Id, task);
            if(added) 
            {
                System.Diagnostics.Debug.WriteLine($"信息：已添加任务: {task.Id} ({task.ChannelMapping?.VariableName})");
            }
            else 
            {
                System.Diagnostics.Debug.WriteLine($"错误：添加任务失败: {task.Id} ({task.ChannelMapping?.VariableName})");
            }
            return added; 
        }

        /// <summary>
        /// 清空所有测试任务
        /// </summary>
        /// <returns>操作是否成功</returns>
        public async Task<bool> ClearAllTasksAsync()
        {
            await ClearAllTasksAsyncInternal();
            return true;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            // 尝试优雅停止，如果不行则继续Dispose
            try 
            {
                StopAllTasksAsync().GetAwaiter().GetResult(); 
            }
            catch(Exception ex) 
            {
                 System.Diagnostics.Debug.WriteLine($"Dispose时停止任务出错: {ex.Message}");
            }
            _masterCancellationTokenSource?.Cancel(); // 确保CTS被取消
            _masterCancellationTokenSource?.Dispose();
            _semaphore?.Dispose();
            ClearAllTasksAsyncInternal().GetAwaiter().GetResult(); // 清理任务列表
        }

        /// <summary>
        /// 获取特定批次关联的通道映射
        /// </summary>
        /// <param name="batchId">批次ID</param>
        /// <param name="batchName">批次名称</param>
        /// <returns>通道映射集合</returns>
        private async Task<IEnumerable<ChannelMapping>> GetChannelMappingsByBatchAsync(string batchId, string batchName)
        {
            // 使用新增的服务方法直接获取特定批次的通道映射数据
            // 这样可以避免获取所有通道数据再过滤的低效方式
            return await _channelMappingService.GetChannelMappingsByBatchNameAsync(batchName);
        }

        /// <summary>
        /// 通知测试结果已更新
        /// </summary>
        private void NotifyTestResultsUpdated(IEnumerable<Guid> updatedChannelIds = null)
        {
            if (updatedChannelIds != null && updatedChannelIds.Any())
            {
                 _eventAggregator.GetEvent<ChannelStatesModifiedEvent>().Publish(updatedChannelIds.ToList());
            }
            else 
            {
                 _eventAggregator.GetEvent<TestResultsUpdatedEvent>().Publish(); // 通用通知
            }
        }

        /// <summary>
        /// 复测指定的通道。
        /// </summary>
        /// <param name="channelMapping">需要复测的通道映射对象。</param>
        /// <returns>如果复测操作成功执行（无论测试本身是否通过），则返回true。</returns>
        public async Task<bool> RetestChannelAsync(ChannelMapping channelMapping) 
        {
            if (channelMapping == null) 
            {
                await _messageService.ShowAsync("错误", "需要复测的通道信息不能为空。", MessageBoxButton.OK);
                return false;
            }

            // 检查是否正在运行其他测试
            if (_isRunning && (_activeTasks.Any(t => t.Value.ChannelMapping.Id != channelMapping.Id) || _activeTasks.Count > 1) )
            {
                 var continueRetest = await _messageService.ShowAsync("警告", "当前有其他测试正在运行中或已计划。是否仍要单独复测此通道？", MessageBoxButton.YesNo);
                 if (continueRetest != MessageBoxResult.Yes) return false;
                 // 如果用户选择继续，可能需要考虑是否取消现有任务，或以某种方式隔离复测
                 // 目前简单处理为允许继续，但可能会有并发问题，需要更完善的逻辑
            }

            if (!await EnsurePlcConnectionsAsync()) return false;

            // 1. 重置通道状态以便复测
            _channelStateManager.ResetForRetest(channelMapping);
            NotifyTestResultsUpdated(new[] { channelMapping.Id }); // 通知UI状态已重置

            // 2. 创建单个测试任务
            TestTask retestTask = null;
            string taskId = channelMapping.Id.ToString();
            switch (channelMapping.ModuleType?.ToUpper())
            {
                case "AI":
                    retestTask = new AITestTask(taskId, channelMapping, _testPlcCommunication, _targetPlcCommunication);
                    break;
                case "AO":
                    retestTask = new AOTestTask(taskId, channelMapping, _testPlcCommunication, _targetPlcCommunication);
                    break;
                case "DI":
                    retestTask = new DITestTask(taskId, channelMapping, _testPlcCommunication, _targetPlcCommunication);
                    break;
                case "DO":
                    retestTask = new DOTestTask(taskId, channelMapping, _testPlcCommunication, _targetPlcCommunication);
                    break;
                default:
                    await _messageService.ShowAsync("错误", $"不支持的模块类型 '{channelMapping.ModuleType}'，无法为通道 '{channelMapping.VariableName}' 创建复测任务。", MessageBoxButton.OK);
                    return false;
            }

            if (retestTask == null) return false;

            // 清理可能存在的旧任务 (如果ID相同)
            _activeTasks.TryRemove(retestTask.Id, out _);
            // 添加新任务
            if (!_activeTasks.TryAdd(retestTask.Id, retestTask))
            {
                await _messageService.ShowAsync("错误", "将复测任务添加到活动列表失败。", MessageBoxButton.OK);
                return false;
            }
            
            _isRunning = true; // 标记测试正在运行
            _masterCancellationTokenSource?.Dispose();
            _masterCancellationTokenSource = new CancellationTokenSource();

            await ShowTestProgressDialogAsync(true, channelMapping);
            System.Diagnostics.Debug.WriteLine($"通道复测启动: {channelMapping.VariableName} @ {DateTime.Now}");

            try
            {
                await UpdateProgressMessageAsync($"复测中: {channelMapping.VariableName} ({channelMapping.ChannelTag})...");
                // 3. 准备硬点测试状态
                _channelStateManager.BeginHardPointTest(channelMapping, DateTime.Now);
                NotifyTestResultsUpdated(new[] { channelMapping.Id });

                // 4. 执行测试
                HardPointTestRawResult rawResult = await retestTask.RunTestAsync(_masterCancellationTokenSource.Token);
                System.Diagnostics.Debug.WriteLine($"通道 {channelMapping.VariableName} 复测完成. 成功: {rawResult.IsSuccess}");

                // 5. 设置测试结果
                _channelStateManager.SetHardPointTestOutcome(channelMapping, rawResult, DateTime.Now);
                NotifyTestResultsUpdated(new[] { channelMapping.Id });
                
                await _messageService.ShowAsync("复测完成", $"通道 '{channelMapping.VariableName}' 的复测已完成。请检查结果。", MessageBoxButton.OK);
            }
            catch (OperationCanceledException)
            {
                System.Diagnostics.Debug.WriteLine($"通道 {channelMapping.VariableName} 复测被取消.");
                var cancelResult = new HardPointTestRawResult(false, "复测任务被用户取消。");
                _channelStateManager.SetHardPointTestOutcome(channelMapping, cancelResult, DateTime.Now);
                NotifyTestResultsUpdated(new[] { channelMapping.Id });
                await _messageService.ShowAsync("复测取消", $"通道 '{channelMapping.VariableName}' 的复测已被取消。", MessageBoxButton.OK);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"通道 {channelMapping.VariableName} 复测发生错误: {ex.Message}");
                var errorResult = new HardPointTestRawResult(false, $"复测 '{channelMapping.VariableName}' 时发生异常: {ex.Message}");
                _channelStateManager.SetHardPointTestOutcome(channelMapping, errorResult, DateTime.Now);
                NotifyTestResultsUpdated(new[] { channelMapping.Id });
                await _messageService.ShowAsync("复测错误", $"通道 '{channelMapping.VariableName}' 复测过程中发生错误: {ex.Message}", MessageBoxButton.OK);
            }
            finally
            {
                _isRunning = false;
                CloseProgressDialog();
                _activeTasks.TryRemove(retestTask.Id, out _); // 从活动任务中移除
                await UpdateBatchStatusAsync(); // 更新批次状态
            }
            return true;
        }

        #endregion

        #region 私有辅助方法

        private async Task ClearAllTasksAsyncInternal() 
        {
            var tasksToStop = _activeTasks.Values.ToList();
            _activeTasks.Clear(); 
            foreach (var task in tasksToStop)
            {
                try
                {
                    await task.StopAsync(); 
                    task.Dispose();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"停止和释放旧任务 {task.Id} 时出错: {ex.Message}");
                }
            }
        }

        private async Task<bool> EnsurePlcConnectionsAsync()
        {
            if (!_testPlcCommunication.IsConnected)
            {
                var testPlcConnectResult = await _testPlcCommunication.ConnectAsync();
                if (!testPlcConnectResult.IsSuccess)
                {
                    await _messageService.ShowAsync("错误", $"无法连接测试PLC: {testPlcConnectResult.ErrorMessage}", MessageBoxButton.OK);
                    return false;
                }
            }
            if (!_targetPlcCommunication.IsConnected)
            {
                var targetPlcConnectResult = await _targetPlcCommunication.ConnectAsync();
                if (!targetPlcConnectResult.IsSuccess)
                {
                    await _messageService.ShowAsync("错误", $"无法连接被测PLC: {targetPlcConnectResult.ErrorMessage}", MessageBoxButton.OK);
                    return false;
                }
            }
            return true;
        }

        private async Task UpdateBatchStatusAsync() { 
            System.Diagnostics.Debug.WriteLine("信息: TestTaskManager.UpdateBatchStatusAsync 被调用（已废弃），批次状态更新应由ViewModel处理。");
            await Task.CompletedTask;
        }

        #endregion

        // StartAllTasks_Refactored 和 RetestChannelAsync 将在下一步骤中实现
        // ... (其他方法存根) ...
        [Obsolete("使用 StartAllTasksSerialAsync_Refactored (串行) 或 StartAllTasksAsync (并发) 替代此方法名")]
        public async Task<bool> StartAllTasksSerialAsync() { 
            System.Diagnostics.Debug.WriteLine("StartAllTasksSerialAsync (Obsolete) called. Refactored version not implemented. Returning false.");
            return await Task.FromResult(false); // Placeholder if StartAllTasksSerialAsync_Refactored is not yet implemented
        }
        [Obsolete("使用 RetestChannelAsync 替代此方法")]
        public async Task<bool> RetestChannelSerialAsync(ChannelMapping channelMapping) { 
            return await RetestChannelAsync(channelMapping); 
        }
    }
}
