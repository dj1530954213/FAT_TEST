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

namespace FatFullVersion.Services
{
    /// <summary>
    /// 测试任务管理器，负责管理所有测试任务的创建、启动、停止和管理
    /// </summary>
    public class TestTaskManager : ITestTaskManager
    {
        #region 字段

        private readonly IChannelMappingService _channelMappingService;
        private readonly IPlcCommunication _testPlcCommunication; // 测试PLC通信实例
        private readonly IPlcCommunication _targetPlcCommunication; // 被测PLC通信实例

        // 使用线程安全的集合存储所有活动的测试任务
        private readonly ConcurrentDictionary<string, TestTask> _activeTasks;
        
        // 用于管理任务执行和取消的令牌源
        private CancellationTokenSource _masterCancellationTokenSource;
        
        // 并行任务选项，用于限制并发任务数量
        private readonly ParallelOptions _parallelOptions;

        // 任务状态
        private bool _isRunning;

        // 信号量，用于限制同时执行的任务数量
        private readonly SemaphoreSlim _semaphore;

        #endregion

        #region 构造函数

        /// <summary>
        /// 创建测试任务管理器实例
        /// </summary>
        /// <param name="channelMappingService">通道映射服务</param>
        /// <param name="testPlcCommunication">测试PLC通信实例</param>
        /// <param name="targetPlcCommunication">被测PLC通信实例</param>
        /// <param name="maxConcurrentTasks">最大并发任务数量，默认为处理器核心数的2倍</param>
        public TestTaskManager(
            IChannelMappingService channelMappingService,
            IPlcCommunication testPlcCommunication,
            IPlcCommunication targetPlcCommunication,
            int? maxConcurrentTasks = null)
        {
            _channelMappingService = channelMappingService ?? throw new ArgumentNullException(nameof(channelMappingService));
            _testPlcCommunication = testPlcCommunication ?? throw new ArgumentNullException(nameof(testPlcCommunication));
            _targetPlcCommunication = targetPlcCommunication ?? throw new ArgumentNullException(nameof(targetPlcCommunication));
            
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
        /// 启动所有测试任务
        /// </summary>
        /// <returns>操作是否成功</returns>
        public async Task<bool> StartAllTasksAsync()
        {
            if (_isRunning)
                return false;

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
                    //return false;
                }
            });

            return true;
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

        #endregion
    }
}
