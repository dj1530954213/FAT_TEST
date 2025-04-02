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

namespace FatFullVersion.Services
{
    /// <summary>
    /// 测试任务管理器，负责管理所有测试任务的创建、启动、停止和管理
    /// </summary>
    public class TestTaskManager : ITestTaskManager
    {
        #region 字段

        private readonly IChannelMappingService _channelMappingService;
        private readonly IPlcCommunication _testPlcCommunication;
        private readonly IPlcCommunication _targetPlcCommunication;
        private readonly IMessageService _messageService;
        private readonly ConcurrentDictionary<string, TestTask> _activeTasks;
        private CancellationTokenSource _masterCancellationTokenSource;
        private readonly ParallelOptions _parallelOptions;
        private bool _isRunning;
        private readonly SemaphoreSlim _semaphore;
        private bool _isWiringCompleted;
        private BatchInfo _currentBatch;
        private Window _progressDialog;
        private readonly object _dialogLock = new object();

        #endregion

        #region 构造函数

        /// <summary>
        /// 创建测试任务管理器实例
        /// </summary>
        /// <param name="channelMappingService">通道映射服务</param>
        /// <param name="serviceLocator">服务定位器</param>
        /// <param name="messageService">消息服务</param>
        /// <param name="maxConcurrentTasks">最大并发任务数量，默认为处理器核心数的2倍</param>
        public TestTaskManager(
            IChannelMappingService channelMappingService,
            IServiceLocator serviceLocator,
            IMessageService messageService,
            int? maxConcurrentTasks = null)
        {
            _channelMappingService = channelMappingService ?? throw new ArgumentNullException(nameof(channelMappingService));
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
            //使用ServiceLocator这个单例服务来完成在APP的注册(使用同一个接口但是通过名称来区分实例)
            _testPlcCommunication = serviceLocator.ResolveNamed<IPlcCommunication>("TestPlcCommunication");
            _targetPlcCommunication = serviceLocator.ResolveNamed<IPlcCommunication>("TargetPlcCommunication");
            
            _activeTasks = new ConcurrentDictionary<string, TestTask>();
            _masterCancellationTokenSource = new CancellationTokenSource();
            
            // 设置并行任务的最大并发数量
            int concurrentTasks = maxConcurrentTasks ?? Environment.ProcessorCount * 2;
            _parallelOptions = new ParallelOptions 
            { 
                MaxDegreeOfParallelism = concurrentTasks,
                CancellationToken = _masterCancellationTokenSource.Token
            };
            
            // 创建信号量以限制并发执行的任务数量
            _semaphore = new SemaphoreSlim(concurrentTasks, concurrentTasks);
            
            _isRunning = false;
            _isWiringCompleted = false;
            _currentBatch = null;
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

            // 按模块类型分类
            var aiChannels = channelMappings.Where(c => c.ModuleType?.ToLower() == "ai").ToList();
            var aoChannels = channelMappings.Where(c => c.ModuleType?.ToLower() == "ao").ToList();
            var diChannels = channelMappings.Where(c => c.ModuleType?.ToLower() == "di").ToList();
            var doChannels = channelMappings.Where(c => c.ModuleType?.ToLower() == "do").ToList();

            // 为每种类型的通道创建相应的测试任务
            taskIds.AddRange(await CreateAITasksAsync(aiChannels));
            taskIds.AddRange(await CreateAOTasksAsync(aoChannels));
            taskIds.AddRange(await CreateDITasksAsync(diChannels));
            taskIds.AddRange(await CreateDOTasksAsync(doChannels));

            return taskIds;
        }

        /// <summary>
        /// 确认接线已完成，启用测试功能
        /// </summary>
        /// <param name="batchInfo">批次信息</param>
        /// <returns>确认操作是否成功</returns>
        //public async Task<bool> ConfirmWiringCompleteAsync(BatchInfo batchInfo)
        //{
        //    // 默认自动开始测试
        //    return await ConfirmWiringCompleteAsync(batchInfo, true);
        //}

        /// <summary>
        /// 确认接线已完成，启用测试功能，可选择是否自动开始测试
        /// </summary>
        /// <param name="batchInfo">批次信息</param>
        /// <param name="autoStart">是否自动开始测试</param>
        /// <returns>确认操作是否成功</returns>
        public async Task<bool> ConfirmWiringCompleteAsync(BatchInfo batchInfo, bool autoStart,IEnumerable<ChannelMapping> testMap)
        {
            if (batchInfo == null)
                return false;

            // 保存当前批次信息
            _currentBatch = batchInfo;
            
            // 检查PLC连接状态
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

            // 向用户确认接线已完成
            var confirmResult = await _messageService.ShowAsync("确认", "确认已完成接线？", MessageBoxButton.YesNo);
            if (confirmResult == MessageBoxResult.Yes)
            {
                // 设置接线已完成的标志
                _isWiringCompleted = true;
                
                // 选择当前批次的通道
                var channelMappings = testMap.Where(c => c.TestBatch?.Equals(batchInfo.BatchName) == true).ToList();
                
                // 确保所有通道都使用批次名称而不是ID
                foreach (var channel in channelMappings)
                {
                    // 明确设置TestBatch为BatchName，避免可能的误用BatchId
                    channel.TestBatch = batchInfo.BatchName;
                }
                
                // 创建测试任务
                await CreateTestTasksAsync(channelMappings);

                // 如果需要自动开始测试
                if (autoStart)
                {
                    // 显示等待对话框
                    await ShowTestProgressDialogAsync(false, null);
                    
                    // 开始测试
                    await StartAllTasksAsync();
                }
                
                return true;
            }

            _isWiringCompleted = false;
            return false;
        }

        /// <summary>
        /// 显示测试进度对话框
        /// </summary>
        /// <param name="isRetestMode">是否为复测模式，默认为false表示全自动测试</param>
        /// <param name="channelInfo">复测的通道信息（复测模式下使用）</param>
        /// <returns>异步任务</returns>
        public async Task ShowTestProgressDialogAsync(bool isRetestMode = false, ChannelMapping channelInfo = null)
        {
            await Task.Run(() =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    lock (_dialogLock)
                    {
                        if (_progressDialog != null && _progressDialog.IsVisible)
                            return;

                        // 创建Grid布局容器
                        var grid = new Grid();
                        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 0: 标题
                        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(10) }); // 1: 间距
                        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 2: 批次信息
                        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(10) }); // 3: 间距
                        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 4: 进度条
                        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(10) }); // 5: 间距
                        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 6: 提示文本

                        // 根据不同模式设置不同的标题和信息
                        string title = isRetestMode ? "通道复测进行中" : "自动测试进行中";
                        string info = isRetestMode ? "请等待通道复测完成..." : "请等待测试完成...";
                        string batchInfo = string.Empty;

                        if (isRetestMode && channelInfo != null)
                        {
                            batchInfo = $"批次: {channelInfo.TestBatch}, 通道: {channelInfo.VariableName}";
                        }
                        else
                        {
                            batchInfo = _currentBatch != null ? $"批次: {_currentBatch.BatchName}" : "批次: 未知";
                        }

                        // 添加标题文本
                        var titleTextBlock = new TextBlock
                        {
                            Text = title,
                            FontSize = 20,
                            FontWeight = FontWeights.Bold,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Foreground = new SolidColorBrush(Color.FromRgb(25, 118, 210)),
                            Margin = new Thickness(0, 15, 0, 0)
                        };
                        Grid.SetRow(titleTextBlock, 0);
                        grid.Children.Add(titleTextBlock);

                        // 添加批次信息
                        var batchInfoTextBlock = new TextBlock
                        {
                            Text = batchInfo,
                            FontSize = 14,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Margin = new Thickness(0, 5, 0, 0)
                        };
                        Grid.SetRow(batchInfoTextBlock, 2);
                        grid.Children.Add(batchInfoTextBlock);

                        // 添加MaterialDesign进度指示器
                        var progressBar = new ProgressBar
                        {
                            IsIndeterminate = true,
                            Style = (Style)Application.Current.Resources["MaterialDesignCircularProgressBar"],
                            Width = 60,
                            Height = 60,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80))
                        };
                        Grid.SetRow(progressBar, 4);
                        grid.Children.Add(progressBar);

                        // 添加提示文本
                        var infoTextBlock = new TextBlock
                        {
                            Text = info,
                            FontSize = 14,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Margin = new Thickness(0, 5, 0, 15)
                        };
                        Grid.SetRow(infoTextBlock, 6);
                        grid.Children.Add(infoTextBlock);

                        // 创建等待对话框
                        _progressDialog = new Window
                        {
                            Title = title,
                            Width = 350,
                            Height = 250,
                            WindowStartupLocation = WindowStartupLocation.CenterScreen,
                            Content = grid,
                            ResizeMode = ResizeMode.NoResize,
                            WindowStyle = WindowStyle.ToolWindow,
                            Background = new SolidColorBrush(Colors.WhiteSmoke),
                            Topmost = true // 确保对话框始终在最前面
                        };

                        // 显示对话框（非模态）
                        _progressDialog.Show();
                    }
                });
            });
        }

        /// <summary>
        /// 关闭测试进度对话框
        /// </summary>
        private void CloseProgressDialog()
        {
            try
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
            catch (Exception ex)
            {
                Console.WriteLine($"关闭进度对话框时发生错误: {ex.Message}");
                // 尝试强制关闭
                try
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _progressDialog = null;
                    });
                }
                catch { /* 忽略可能发生的任何错误 */ }
            }
        }

        /// <summary>
        /// 启动所有测试任务
        /// </summary>
        /// <returns>操作是否成功</returns>
        public async Task<bool> StartAllTasksAsync()
        {
            if (_isRunning)
                return false;

            if (!_isWiringCompleted)
            {
                await _messageService.ShowAsync("警告", "请先确认完成接线后再开始测试", MessageBoxButton.OK);
                return false;
            }

            _isRunning = true;
            
            // 确保取消令牌源是新的且有效的
            if (_masterCancellationTokenSource.IsCancellationRequested)
            {
                _masterCancellationTokenSource.Dispose();
                _masterCancellationTokenSource = new CancellationTokenSource();
                _parallelOptions.CancellationToken = _masterCancellationTokenSource.Token;
            }

            // 异步启动所有任务
            await Task.Run(async () =>
            {
                try
                {
                    // 使用ForEachAsync按照最大并发数执行所有任务
                    await ForEachAsyncWithThrottling(_activeTasks.Values, async (task) =>
                    {
                        try
                        {
                            await task.StartAsync(_masterCancellationTokenSource.Token);
                            
                            // 测试完成后，同步更新原始通道的硬点测试结果
                            SyncHardPointTestResult(task);
                        }
                        catch (Exception ex)
                        {
                            // 记录异常但不中断其他任务的执行
                            Console.WriteLine($"任务执行错误: {ex.Message}");
                        }
                    }, _parallelOptions.MaxDegreeOfParallelism);
                }
                catch (OperationCanceledException)
                {
                    // 任务取消，正常处理
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"启动任务时出错: {ex.Message}");
                }
                finally
                {
                    // 所有任务完成或取消后，关闭进度对话框并更新状态
                    _isRunning = false;
                    CloseProgressDialog();

                    // 更新批次状态
                    await UpdateBatchStatusAsync();

                    // 通知UI刷新显示
                    NotifyTestResultsUpdated();

                    // 显示测试完成消息
                    await Application.Current.Dispatcher.InvokeAsync(async () =>
                    {
                        await _messageService.ShowAsync("测试完成", "所有硬点测试已完成", MessageBoxButton.OK);
                    });
                }
            });

            return true;
        }

        /// <summary>
        /// 同步硬点测试结果到原始通道集合
        /// </summary>
        /// <param name="task">测试任务</param>
        private void SyncHardPointTestResult(TestTask task)
        {
            if (task?.Result == null || task.ChannelMapping == null)
                return;

            try
            {
                // 获取任务对应的原始通道映射
                var channelMappings = _channelMappingService.GetChannelMappingsByBatchNameAsync(task.ChannelMapping.TestBatch)
                    .GetAwaiter().GetResult();
                    
                if (channelMappings == null || !channelMappings.Any())
                {
                    Console.WriteLine($"未找到批次 {task.ChannelMapping.TestBatch} 的通道映射");
                    return;
                }

                var originalChannel = channelMappings.FirstOrDefault(c => 
                    c.VariableName == task.ChannelMapping.VariableName && 
                    c.ChannelTag == task.ChannelMapping.ChannelTag);

                if (originalChannel == null)
                {
                    Console.WriteLine($"未找到变量名为 {task.ChannelMapping.VariableName} 的原始通道");
                    return;
                }

                // 将测试状态同步到硬点测试结果中
                // 如果测试成功，则HardPointTestResult显示"通过"
                // 如果测试失败，则HardPointTestResult显示"失败"，并将详细错误信息保存在ErrorMessage中
                if (task.Result.Status == "通过")
                {
                    originalChannel.HardPointTestResult = "通过";
                    originalChannel.TestResultStatus = 1; // 成功
                }
                else if (task.Result.Status.Contains("失败"))
                {
                    originalChannel.HardPointTestResult = "失败";
                    originalChannel.TestResultStatus = 2; // 失败
                    
                    // 如果有错误信息，则保存到ErrorMessage中
                    if (!string.IsNullOrEmpty(task.Result.ErrorMessage))
                    {
                        originalChannel.ErrorMessage = task.Result.ErrorMessage;
                    }
                }
                
                // 同步测试完成时间
                originalChannel.TestTime = DateTime.Now;
                originalChannel.EndTime = task.Result.EndTime;
                
                // 同步测试状态
                originalChannel.Status = task.Result.Status;
                
                // 同步预期值和实际值
                originalChannel.ExpectedValue = task.Result.ExpectedValue;
                originalChannel.ActualValue = task.Result.ActualValue;
                
                // 根据模块类型同步特定数据
                switch (task.ChannelMapping.ModuleType?.ToLower())
                {
                    case "ai":
                        // 同步AI测试的百分比值
                        originalChannel.Value0Percent = task.Result.Value0Percent;
                        originalChannel.Value25Percent = task.Result.Value25Percent;
                        originalChannel.Value50Percent = task.Result.Value50Percent;
                        originalChannel.Value75Percent = task.Result.Value75Percent;
                        originalChannel.Value100Percent = task.Result.Value100Percent;
                        break;
                        
                    case "ao":
                        // 同步AO测试的值
                        originalChannel.Value0Percent = task.Result.Value0Percent;
                        originalChannel.Value25Percent = task.Result.Value25Percent;
                        originalChannel.Value50Percent = task.Result.Value50Percent;
                        originalChannel.Value75Percent = task.Result.Value75Percent;
                        originalChannel.Value100Percent = task.Result.Value100Percent;
                        break;
                        
                    case "di":
                    case "do":
                        // 同步DI/DO测试状态
                        break;
                }
                
                Console.WriteLine($"成功同步通道 {originalChannel.VariableName} 的测试结果: {originalChannel.HardPointTestResult}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"同步测试结果时出错: {ex.Message}");
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
        public async Task<bool> CompleteAllTestsAsync()
        {
            if (_currentBatch != null)
            {
                _currentBatch.Status = BatchTestStatus.Completed.ToString();
                _currentBatch.LastTestTime = DateTime.Now;
                
                // 通知UI刷新显示
                NotifyTestResultsUpdated();
                
                return true;
            }
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
                Console.WriteLine($"停止任务时出错: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 暂停所有测试任务
        /// </summary>
        /// <returns>操作是否成功</returns>
        public async Task<bool> PauseAllTasksAsync()
        {
            if (!_isRunning)
                return false;

            try
            {
                // 对每个任务执行暂停操作
                await Task.WhenAll(_activeTasks.Values.Select(t => t.PauseAsync()));
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"暂停任务时出错: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 恢复所有测试任务
        /// </summary>
        /// <returns>操作是否成功</returns>
        public async Task<bool> ResumeAllTasksAsync()
        {
            try
            {
                // 对每个任务执行恢复操作
                await Task.WhenAll(_activeTasks.Values.Select(t => t.ResumeAsync()));
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"恢复任务时出错: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 根据ID获取测试任务
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <returns>测试任务实例，如果不存在则返回null</returns>
        public TestTask GetTaskById(string taskId)
        {
            if (string.IsNullOrEmpty(taskId) || !_activeTasks.ContainsKey(taskId))
                return null;

            return _activeTasks[taskId];
        }

        /// <summary>
        /// 根据通道映射获取测试任务
        /// </summary>
        /// <param name="channelMapping">通道映射实例</param>
        /// <returns>测试任务实例，如果不存在则返回null</returns>
        public TestTask GetTaskByChannel(ChannelMapping channelMapping)
        {
            if (channelMapping == null)
                return null;

            return _activeTasks.Values.FirstOrDefault(t => t.ChannelMapping.VariableName == channelMapping.VariableName);
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
            if (string.IsNullOrEmpty(taskId) || !_activeTasks.ContainsKey(taskId))
                return false;

            if (_activeTasks.TryRemove(taskId, out TestTask task))
            {
                // 确保任务停止
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
        public bool AddTask(TestTask task)
        {
            if (task == null || _activeTasks.ContainsKey(task.Id))
                return false;

            return _activeTasks.TryAdd(task.Id, task);
        }

        /// <summary>
        /// 清空所有测试任务
        /// </summary>
        /// <returns>操作是否成功</returns>
        public async Task<bool> ClearAllTasksAsync()
        {
            try
            {
                // 先停止所有任务
                await StopAllTasksAsync();

                // 逐个删除并释放资源
                foreach (var task in _activeTasks.Values)
                {
                    task.Dispose();
                }

                // 清空集合
                _activeTasks.Clear();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"清空任务时出错: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            // 停止所有任务
            StopAllTasksAsync().Wait();

            // 关闭进度对话框
            CloseProgressDialog();

            // 释放资源
            _masterCancellationTokenSource.Dispose();
            _semaphore.Dispose();

            // 清理所有任务资源
            foreach (var task in _activeTasks.Values)
            {
                task.Dispose();
            }
            
            // 清空任务集合
            _activeTasks.Clear();
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
        private void NotifyTestResultsUpdated()
        {
            try
            {
                // 使用反射获取事件聚合器
                var eventAggregatorField = this.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .FirstOrDefault(f => f.FieldType.Name.Contains("EventAggregator"));

                if (eventAggregatorField != null)
                {
                    var eventAggregator = eventAggregatorField.GetValue(this);
                    
                    // 发布测试结果更新事件
                    var publishMethod = eventAggregator.GetType().GetMethod("Publish");
                    if (publishMethod != null)
                    {
                        // 找一个TestResultsUpdatedEvent类型或创建一个空对象
                        var eventInstance = Activator.CreateInstance(Type.GetType("FatFullVersion.Events.TestResultsUpdatedEvent, FatFullVersion") 
                            ?? typeof(object));
                            
                        publishMethod.Invoke(eventAggregator, new[] { eventInstance });
                        
                        Console.WriteLine("已通知UI刷新测试结果");
                    }
                }
                else
                {
                    // 直接通过调度器尝试刷新
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        // 尝试获取主要ViewModel
                        var mainWindow = Application.Current.MainWindow;
                        if (mainWindow != null)
                        {
                            var dataContext = mainWindow.DataContext;
                            if (dataContext != null)
                            {
                                // 尝试刷新属性
                                var propertyInfo = dataContext.GetType().GetProperty("TestResults");
                                if (propertyInfo != null)
                                {
                                    propertyInfo.SetValue(dataContext, propertyInfo.GetValue(dataContext));
                                    Console.WriteLine("已通过主窗口刷新测试结果");
                                }
                            }
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"通知UI更新失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 对单个通道进行复测
        /// </summary>
        /// <param name="channelMapping">需要复测的通道映射</param>
        /// <returns>操作是否成功</returns>
        public async Task<bool> RetestChannelAsync(ChannelMapping channelMapping)
        {
            if (channelMapping == null)
                return false;

            try
            {
                // 移除旧的相关测试任务
                var existingTask = GetTaskByChannel(channelMapping);
                if (existingTask != null)
                {
                    // 如果任务正在运行，先停止
                    if (existingTask.Status == TestTaskStatus.Running || existingTask.Status == TestTaskStatus.Paused)
                    {
                        await existingTask.StopAsync();
                    }
                    
                    // 从活跃任务列表中移除
                    await RemoveTaskAsync(existingTask.Id);
                }

                // 根据通道类型创建并启动新的测试任务
                string taskId = string.Empty;
                List<string> taskIds = new List<string>();
                
                switch (channelMapping.ModuleType?.ToLower())
                {
                    case "ai":
                        taskIds = (await CreateAITasksAsync(new[] { channelMapping })).ToList();
                        break;
                    case "ao":
                        taskIds = (await CreateAOTasksAsync(new[] { channelMapping })).ToList();
                        break;
                    case "di":
                        taskIds = (await CreateDITasksAsync(new[] { channelMapping })).ToList();
                        break;
                    case "do":
                        taskIds = (await CreateDOTasksAsync(new[] { channelMapping })).ToList();
                        break;
                    default:
                        return false;
                }

                // 如果成功创建了任务，启动它
                if (taskIds.Count > 0)
                {
                    taskId = taskIds[0];
                    var newTask = GetTaskById(taskId);
                    if (newTask != null)
                    {
                        // 设置测试起始时间
                        channelMapping.TestTime = DateTime.Now;
                        channelMapping.TestResultStatus = 0; // 重置结果状态为未测试
                        channelMapping.HardPointTestResult = "正在复测中...";

                        // 显示进度对话框
                        await ShowTestProgressDialogAsync(true, channelMapping);
                        
                        // 启动任务
                        await newTask.StartAsync();
                        
                        // 同步任务结果到原始通道
                        //SyncHardPointTestResult(newTask);
                        
                        // 关闭进度对话框
                        CloseProgressDialog();
                        
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _messageService.ShowAsync("错误", $"复测通道失败: {ex.Message}", MessageBoxButton.OK);
                return false;
            }
        }

        #endregion

        #region 私有辅助方法

        /// <summary>
        /// 创建AI类型的测试任务
        /// </summary>
        /// <param name="aiChannels">AI通道映射集合</param>
        /// <returns>创建的任务ID集合</returns>
        private async Task<IEnumerable<string>> CreateAITasksAsync(IEnumerable<ChannelMapping> aiChannels)
        {
            List<string> taskIds = new List<string>();

            foreach (var channel in aiChannels)
            {
                // 确保使用批次名称而不是ID
                if (_currentBatch != null && string.IsNullOrEmpty(channel.TestBatch))
                {
                    channel.TestBatch = _currentBatch.BatchName;
                }
                
                string taskId = Guid.NewGuid().ToString();
                var task = new AITestTask(
                    taskId,
                    channel,
                    _testPlcCommunication,
                    _targetPlcCommunication);
                
                if (_activeTasks.TryAdd(taskId, task))
                {
                    taskIds.Add(taskId);
                }
            }

            return await Task.FromResult(taskIds);
        }

        /// <summary>
        /// 创建AO类型的测试任务
        /// </summary>
        /// <param name="aoChannels">AO通道映射集合</param>
        /// <returns>创建的任务ID集合</returns>
        private async Task<IEnumerable<string>> CreateAOTasksAsync(IEnumerable<ChannelMapping> aoChannels)
        {
            List<string> taskIds = new List<string>();

            foreach (var channel in aoChannels)
            {
                // 确保使用批次名称而不是ID
                if (_currentBatch != null && string.IsNullOrEmpty(channel.TestBatch))
                {
                    channel.TestBatch = _currentBatch.BatchName;
                }
                
                string taskId = Guid.NewGuid().ToString();
                var task = new AOTestTask(
                    taskId,
                    channel,
                    _testPlcCommunication,
                    _targetPlcCommunication);
                
                if (_activeTasks.TryAdd(taskId, task))
                {
                    taskIds.Add(taskId);
                }
            }

            return await Task.FromResult(taskIds);
        }

        /// <summary>
        /// 创建DI类型的测试任务
        /// </summary>
        /// <param name="diChannels">DI通道映射集合</param>
        /// <returns>创建的任务ID集合</returns>
        private async Task<IEnumerable<string>> CreateDITasksAsync(IEnumerable<ChannelMapping> diChannels)
        {
            List<string> taskIds = new List<string>();

            foreach (var channel in diChannels)
            {
                // 确保使用批次名称而不是ID
                if (_currentBatch != null && string.IsNullOrEmpty(channel.TestBatch))
                {
                    channel.TestBatch = _currentBatch.BatchName;
                }
                
                string taskId = Guid.NewGuid().ToString();
                var task = new DITestTask(
                    taskId,
                    channel,
                    _testPlcCommunication,
                    _targetPlcCommunication);
                
                if (_activeTasks.TryAdd(taskId, task))
                {
                    taskIds.Add(taskId);
                }
            }

            return await Task.FromResult(taskIds);
        }

        /// <summary>
        /// 创建DO类型的测试任务
        /// </summary>
        /// <param name="doChannels">DO通道映射集合</param>
        /// <returns>创建的任务ID集合</returns>
        private async Task<IEnumerable<string>> CreateDOTasksAsync(IEnumerable<ChannelMapping> doChannels)
        {
            List<string> taskIds = new List<string>();

            foreach (var channel in doChannels)
            {
                // 确保使用批次名称而不是ID
                if (_currentBatch != null && string.IsNullOrEmpty(channel.TestBatch))
                {
                    channel.TestBatch = _currentBatch.BatchName;
                }
                
                string taskId = Guid.NewGuid().ToString();
                var task = new DOTestTask(
                    taskId,
                    channel,
                    _testPlcCommunication,
                    _targetPlcCommunication);
                
                if (_activeTasks.TryAdd(taskId, task))
                {
                    taskIds.Add(taskId);
                }
            }

            return await Task.FromResult(taskIds);
        }

        /// <summary>
        /// 实现受限并发的异步ForEach方法
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="source">数据源</param>
        /// <param name="body">对每个元素执行的方法</param>
        /// <param name="maxDegreeOfParallelism">最大并行度</param>
        /// <returns>异步任务</returns>
        private async Task ForEachAsyncWithThrottling<T>(IEnumerable<T> source, Func<T, Task> body, int maxDegreeOfParallelism)
        {
            // 创建任务列表
            var tasks = new List<Task>();
            
            foreach (var item in source)
            {
                // 等待信号量，限制并发数
                await _semaphore.WaitAsync(_masterCancellationTokenSource.Token);
                
                // 创建并启动任务
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        // 执行主体方法
                        await body(item);
                    }
                    finally
                    {
                        // 释放信号量
                        _semaphore.Release();
                    }
                }, _masterCancellationTokenSource.Token));
            }
            
            // 等待所有任务完成
            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// 更新批次状态逻辑
        /// </summary>
        private async Task UpdateBatchStatusAsync()
        {
            if (_currentBatch != null)
            {
                // 更新批次状态
                _currentBatch.Status = GetOverallBatchStatus().ToString();
                _currentBatch.LastTestTime = DateTime.Now;

                try
                {
                    // 通知ViewModel更新批次状态
                    // 这里可以使用事件聚合器发布消息，让ViewModel订阅并更新UI
                    // 或者由ViewModel在适当的时机调用服务来刷新批次状态
                    
                    // 如果还有其他需要保存的操作，可以在这里进行
                    // 例如将测试结果保存到数据库等
                }
                catch (Exception ex)
                {
                    await _messageService.ShowAsync("错误", $"更新批次状态时出错: {ex.Message}", MessageBoxButton.OK);
                }
            }
        }

        #endregion
    }
}
