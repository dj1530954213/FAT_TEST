using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FatFullVersion.IServices;
using FatFullVersion.Models;
using FatFullVersion.Entities;
using FatFullVersion.Entities.EntitiesEnum;
using FatFullVersion.Entities.ValueObject;
using System.Windows;
using FatFullVersion.ViewModels;
using System.Collections.ObjectModel;

namespace FatFullVersion.Services
{
    /// <summary>
    /// 通道映射服务实现类，提供通道分配和管理功能的具体实现
    /// </summary>
    public class ChannelMappingService : IChannelMappingService
    {
        private readonly IRepository _repository;
        private readonly IChannelStateManager _channelStateManager;

        /// <summary>
        /// 当前使用的测试PLC配置
        /// </summary>
        private TestPlcConfig _testPlcConfig;

        /// <summary>
        /// 构造函数
        /// </summary>
        public ChannelMappingService(IRepository repository, IChannelStateManager channelStateManager)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _channelStateManager = channelStateManager ?? throw new ArgumentNullException(nameof(channelStateManager));
            // 初始化默认PLC配置
            _testPlcConfig = new TestPlcConfig
            {
                BrandType = PlcBrandTypeEnum.Micro850,
                IpAddress = "127.0.0.1",
                CommentsTables = new List<ComparisonTable>()
            };

            // 默认添加一些通道映射关系
            //InitializeDefaultChannelMappings();
        }

        /// <summary>
        /// 初始化默认的通道映射关系,测试使用后续使用数据库接口获取
        /// </summary>
        //private void InitializeDefaultChannelMappings()
        //{
        //    // 添加默认AI通道
        //    for (int i = 1; i <= _defaultTestPlcConfig.AiModules; i++)
        //    {
        //        for (int j = 1; j <= _defaultTestPlcConfig.AiChannelsPerModule; j++)
        //        {
        //            _testPlcConfig.CommentsTables.Add(new ComparisonTable(
        //                $"AI{i}_{j}",
        //                $"AI{i}.{j}",
        //                TestPlcChannelType.AI));
        //        }
        //    }

        //    // 添加默认AO通道
        //    for (int i = 1; i <= _defaultTestPlcConfig.AoModules; i++)
        //    {
        //        for (int j = 1; j <= _defaultTestPlcConfig.AoChannelsPerModule; j++)
        //        {
        //            _testPlcConfig.CommentsTables.Add(new ComparisonTable(
        //                $"AO{i}_{j}",
        //                $"AO{i}.{j}",
        //                TestPlcChannelType.AO));
        //        }
        //    }

        //    // 添加默认DI通道
        //    for (int i = 1; i <= _defaultTestPlcConfig.DiModules; i++)
        //    {
        //        for (int j = 1; j <= _defaultTestPlcConfig.DiChannelsPerModule; j++)
        //        {
        //            _testPlcConfig.CommentsTables.Add(new ComparisonTable(
        //                $"DI{i}_{j}",
        //                $"DI{i}.{j}",
        //                TestPlcChannelType.DI));
        //        }
        //    }

        //    // 添加默认DO通道
        //    for (int i = 1; i <= _defaultTestPlcConfig.DoModules; i++)
        //    {
        //        for (int j = 1; j <= _defaultTestPlcConfig.DoChannelsPerModule; j++)
        //        {
        //            _testPlcConfig.CommentsTables.Add(new ComparisonTable(
        //                $"DO{i}_{j}",
        //                $"DO{i}.{j}",
        //                TestPlcChannelType.DO));
        //        }
        //    }
        //}

        /// <summary>
        /// 原有的通道分配方法，兼容旧版本
        /// </summary>
        /// <param name="aiChannels">被测PLC的AI通道列表</param>
        /// <param name="aoChannels">被测PLC的AO通道列表</param>
        /// <param name="diChannels">被测PLC的DI通道列表</param>
        /// <param name="doChannels">被测PLC的DO通道列表</param>
        /// <param name="testAoModuleCount">测试PLC的AO模块数量</param>
        /// <param name="testAoChannelPerModule">每个AO模块的通道数</param>
        /// <param name="testAiModuleCount">测试PLC的AI模块数量</param>
        /// <param name="testAiChannelPerModule">每个AI模块的通道数</param>
        /// <param name="testDoModuleCount">测试PLC的DO模块数量</param>
        /// <param name="testDoChannelPerModule">每个DO模块的通道数</param>
        /// <param name="testDiModuleCount">测试PLC的DI模块数量</param>
        /// <param name="testDiChannelPerModule">每个DI模块的通道数</param>
        /// <returns>分配通道后的通道映射信息</returns>
        //public async Task<(IEnumerable<ChannelMapping> AI, IEnumerable<ChannelMapping> AO, IEnumerable<ChannelMapping> DI, IEnumerable<ChannelMapping> DO)> 
        //    AllocateChannelsAsync(
        //        IEnumerable<ChannelMapping> aiChannels,
        //        IEnumerable<ChannelMapping> aoChannels,
        //        IEnumerable<ChannelMapping> diChannels,
        //        IEnumerable<ChannelMapping> doChannels,
        //        int testAoModuleCount, int testAoChannelPerModule,
        //        int testAiModuleCount, int testAiChannelPerModule,
        //        int testDoModuleCount, int testDoChannelPerModule,
        //        int testDiModuleCount, int testDiChannelPerModule)
        //{
        //    _testPlcConfig.CommentsTables = await _repository.GetComparisonTablesAsync();
        //    // 为避免阻塞UI线程，异步执行分配操作
        //    return await Task.Run(() =>
        //    {
        //        // 计算测试PLC各类型通道的总数量
        //        int totalTestAoChannels = testAoModuleCount * testAoChannelPerModule;
        //        int totalTestAiChannels = testAiModuleCount * testAiChannelPerModule;
        //        int totalTestDoChannels = testDoModuleCount * testDoChannelPerModule;
        //        int totalTestDiChannels = testDiModuleCount * testDiChannelPerModule;

        //        // 转换为列表以便修改
        //        var aiList = aiChannels.ToList();
        //        var aoList = aoChannels.ToList();
        //        var diList = diChannels.ToList();
        //        var doList = doChannels.ToList();

        //        // 1. 为AI通道分配批次和测试PLC的AO通道(AI-AO)
        //        AllocateChannels(aiList, totalTestAoChannels, "AO", testAoModuleCount, testAoChannelPerModule);

        //        // 2. 为AO通道分配批次和测试PLC的AI通道(AO-AI)
        //        AllocateChannels(aoList, totalTestAiChannels, "AI", testAiModuleCount, testAiChannelPerModule);

        //        // 3. 为DI通道分配批次和测试PLC的DO通道(DI-DO)
        //        AllocateChannels(diList, totalTestDoChannels, "DO", testDoModuleCount, testDoChannelPerModule);

        //        // 4. 为DO通道分配批次和测试PLC的DI通道(DO-DI)
        //        AllocateChannels(doList, totalTestDiChannels, "DI", testDiModuleCount, testDiChannelPerModule);

        //        return (aiList, aoList, diList, doList);
        //    });
        //}

        /// <summary>
        /// 为指定类型的通道分配测试PLC通道和批次
        /// </summary>
        /// <param name="channels">通道列表</param>
        /// <param name="totalTestChannels">测试PLC通道总数</param>
        /// <param name="testChannelType">测试PLC通道类型</param>
        /// <param name="moduleCount">模块数量</param>
        /// <param name="channelsPerModule">每个模块的通道数</param>
        //private void AllocateChannels(List<ChannelMapping> channels, int totalTestChannels, string testChannelType, int moduleCount, int channelsPerModule)
        //{
        //    if (channels == null || channels.Count == 0)
        //        return;

        //    // 计算需要分配的批次数
        //    int batchCount = (int)Math.Ceiling((double)channels.Count / totalTestChannels);
            
        //    // 为每个通道分配批次和测试PLC通道
        //    for (int i = 0; i < channels.Count; i++)
        //    {
        //        // 计算批次号（从1开始）
        //        int batchNumber = i / totalTestChannels + 1;
                
        //        // 计算在当前批次中的索引位置
        //        int indexInBatch = i % totalTestChannels;
                
        //        // 计算模块号（从1开始）
        //        int moduleNumber = indexInBatch / channelsPerModule + 1;
                
        //        // 计算在当前模块中的通道号（从1开始）
        //        int channelNumberInModule = indexInBatch % channelsPerModule + 1;
                
        //        // 更新通道信息
        //        channels[i].TestBatch = $"批次{batchNumber}";
        //        channels[i].TestPLCChannelTag = $"{testChannelType}{moduleNumber}_{channelNumberInModule}";
        //        //测试PLC的通道对应的通讯地址需要从仓储层中获取
        //        //_repository.
        //        channels[i].TestPLCCommunicationAddress = $"{testChannelType}{moduleNumber}.{channelNumberInModule}";
        //    }
        //}

        /// <summary>
        /// 使用新的测试PLC配置进行通道分配(测试)
        /// </summary>
        /// <param name="aiChannels">被测PLC的AI通道列表</param>
        /// <param name="aoChannels">被测PLC的AO通道列表</param>
        /// <param name="diChannels">被测PLC的DI通道列表</param>
        /// <param name="doChannels">被测PLC的DO通道列表</param>
        /// <param name="testResults">测试结果集合，用于同步更新</param>
        /// <returns>分配通道后的通道映射信息</returns>
        //public async Task<(IEnumerable<ChannelMapping> AI, IEnumerable<ChannelMapping> AO, IEnumerable<ChannelMapping> DI, IEnumerable<ChannelMapping> DO)>
        //    AllocateChannelsTestAsync(
        //        IEnumerable<ChannelMapping> aiChannels,
        //        IEnumerable<ChannelMapping> aoChannels,
        //        IEnumerable<ChannelMapping> diChannels,
        //        IEnumerable<ChannelMapping> doChannels,
        //        IEnumerable<ChannelMapping> testResults = null)
        //{
        //    // 设置当前使用的配置
        //    await SetTestPlcConfigAsync(_testPlcConfig);

        //    // 获取各类型测试通道数量
        //    var channelCounts = GetChannelCountsFromConfig();

        //    // 转换为列表以便修改
        //    var aiList = aiChannels.ToList();
        //    var aoList = aoChannels.ToList();
        //    var diList = diChannels.ToList();
        //    var doList = doChannels.ToList();

        //    // 使用配置中的通道信息进行分配
        //    await Task.Run(() =>
        //    {
        //        // 获取通道映射
        //        var aoMappings = _testPlcConfig.CommentsTables
        //            .Where(t => t.ChannelType == TestPlcChannelType.AO)
        //            .ToList();
        //        var aiMappings = _testPlcConfig.CommentsTables
        //            .Where(t => t.ChannelType == TestPlcChannelType.AI)
        //            .ToList();
        //        var doMappings = _testPlcConfig.CommentsTables
        //            .Where(t => t.ChannelType == TestPlcChannelType.DO)
        //            .ToList();
        //        var diMappings = _testPlcConfig.CommentsTables
        //            .Where(t => t.ChannelType == TestPlcChannelType.DI)
        //            .ToList();

        //        // 1. 为AI通道分配批次和测试PLC的AO通道(AI-AO)
        //        AllocateChannelsWithConfig(aiList, aoMappings, channelCounts.totalAoChannels);

        //        // 2. 为AO通道分配批次和测试PLC的AI通道(AO-AI)
        //        AllocateChannelsWithConfig(aoList, aiMappings, channelCounts.totalAiChannels);

        //        // 3. 为DI通道分配批次和测试PLC的DO通道(DI-DO)
        //        AllocateChannelsWithConfig(diList, doMappings, channelCounts.totalDoChannels);

        //        // 4. 为DO通道分配批次和测试PLC的DI通道(DO-DI)
        //        AllocateChannelsWithConfig(doList, diMappings, channelCounts.totalDiChannels);
        //    });

        //    return (aiList, aoList, diList, doList);
        //}

        /// <summary>
        /// 同步更新测试结果中通道分配的信息
        /// </summary>
        /// <param name="aiChannels">AI通道列表</param>
        /// <param name="aoChannels">AO通道列表</param>
        /// <param name="diChannels">DI通道列表</param>
        /// <param name="doChannels">DO通道列表</param>
        /// <param name="testResults">测试结果集合，用于同步更新</param>
        public void SyncChannelAllocation(
            IEnumerable<ChannelMapping> aiChannels,
            IEnumerable<ChannelMapping> aoChannels,
            IEnumerable<ChannelMapping> diChannels,
            IEnumerable<ChannelMapping> doChannels,
            IEnumerable<ChannelMapping> testResults = null)
        {
            try
            {
                // 参数有效性检查
                aiChannels = aiChannels ?? Enumerable.Empty<ChannelMapping>();
                aoChannels = aoChannels ?? Enumerable.Empty<ChannelMapping>();
                diChannels = diChannels ?? Enumerable.Empty<ChannelMapping>();
                doChannels = doChannels ?? Enumerable.Empty<ChannelMapping>();
                
                // 合并所有通道
                var allChannels = aiChannels.Concat(aoChannels).Concat(diChannels).Concat(doChannels)
                    .Where(c => c != null)
                    .ToList();
                    
                if (allChannels.Count == 0)
                {
                    return;
                }
                
                // 如果测试结果集合不为空，则进行同步
                if (testResults != null && testResults.Any())
                {
                    // 使用字典提高查找效率
                    var channelDict = allChannels.ToDictionary(
                        c => GetChannelKey(c.VariableName, c.ModuleType, c.ChannelTag), 
                        c => c);
                        
                    foreach (var result in testResults.Where(r => r != null))
                    {
                        string key = GetChannelKey(result.VariableName, result.ModuleType, result.ChannelTag);
                        if (channelDict.TryGetValue(key, out var channel))
                        {
                            // 同步通道信息
                            result.TestBatch = channel.TestBatch;
                            result.TestPLCChannelTag = channel.TestPLCChannelTag;
                            result.TestPLCCommunicationAddress = channel.TestPLCCommunicationAddress;
                            
                            System.Diagnostics.Debug.WriteLine($"同步通道 {channel.VariableName} 的测试PLC通道标签: {channel.TestPLCChannelTag}");
                        }
                    }
                }
                
                // 遍历所有通道，确保TestBatch和TestPLCChannelTag已正确设置
                foreach (var channel in allChannels)
                {
                    // 确保通道映射对象的属性已正确设置
                    if (!string.IsNullOrEmpty(channel.TestPLCChannelTag))
                    {
                        System.Diagnostics.Debug.WriteLine($"检查通道 {channel.VariableName} 的测试PLC通道标签: {channel.TestPLCChannelTag}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"同步通道分配信息失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 获取通道的唯一键
        /// </summary>
        /// <param name="variableName">变量名</param>
        /// <param name="moduleType">模块类型</param>
        /// <param name="channelTag">通道标签</param>
        /// <returns>唯一键</returns>
        private string GetChannelKey(string variableName, string moduleType, string channelTag)
        {
            return $"{variableName}_{moduleType}_{channelTag}";
        }
        
        /// <summary>
        /// 从通道映射信息中提取批次信息
        /// </summary>
        /// <param name="allChannels">所有通道集合</param>
        /// <returns>提取的批次信息集合</returns>
        public async Task<IEnumerable<ViewModels.BatchInfo>> ExtractBatchInfoAsync(
            IEnumerable<ChannelMapping> allChannels)
        {
            if (allChannels == null)
            {
                return Enumerable.Empty<ViewModels.BatchInfo>();
            }

            return await Task.Run(() =>
            {
                try
                {
                    // 从通道列表中提取批次信息
                    var batchGroups = allChannels
                        .Where(c => !string.IsNullOrEmpty(c.TestBatch))
                        .GroupBy(c => c.TestBatch)
                        .Select(g => new
                        {
                            BatchName = g.Key,
                            Channels = g.ToList()
                        })
                        .ToList();
                    
                    // 创建批次信息对象列表
                    var batchInfoList = new List<ViewModels.BatchInfo>();
                    
                    foreach (var batch in batchGroups)
                    {
                        // 获取该批次中的通道列表
                        var batchChannels = batch.Channels;
                        
                        // 统计测试状态
                        int notTestedCount = batchChannels.Count(c => c.TestResultStatus == 0);
                        int testedCount = batchChannels.Count(c => c.TestResultStatus > 0);
                        int successCount = batchChannels.Count(c => c.TestResultStatus == 1);
                        int failureCount = batchChannels.Count(c => c.TestResultStatus == 2);
                        
                        // 确定批次的测试状态
                        string status = "未开始";
                        if (testedCount > 0)
                        {
                            if (notTestedCount == 0)
                            {
                                status = failureCount > 0 ? "完成(有失败)" : "全部通过";
                            }
                            else
                            {
                                status = "测试中";
                            }
                        }
                        
                        // 获取测试时间信息
                        var testedChannels = batchChannels.Where(c => c.TestTime.HasValue).ToList();
                        var firstTestTime = testedChannels.Any() ? testedChannels.Min(c => c.TestTime) : null;
                        var lastTestTime = testedChannels.Any() ? testedChannels.Max(c => c.TestTime) : null;
                        
                        // 创建批次信息对象
                        var batchInfo = new ViewModels.BatchInfo(batch.BatchName, batchChannels.Count)
                        {
                            Status = status,
                            FirstTestTime = firstTestTime,
                            LastTestTime = lastTestTime
                        };
                        
                        batchInfoList.Add(batchInfo);
                    }
                    
                    return batchInfoList;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"提取批次信息失败: {ex.Message}");
                    return Enumerable.Empty<ViewModels.BatchInfo>();
                }
            });
        }

        /// <summary>
        /// 使用配置中的通道信息分配通道(实际使用的通道分配的方法)use
        /// </summary>
        /// <param name="channels">待分配的通道</param>
        /// <param name="testChannelMappings">测试PLC的通道映射</param>
        /// <param name="totalTestChannels">测试PLC通道总数</param>
        private void AllocateChannelsWithConfig(
            List<ChannelMapping> channels, 
            List<ComparisonTable> testChannelMappings, 
            int totalTestChannels)
        {
            if (channels == null || channels.Count == 0 || testChannelMappings == null || testChannelMappings.Count == 0)
            {
                return;
            }

            try
            {
                // 计算需要分配的批次数
                int batchCount = (int)Math.Ceiling((double)channels.Count / totalTestChannels);
                
                // 为每个通道分配批次和测试PLC通道
                for (int i = 0; i < channels.Count; i++)
                {
                    var channel = channels[i];
                    if (channel == null) continue;

                    // 计算批次号（从1开始）
                    int batchNumber = i / totalTestChannels + 1;
                    
                    // 计算在当前批次中的索引位置
                    int indexInBatch = i % totalTestChannels;
                    
                    // 如果索引超出了测试通道的范围，则跳过
                    if (indexInBatch >= testChannelMappings.Count)
                    {
                        // 记录错误信息
                        System.Diagnostics.Debug.WriteLine($"警告：通道 {channel.VariableName} 无法分配测试PLC通道，因为超出了可用通道范围");
                        continue;
                    }
                    
                    // 获取对应的测试通道映射
                    var testChannelMapping = testChannelMappings[indexInBatch];
                    if (testChannelMapping == null) continue;
                    
                    // 更新通道信息
                    channel.TestBatch = $"批次{batchNumber}";
                    channel.TestPLCChannelTag = testChannelMapping.ChannelAddress;
                    channel.TestPLCCommunicationAddress = testChannelMapping.CommunicationAddress;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"分配通道失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 使用测试PLC配置进行通道分配
        /// </summary>
        /// <param name="aiChannels">AI通道列表</param>
        /// <param name="aoChannels">AO通道列表</param>
        /// <param name="diChannels">DI通道列表</param>
        /// <param name="doChannels">DO通道列表</param>
        /// <param name="testPlcConfig">测试PLC配置</param>
        /// <returns>分配后的通道映射信息</returns>
        public async Task<(IEnumerable<ChannelMapping> AI, IEnumerable<ChannelMapping> AO, IEnumerable<ChannelMapping> DI, IEnumerable<ChannelMapping> DO)> 
            AllocateChannelsAsync(
                IEnumerable<ChannelMapping> aiChannels,
                IEnumerable<ChannelMapping> aoChannels,
                IEnumerable<ChannelMapping> diChannels,
                IEnumerable<ChannelMapping> doChannels, 
                TestPlcConfig testPlcConfig)
        {
            //读取相关点位配置
            _testPlcConfig.CommentsTables = await _repository.GetComparisonTablesAsync();
            // 设置当前使用的配置
            await SetTestPlcConfigAsync(testPlcConfig);

            // 获取各类型测试通道数量
            var channelCounts = GetChannelCountsFromConfig();

            // 转换为列表以便修改
            var aiList = aiChannels.ToList();
            var aoList = aoChannels.ToList();
            var diList = diChannels.ToList();
            var doList = doChannels.ToList();

            // 使用配置中的通道信息进行分配
            await Task.Run(() =>
            {
                // 获取通道映射
                var aoMappings = _testPlcConfig.CommentsTables
                    .Where(t => t.ChannelType == TestPlcChannelType.AO)
                    .ToList();
                var aiMappings = _testPlcConfig.CommentsTables
                    .Where(t => t.ChannelType == TestPlcChannelType.AI)
                    .ToList();
                var doMappings = _testPlcConfig.CommentsTables
                    .Where(t => t.ChannelType == TestPlcChannelType.DO)
                    .ToList();
                var diMappings = _testPlcConfig.CommentsTables
                    .Where(t => t.ChannelType == TestPlcChannelType.DI)
                    .ToList();

                // 1. 为AI通道分配批次和测试PLC的AO通道(AI-AO)
                AllocateChannelsWithConfig(aiList, aoMappings, channelCounts.totalAoChannels);

                // 2. 为AO通道分配批次和测试PLC的AI通道(AO-AI)
                AllocateChannelsWithConfig(aoList, aiMappings, channelCounts.totalAiChannels);

                // 3. 为DI通道分配批次和测试PLC的DO通道(DI-DO)
                AllocateChannelsWithConfig(diList, doMappings, channelCounts.totalDoChannels);

                // 4. 为DO通道分配批次和测试PLC的DI通道(DO-DI)
                AllocateChannelsWithConfig(doList, diMappings, channelCounts.totalDiChannels);
            });

            return (aiList, aoList, diList, doList);
        }

        /// <summary>
        /// 测试使用的分配方法，后续替换为AllocateChannelsAsync(目前使用的实际的通道分配的方法)use
        /// </summary>
        /// <param name="allChannels">所有通道集合</param>
        /// <returns>分配通道后的通道映射信息</returns>
        public async Task<IEnumerable<ChannelMapping>> AllocateChannelsTestAsync(
            IEnumerable<ChannelMapping> allChannels)
        {
            if (allChannels == null)
            {
                return Enumerable.Empty<ChannelMapping>();
            }

            try
            {
                // 读取相关点位配置
                _testPlcConfig.CommentsTables = await _repository.GetComparisonTablesAsync();
                
                // 获取各类型通道集合
                var aiChannels = GetAIChannels(allChannels).ToList();
                var aoChannels = GetAOChannels(allChannels).ToList();
                var diChannels = GetDIChannels(allChannels).ToList();
                var doChannels = GetDOChannels(allChannels).ToList();
                var aiNoneChannels = GetAINoneChannels(allChannels).ToList();
                var aoNoneChannels = GetAONoneChannels(allChannels).ToList();
                var diNoneChannels = GetDINoneChannels(allChannels).ToList();
                var doNoneChannels = GetDONoneChannels(allChannels).ToList();

                // 设置当前使用的配置
                await SetTestPlcConfigAsync(_testPlcConfig);

                // 获取各类型测试通道数量
                var channelCounts = GetChannelCountsFromConfig();
                
                // 使用配置中的通道信息进行分配
                await Task.Run(() =>
                {
                    // 获取通道映射
                    var aoMappings = _testPlcConfig.CommentsTables
                        ?.Where(t => t.ChannelType == TestPlcChannelType.AO)
                        ?.ToList() ?? new List<ComparisonTable>();
                        
                    var aiMappings = _testPlcConfig.CommentsTables
                        ?.Where(t => t.ChannelType == TestPlcChannelType.AI)
                        ?.ToList() ?? new List<ComparisonTable>();
                        
                    var doMappings = _testPlcConfig.CommentsTables
                        ?.Where(t => t.ChannelType == TestPlcChannelType.DO)
                        ?.ToList() ?? new List<ComparisonTable>();
                        
                    var diMappings = _testPlcConfig.CommentsTables
                        ?.Where(t => t.ChannelType == TestPlcChannelType.DI)
                        ?.ToList() ?? new List<ComparisonTable>();
                    var aoNoneMappings = _testPlcConfig.CommentsTables
                        ?.Where(t => t.ChannelType == TestPlcChannelType.AONone)
                        ?.ToList() ?? new List<ComparisonTable>();

                    var aiNoneMappings = _testPlcConfig.CommentsTables
                        ?.Where(t => t.ChannelType == TestPlcChannelType.AINone)
                        ?.ToList() ?? new List<ComparisonTable>();

                    var doNoneMappings = _testPlcConfig.CommentsTables
                        ?.Where(t => t.ChannelType == TestPlcChannelType.DONone)
                        ?.ToList() ?? new List<ComparisonTable>();

                    var diNoneMappings = _testPlcConfig.CommentsTables
                        ?.Where(t => t.ChannelType == TestPlcChannelType.DINone)
                        ?.ToList() ?? new List<ComparisonTable>();

                    // 1. 为AI通道分配批次和测试PLC的AO通道(AI-AO)
                    AllocateChannelsWithConfig(aiChannels, aoNoneMappings, aoNoneMappings.Count);

                    // 2. 为AO通道分配批次和测试PLC的AI通道(AO-AI)
                    AllocateChannelsWithConfig(aoChannels, aiNoneMappings, aiNoneMappings.Count);

                    // 3. 为DI通道分配批次和测试PLC的DO通道(DI-DO)
                    AllocateChannelsWithConfig(diChannels, doNoneMappings, doNoneMappings.Count);

                    // 4. 为DO通道分配批次和测试PLC的DI通道(DO-DI)
                    AllocateChannelsWithConfig(doChannels, diNoneMappings, diNoneMappings.Count);

                    // 1. 为AI通道分配批次和测试PLC的AO通道(AI-AO)
                    AllocateChannelsWithConfig(aiNoneChannels, aoMappings, aoMappings.Count);

                    // 2. 为AO通道分配批次和测试PLC的AI通道(AO-AI)
                    AllocateChannelsWithConfig(aoNoneChannels, aiMappings, aiMappings.Count);

                    // 3. 为DI通道分配批次和测试PLC的DO通道(DI-DO)
                    AllocateChannelsWithConfig(diNoneChannels, doMappings, doMappings.Count);

                    // 4. 为DO通道分配批次和测试PLC的DI通道(DO-DI)
                    AllocateChannelsWithConfig(doNoneChannels, diMappings, diMappings.Count);
                });
                    
                // 返回合并后的结果
                return aiChannels.Concat(aoChannels).Concat(diChannels).Concat(doChannels);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"通道分配失败: {ex.Message}");
                return allChannels; // 出错时返回原始通道列表
            }
        }

        /// <summary>
        /// 同步更新测试结果中通道分配的信息
        /// </summary>
        /// <param name="allChannels">所有通道集合</param>
        /// <param name="testResults">测试结果集合，用于同步更新</param>
        public void SyncChannelAllocation(
            IEnumerable<ChannelMapping> allChannels,
            IEnumerable<ChannelMapping> testResults = null)
        {
            // 获取各类型通道集合
            var aiChannels = GetAIChannels(allChannels).ToList();
            var aoChannels = GetAOChannels(allChannels).ToList();
            var diChannels = GetDIChannels(allChannels).ToList();
            var doChannels = GetDOChannels(allChannels).ToList();
            
            // 调用现有方法
            SyncChannelAllocation(aiChannels, aoChannels, diChannels, doChannels, testResults);
        }

        /// <summary>
        /// 同步更新测试结果中通道分配的信息
        /// </summary>
        /// <param name="allChannels">所有通道集合</param>
        public void SyncChannelAllocation(
            IEnumerable<ChannelMapping> allChannels)
        {
            SyncChannelAllocation(allChannels, null);
        }

        /// <summary>
        /// 获取特定类型的通道列表
        /// </summary>
        /// <param name="moduleType">通道类型</param>
        /// <param name="allChannels">所有通道集合</param>
        /// <returns>过滤后的通道列表</returns>
        public IEnumerable<ChannelMapping> GetChannelsByType(string moduleType,string powerType, IEnumerable<ChannelMapping> allChannels)
        {
            if (string.IsNullOrEmpty(moduleType) || allChannels == null)
                return Enumerable.Empty<ChannelMapping>();

            // 转换为小写后比较，提高匹配准确性    
            var lowerModuleType = moduleType.ToLowerInvariant();
            if (lowerModuleType.Contains("none"))
            {
                lowerModuleType = lowerModuleType.Replace("none", "");
            }
            // 使用ToList()提前执行查询，避免重复计算
            return allChannels.Where(c => (c.ModuleType?.ToLowerInvariant() == lowerModuleType) && (c.PowerSupplyType.Contains(powerType))).ToList();
        }

        /// <summary>
        /// 获取AI类型的通道列表
        /// </summary>
        /// <param name="allChannels">所有通道集合</param>
        /// <returns>AI类型的通道列表</returns>
        public IEnumerable<ChannelMapping> GetAIChannels(IEnumerable<ChannelMapping> allChannels)
        {
            return GetChannelsByType(TestPlcChannelType.AI.ToString(),"有源", allChannels);
        }

        /// <summary>
        /// 获取AO类型的通道列表
        /// </summary>
        /// <param name="allChannels">所有通道集合</param>
        /// <returns>AO类型的通道列表</returns>
        public IEnumerable<ChannelMapping> GetAOChannels(IEnumerable<ChannelMapping> allChannels)
        {
            return GetChannelsByType(TestPlcChannelType.AO.ToString(),"有源", allChannels);
        }

        /// <summary>
        /// 获取DI类型的通道列表
        /// </summary>
        /// <param name="allChannels">所有通道集合</param>
        /// <returns>DI类型的通道列表</returns>
        public IEnumerable<ChannelMapping> GetDIChannels(IEnumerable<ChannelMapping> allChannels)
        {
            return GetChannelsByType(TestPlcChannelType.DI.ToString(), "有源", allChannels);
        }

        /// <summary>
        /// 获取DO类型的通道列表
        /// </summary>
        /// <param name="allChannels">所有通道集合</param>
        /// <returns>DO类型的通道列表</returns>
        public IEnumerable<ChannelMapping> GetDOChannels(IEnumerable<ChannelMapping> allChannels)
        {
            return GetChannelsByType(TestPlcChannelType.DO.ToString(), "有源", allChannels);
        }

        /// <summary>
        /// 获取AINone类型的通道列表
        /// </summary>
        /// <param name="allChannels">所有通道集合</param>
        /// <returns>AI类型的通道列表</returns>
        public IEnumerable<ChannelMapping> GetAINoneChannels(IEnumerable<ChannelMapping> allChannels)
        {
            return GetChannelsByType(TestPlcChannelType.AINone.ToString(), "无源", allChannels);
        }

        /// <summary>
        /// 获取AONone类型的通道列表
        /// </summary>
        /// <param name="allChannels">所有通道集合</param>
        /// <returns>AO类型的通道列表</returns>
        public IEnumerable<ChannelMapping> GetAONoneChannels(IEnumerable<ChannelMapping> allChannels)
        {
            return GetChannelsByType(TestPlcChannelType.AONone.ToString(),"无源", allChannels);
        }

        /// <summary>
        /// 获取DINone类型的通道列表
        /// </summary>
        /// <param name="allChannels">所有通道集合</param>
        /// <returns>DI类型的通道列表</returns>
        public IEnumerable<ChannelMapping> GetDINoneChannels(IEnumerable<ChannelMapping> allChannels)
        {
            return GetChannelsByType(TestPlcChannelType.DINone.ToString(), "无源", allChannels);
        }

        /// <summary>
        /// 获取DONone类型的通道列表
        /// </summary>
        /// <param name="allChannels">所有通道集合</param>
        /// <returns>DO类型的通道列表</returns>
        public IEnumerable<ChannelMapping> GetDONoneChannels(IEnumerable<ChannelMapping> allChannels)
        {
            return GetChannelsByType(TestPlcChannelType.DONone.ToString(), "无源", allChannels);
        }

        /// <summary>
        /// 单个通道的对应关系修改：当默认通道分配好后用户有可能需要调整对应关系
        /// </summary>
        /// <param name="targetChannel">要修改的被测PLC通道</param>
        /// <param name="newTestPlcChannel">新分配的测试PLC通道标识</param>
        /// <param name="newTestPlcCommAddress">新分配的测试PLC通讯地址</param>
        /// <param name="allChannels">所有通道的集合，用于查找和更新原有的映射关系</param>
        /// <returns>修改后的目标通道信息</returns>
        //public async Task<ChannelMapping> UpdateChannelMappingAsync(
        //    ChannelMapping targetChannel, 
        //    string newTestPlcChannel, 
        //    string newTestPlcCommAddress,
        //    IEnumerable<ChannelMapping> allChannels)
        //{
        //    return await Task.Run(() =>
        //    {
        //        if (targetChannel == null)
        //            throw new ArgumentNullException(nameof(targetChannel));

        //        // 查找使用了这个新测试PLC通道的现有映射
        //        var existingMapping = allChannels?.FirstOrDefault(c => 
        //            c.TestPLCChannelTag == newTestPlcChannel && 
        //            c.TestPLCCommunicationAddress == newTestPlcCommAddress && 
        //            c != targetChannel);

        //        // 如果找到，清除该映射的测试PLC信息
        //        if (existingMapping != null)
        //        {
        //            existingMapping.TestPLCChannelTag = string.Empty;
        //            existingMapping.TestPLCCommunicationAddress = string.Empty;
        //            // 注意：不清除批次信息，因为可能同一批次中的其他通道仍然在使用
        //        }

        //        // 更新目标通道的测试PLC信息
        //        targetChannel.TestPLCChannelTag = newTestPlcChannel;
        //        targetChannel.TestPLCCommunicationAddress = newTestPlcCommAddress;

        //        return targetChannel;
        //    });
        //}

        /// <summary>
        /// 设置当前使用的测试PLC配置
        /// </summary>
        /// <param name="config">测试PLC配置</param>
        /// <returns>操作是否成功</returns>
        public async Task<bool> SetTestPlcConfigAsync(TestPlcConfig config)
        {
            return await Task.Run(() =>
            {
                if (config == null)
                    return false;

                _testPlcConfig = config;
                return true;
            });
        }

        /// <summary>
        /// 获取当前使用的测试PLC配置
        /// </summary>
        /// <returns>测试PLC配置</returns>
        //public async Task<TestPlcConfig> GetTestPlcConfigAsync()
        //{
        //    return await Task.Run(() => _testPlcConfig);
        //}

        /// <summary>
        /// 获取测试PLC通道的配置信息
        /// </summary>
        /// <returns>测试PLC的通道配置信息</returns>
        //public async Task<(int AoModules, int AoChannelsPerModule, int AiModules, int AiChannelsPerModule, 
        //        int DoModules, int DoChannelsPerModule, int DiModules, int DiChannelsPerModule)> 
        //    GetTestPlcConfigurationAsync()
        //{
        //    // 优先使用当前配置中的通道数据
        //    return await Task.Run(() =>
        //    {
        //        if (_testPlcConfig != null && _testPlcConfig.CommentsTables != null && _testPlcConfig.CommentsTables.Any())
        //        {
        //            // 计算各类型通道的数量和模块数
        //            var channelCounts = GetChannelCountsFromConfig();
        //            return (
        //                GetModuleCount(channelCounts.aoChannels), GetChannelsPerModule(channelCounts.aoChannels),
        //                GetModuleCount(channelCounts.aiChannels), GetChannelsPerModule(channelCounts.aiChannels),
        //                GetModuleCount(channelCounts.doChannels), GetChannelsPerModule(channelCounts.doChannels),
        //                GetModuleCount(channelCounts.diChannels), GetChannelsPerModule(channelCounts.diChannels)
        //            );
        //        }
                
        //        // 如果没有配置或配置为空，使用默认配置
        //        return _defaultTestPlcConfig;
        //    });
        //}

        /// <summary>
        /// 从配置中获取通道数量统计use
        /// </summary>
        /// <returns>各类型通道的数量信息</returns>
        private (
            IEnumerable<ComparisonTable> aoChannels, 
            IEnumerable<ComparisonTable> aiChannels, 
            IEnumerable<ComparisonTable> doChannels, 
            IEnumerable<ComparisonTable> diChannels,
            int totalAoChannels,
            int totalAiChannels,
            int totalDoChannels,
            int totalDiChannels
        ) GetChannelCountsFromConfig()
        {
            if (_testPlcConfig == null || _testPlcConfig.CommentsTables == null)
            {
                return (
                    new List<ComparisonTable>(),
                    new List<ComparisonTable>(),
                    new List<ComparisonTable>(),
                    new List<ComparisonTable>(),
                    0, 0, 0, 0
                );
            }

            var aoChannels = _testPlcConfig.CommentsTables
                .Where(t => t.ChannelType == TestPlcChannelType.AO)
                .ToList();
            var aiChannels = _testPlcConfig.CommentsTables
                .Where(t => t.ChannelType == TestPlcChannelType.AI)
                .ToList();
            var doChannels = _testPlcConfig.CommentsTables
                .Where(t => t.ChannelType == TestPlcChannelType.DO)
                .ToList();
            var diChannels = _testPlcConfig.CommentsTables
                .Where(t => t.ChannelType == TestPlcChannelType.DI)
                .ToList();

            return (
                aoChannels,
                aiChannels,
                doChannels,
                diChannels,
                aoChannels.Count,
                aiChannels.Count,
                doChannels.Count,
                diChannels.Count
            );
        }

        /// <summary>
        /// 计算通道所属的模块数量
        /// </summary>
        /// <param name="channels">通道列表</param>
        /// <returns>模块数量</returns>
        //private int GetModuleCount(IEnumerable<ComparisonTable> channels)
        //{
        //    if (channels == null || !channels.Any())
        //        return 0;

        //    // 从通道地址中提取模块编号
        //    var moduleNumbers = channels
        //        .Select(c => 
        //        {
        //            // 提取模块编号，格式为"XX1_2"，其中1为模块编号
        //            var parts = c.ChannelAddress.Split('_');
        //            if (parts.Length > 0)
        //            {
        //                var typeAndModule = parts[0]; // 例如"AO1"
        //                var moduleNumberStr = string.Empty;
                        
        //                // 去除类型前缀，保留数字部分
        //                for (int i = 0; i < typeAndModule.Length; i++)
        //                {
        //                    if (char.IsDigit(typeAndModule[i]))
        //                    {
        //                        moduleNumberStr = typeAndModule.Substring(i);
        //                        break;
        //                    }
        //                }

        //                if (!string.IsNullOrEmpty(moduleNumberStr) && int.TryParse(moduleNumberStr, out int moduleNumber))
        //                {
        //                    return moduleNumber;
        //                }
        //            }
        //            return 0;
        //        })
        //        .Where(m => m > 0)
        //        .Distinct()
        //        .ToList();

        //    return moduleNumbers.Count;
        //}

        ///// <summary>
        ///// 计算每个模块的通道数量
        ///// </summary>
        ///// <param name="channels">通道列表</param>
        ///// <returns>每个模块的平均通道数</returns>
        //private int GetChannelsPerModule(IEnumerable<ComparisonTable> channels)
        //{
        //    if (channels == null || !channels.Any())
        //        return 0;

        //    // 提取模块编号和通道在模块中的序号
        //    var moduleChannels = new Dictionary<int, List<int>>();
        //    foreach (var channel in channels)
        //    {
        //        // 提取模块编号和通道编号，格式为"XX1_2"，其中1为模块编号，2为通道编号
        //        var parts = channel.ChannelAddress.Split('_');
        //        if (parts.Length >= 2)
        //        {
        //            var typeAndModule = parts[0]; // 例如"AO1"
        //            var moduleNumberStr = string.Empty;
                    
        //            // 去除类型前缀，保留数字部分
        //            for (int i = 0; i < typeAndModule.Length; i++)
        //            {
        //                if (char.IsDigit(typeAndModule[i]))
        //                {
        //                    moduleNumberStr = typeAndModule.Substring(i);
        //                    break;
        //                }
        //            }

        //            if (!string.IsNullOrEmpty(moduleNumberStr) && int.TryParse(moduleNumberStr, out int moduleNumber) &&
        //                int.TryParse(parts[1], out int channelNumber))
        //            {
        //                if (!moduleChannels.ContainsKey(moduleNumber))
        //                {
        //                    moduleChannels[moduleNumber] = new List<int>();
        //                }
        //                moduleChannels[moduleNumber].Add(channelNumber);
        //            }
        //        }
        //    }

        //    // 如果没有有效的模块通道数据，则返回0
        //    if (moduleChannels.Count == 0)
        //        return 0;

        //    // 计算每个模块的通道数量平均值
        //    return (int)Math.Ceiling(moduleChannels.Values.Average(list => list.Count));
        //}

        ///// <summary>
        ///// 获取已被分配的测试PLC通道列表
        ///// </summary>
        ///// <param name="allChannels">所有通道的集合</param>
        ///// <returns>已分配的测试PLC通道信息</returns>
        //public async Task<IEnumerable<(string ChannelType, string ChannelTag, string CommAddress)>> GetAllocatedTestChannelsAsync(
        //    IEnumerable<ChannelMapping> allChannels)
        //{
        //    return await Task.Run(() =>
        //    {
        //        var allocatedChannels = allChannels
        //            .Where(c => !string.IsNullOrEmpty(c.TestPLCChannelTag) && !string.IsNullOrEmpty(c.TestPLCCommunicationAddress))
        //            .Select(c => (
        //                // 从TestPLCChannelTag提取通道类型（如"AO"、"AI"等）
        //                ChannelType: c.TestPLCChannelTag.Substring(0, 2),
        //                ChannelTag: c.TestPLCChannelTag,
        //                CommAddress: c.TestPLCCommunicationAddress
        //            ))
        //            .ToList();

        //        return allocatedChannels;
        //    });
        //}

        /// <summary>
        /// 清除所有通道分配信息
        /// </summary>
        /// <param name="channels">需要清除分配信息的通道集合</param>
        /// <returns>清除分配信息后的通道集合</returns>
        public async Task<IEnumerable<ChannelMapping>> ClearAllChannelAllocationsAsync(IEnumerable<ChannelMapping> channels)
        {
            if (channels == null)
            {
                return Enumerable.Empty<ChannelMapping>();
            }

            return await Task.Run(() =>
            {
                try 
                {
                    var updatedChannels = channels.ToList();
                    
                    foreach (var channel in updatedChannels)
                    {
                        if (channel != null)
                        {
                            channel.TestPLCChannelTag = string.Empty;
                            channel.TestPLCCommunicationAddress = string.Empty;
                            channel.TestBatch = string.Empty;
                            // 同时重置测试状态相关字段
                            channel.TestResultStatus = 0;
                            channel.ResultText = "未测试";
                            channel.TestTime = null;
                        }
                    }
                    
                    return updatedChannels;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"清除通道分配信息失败: {ex.Message}");
                    return channels; // 出错时返回原始通道列表
                }
            });
        }

        /// <summary>
        /// 默认通道分批功能：根据测试PLC的配置信息获取可用测试通道数，对应被测PLC进行通道自动分配
        /// </summary>
        /// <param name="allChannels">所有通道集合</param>
        /// <param name="testPlcConfig">测试PLC配置信息</param>
        /// <returns>分配通道后的通道映射信息</returns>
        //public async Task<IEnumerable<ChannelMapping>> AllocateChannelsAsync(
        //    IEnumerable<ChannelMapping> allChannels,
        //    TestPlcConfig testPlcConfig)
        //{
        //    // 获取各类型通道集合
        //    var aiChannels = GetAIChannels(allChannels).ToList();
        //    var aoChannels = GetAOChannels(allChannels).ToList();
        //    var diChannels = GetDIChannels(allChannels).ToList();
        //    var doChannels = GetDOChannels(allChannels).ToList();

        //    // 使用现有方法进行分配
        //    var result = await AllocateChannelsAsync(
        //        aiChannels, aoChannels, diChannels, doChannels, testPlcConfig);
            
        //    // 合并结果并返回
        //    return result.AI.Concat(result.AO).Concat(result.DI).Concat(result.DO);
        //}

        /// <summary>
        /// 更新批次状态信息，根据测试结果更新批次的状态
        /// </summary>
        /// <param name="batches">批次信息集合</param>
        /// <param name="testResults">测试结果集合</param>
        /// <returns>更新后的批次信息集合</returns>
        public async Task<IEnumerable<ViewModels.BatchInfo>> UpdateBatchStatusAsync(
            IEnumerable<ViewModels.BatchInfo> batches,
            IEnumerable<ChannelMapping> testResults)
        {
            // 参数有效性检查
            if (batches == null || testResults == null)
            {
                return batches ?? Enumerable.Empty<ViewModels.BatchInfo>();
            }

            // 异步执行以避免阻塞UI线程
            return await Task.Run(() =>
            {
                try
                {
                    var batchList = batches.ToList();
                    
                    // 获取按批次分组的测试结果
                    var resultsByBatch = testResults
                        .Where(r => !string.IsNullOrEmpty(r.TestBatch))
                        .GroupBy(r => r.TestBatch)
                        .ToDictionary(g => g.Key, g => g.ToList());

                    foreach (var batch in batchList)
                    {
                        if (batch == null || string.IsNullOrEmpty(batch.BatchName))
                        {
                            continue;
                        }
                        
                        if (resultsByBatch.TryGetValue(batch.BatchName, out var batchResults))
                        {
                            // 计算该批次的测试状态
                            int totalPoints = batchResults.Count;
                            int testedPoints = batchResults.Count(r => r.TestResultStatus > 0);
                            int successPoints = batchResults.Count(r => r.TestResultStatus == 1);
                            int failurePoints = batchResults.Count(r => r.TestResultStatus == 2);

                            // 更新批次状态
                            UpdateBatchStatus(batch, totalPoints, testedPoints, failurePoints);

                            // 更新批次的测试时间信息
                            UpdateBatchTestTimes(batch, batchResults);

                            // 更新批次项目数量
                            batch.ItemCount = totalPoints;
                        }
                    }

                    return batchList;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"更新批次状态失败: {ex.Message}");
                    return batches;
                }
            });
        }

        /// <summary>
        /// 更新批次的测试状态
        /// </summary>
        /// <param name="batch">批次信息</param>
        /// <param name="totalPoints">总点数</param>
        /// <param name="testedPoints">已测试点数</param>
        /// <param name="failurePoints">失败点数</param>
        private void UpdateBatchStatus(ViewModels.BatchInfo batch, int totalPoints, int testedPoints, int failurePoints)
        {
            if (testedPoints == 0)
            {
                batch.Status = "未开始";
            }
            else if (testedPoints < totalPoints)
            {
                batch.Status = "测试中";
            }
            else if (failurePoints > 0)
            {
                batch.Status = "测试完成(有失败)";
            }
            else
            {
                batch.Status = "测试完成(全部通过)";
            }
        }

        /// <summary>
        /// 更新批次的测试时间信息
        /// </summary>
        /// <param name="batch">批次信息</param>
        /// <param name="batchResults">批次测试结果</param>
        private void UpdateBatchTestTimes(ViewModels.BatchInfo batch, List<ChannelMapping> batchResults)
        {
            var testedResultsWithTime = batchResults
                .Where(r => r.TestTime.HasValue)
                .ToList();

            if (testedResultsWithTime.Any())
            {
                batch.FirstTestTime = testedResultsWithTime
                    .Min(r => r.TestTime);

                batch.LastTestTime = testedResultsWithTime
                    .Max(r => r.TestTime);
            }
        }

        /// <summary>
        /// 根据批次名称获取相关的通道映射数据
        /// </summary>
        /// <param name="batchName">批次名称</param>
        /// <returns>属于该批次的通道映射集合</returns>
        public async Task<IEnumerable<ChannelMapping>> GetChannelMappingsByBatchNameAsync(string batchName)
        {
            if (string.IsNullOrEmpty(batchName))
                return Enumerable.Empty<ChannelMapping>();
            
            // 这里模拟从数据库或者其他存储中获取所有通道映射数据
            // 然后按照批次名称进行过滤
            // 在实际应用中，可以直接从数据库中按批次名称查询以提高效率
            
            // 使用AllocateChannelsTestAsync作为获取所有通道的方法
            var allChannels = await AllocateChannelsTestAsync(new List<ChannelMapping>());
            
            // 过滤出属于指定批次的通道映射
            return allChannels.Where(c => c.TestBatch == batchName).ToList();
        }

        /// <summary>
        /// 获取所有通道映射
        /// </summary>
        /// <returns>通道映射列表</returns>
        public async Task<IEnumerable<ChannelMapping>> GetAllChannelMappingsAsync()
        {
            // 这里应该从数据库或其他存储获取所有通道映射
            // 暂时返回空集合
            return await Task.FromResult(new List<ChannelMapping>());
        }

        /// <summary>
        /// 根据通道类型获取通道映射
        /// </summary>
        /// <param name="moduleType">通道类型</param>
        /// <returns>该类型的通道映射列表</returns>
        public async Task<IEnumerable<ChannelMapping>> GetChannelMappingsByModuleTypeAsync(string moduleType)
        {
            // 获取所有通道映射
            var allChannels = await GetAllChannelMappingsAsync();
            
            // 按类型筛选
            return allChannels.Where(c => c.ModuleType == moduleType).ToList();
        }

        /// <summary>
        /// 添加通道映射
        /// </summary>
        /// <param name="channelMapping">通道映射实体</param>
        /// <returns>操作结果</returns>
        public async Task<bool> AddChannelMappingAsync(ChannelMapping channelMapping)
        {
            if (channelMapping == null)
                return false;
                
            // 添加通道映射的实现
            // 实际项目中应该将数据保存到数据库
            return await Task.FromResult(true);
        }

        /// <summary>
        /// 批量添加通道映射
        /// </summary>
        /// <param name="channelMappings">通道映射列表</param>
        /// <returns>操作结果</returns>
        public async Task<bool> AddChannelMappingsAsync(IEnumerable<ChannelMapping> channelMappings)
        {
            if (channelMappings == null || !channelMappings.Any())
                return false;
                
            // 批量添加通道映射的实现
            // 实际项目中应该批量保存到数据库
            return await Task.FromResult(true);
        }

        /// <summary>
        /// 更新通道映射
        /// </summary>
        /// <param name="channelMapping">通道映射实体</param>
        /// <returns>操作结果</returns>
        public async Task<bool> UpdateChannelMappingAsync(ChannelMapping channelMapping)
        {
            if (channelMapping == null)
                return false;
                
            // 更新通道映射的实现
            // 实际项目中应该更新数据库中的记录
            return await Task.FromResult(true);
        }

        /// <summary>
        /// 批量更新通道映射
        /// </summary>
        /// <param name="channelMappings">通道映射列表</param>
        /// <returns>操作结果</returns>
        public async Task<bool> UpdateChannelMappingsAsync(IEnumerable<ChannelMapping> channelMappings)
        {
            if (channelMappings == null || !channelMappings.Any())
                return false;
                
            // 批量更新通道映射的实现
            // 实际项目中应该批量更新数据库中的记录
            return await Task.FromResult(true);
        }

        /// <summary>
        /// 删除通道映射
        /// </summary>
        /// <param name="id">通道映射ID</param>
        /// <returns>操作结果</returns>
        public async Task<bool> DeleteChannelMappingAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                return false;
                
            // 删除通道映射的实现
            // 实际项目中应该删除数据库中的记录
            return await Task.FromResult(true);
        }

        /// <summary>
        /// 批量删除通道映射
        /// </summary>
        /// <param name="ids">通道映射ID列表</param>
        /// <returns>操作结果</returns>
        public async Task<bool> DeleteChannelMappingsAsync(IEnumerable<string> ids)
        {
            if (ids == null || !ids.Any())
                return false;
                
            // 批量删除通道映射的实现
            // 实际项目中应该批量删除数据库中的记录
            return await Task.FromResult(true);
        }

        /// <summary>
        /// 设置当前使用的测试PLC配置
        /// </summary>
        /// <param name="config">配置信息</param>
        public void SetTestPlcConfig(TestPlcConfig config)
        {
            _testPlcConfig = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// 从通道映射信息中提取批次信息
        /// </summary>
        /// <param name="aiChannels">AI通道列表</param>
        /// <param name="aoChannels">AO通道列表</param>
        /// <param name="diChannels">DI通道列表</param>
        /// <param name="doChannels">DO通道列表</param>
        /// <returns>提取的批次信息集合</returns>
        public async Task<IEnumerable<ViewModels.BatchInfo>> ExtractBatchInfoAsync(
            IEnumerable<ChannelMapping> aiChannels,
            IEnumerable<ChannelMapping> aoChannels,
            IEnumerable<ChannelMapping> diChannels,
            IEnumerable<ChannelMapping> doChannels)
        {
            // 合并所有通道列表
            var allChannels = aiChannels.Concat(aoChannels).Concat(diChannels).Concat(doChannels).ToList();
            
            // 使用合并后的通道集合调用单参数版本
            return await ExtractBatchInfoAsync(allChannels);
        }

        /// <summary>
        /// 从Excel导入的数据创建通道映射
        /// </summary>
        /// <param name="excelData">Excel导入的数据</param>
        /// <returns>创建的通道映射集合</returns>
        public async Task<IEnumerable<ChannelMapping>> CreateChannelMappingsFromExcelAsync(IEnumerable<ExcelPointData> excelData)
        {
            if (excelData == null || !excelData.Any())
                return Enumerable.Empty<ChannelMapping>();
                
            // 将Excel数据转换为通道映射
            var channelMappings = new List<ChannelMapping>();
            
            foreach (var item in excelData)
            {
                // 创建通道映射实体
                var channel = new ChannelMapping
                {
                    ModuleName = item.ModuleName,
                    ModuleType = item.ModuleType,
                    Tag = item.Tag,
                    VariableName = item.VariableName,
                    VariableDescription = item.VariableDescription,
                    // 添加其他属性映射
                };
                
                channelMappings.Add(channel);
            }
            
            return await Task.FromResult(channelMappings);
        }

        /// <summary>
        /// 获取所有批次
        /// </summary>
        /// <returns>批次信息列表</returns>
        public async Task<IEnumerable<ViewModels.BatchInfo>> GetAllBatchesAsync()
        {
            // 获取所有通道
            var allChannels = await GetAllChannelMappingsAsync();
            
            // 使用现有方法提取批次信息
            return await ExtractBatchInfoAsync(allChannels);
        }

        /// <summary>
        /// 获取或创建批次信息
        /// </summary>
        /// <param name="batchName">批次名称</param>
        /// <param name="itemCount">批次包含的项目数量</param>
        /// <returns>批次信息</returns>
        public async Task<ViewModels.BatchInfo> GetOrCreateBatchAsync(string batchName, int itemCount)
        {
            if (string.IsNullOrEmpty(batchName))
                throw new ArgumentException("批次名称不能为空", nameof(batchName));
                
            // 获取所有批次
            var batches = await GetAllBatchesAsync();
            
            // 查找匹配的批次
            var existingBatch = batches.FirstOrDefault(b => b.BatchName == batchName);
            
            // 如果找到则返回，否则创建新批次
            if (existingBatch != null)
                return existingBatch;
                
            // 创建新批次
            return new ViewModels.BatchInfo(batchName, itemCount);
        }

        /// <summary>
        /// (示例方法) 根据导入的Excel数据创建并初始化ChannelMapping对象集合。
        /// 这个方法可能不存在，或者逻辑分散在DataEditViewModel中。
        /// </summary>
        public async Task<ObservableCollection<ChannelMapping>> CreateAndInitializeChannelMappingsAsync(IEnumerable<ExcelPointData> importedData)
        {
            var allChannels = new ObservableCollection<ChannelMapping>();
            if (importedData == null || !importedData.Any())
            {
                return allChannels;
            }

            var importTime = DateTime.Now; // 统一导入时间

            // 获取所有现有的 ChannelTag 以确保唯一性，或根据需求调整 TestId 的唯一性逻辑
            // 这是一个简化的例子，实际项目中可能需要更复杂的唯一性检查或从数据库获取
            var existingChannelTags = new HashSet<string>(); // 假设从某处获取

            int currentId = 1; // 用于生成临时的唯一 TestId

            foreach (var pointData in importedData)
            {
                if (pointData == null) continue;

                var channel = new ChannelMapping
                {
                    // 基础属性映射
                    Id = Guid.NewGuid(), // 确保每次创建都有新的GUID
                    // TestId = pointData.TestId, // 如果Excel中的TestId保证唯一性，则直接使用
                    TestId = $"TempID_{currentId++}", // 临时生成，确保唯一性，后续可优化
                    TestTag = $"Import_{importTime:yyyyMMddHHmmss}", // 统一的测试标签前缀
                    ChannelTag = pointData.ChannelTag,
                    VariableName = pointData.VariableName,
                    ModuleType = pointData.ModuleType,
                    ModuleName = pointData.ModuleName,
                    StationName = pointData.StationName,
                    VariableDescription = pointData.VariableDescription,
                    DataType = pointData.DataType,
                    PlcCommunicationAddress = pointData.PlcCommunicationAddress, // 被测PLC通讯地址
                    WireSystem = pointData.WireSystem,
                    PowerSupplyType = pointData.PowerSupplyType,
                    Status = "Imported", // 临时状态，将被InitializeChannelFromImport覆盖
                    
                    // 这些属性将由 InitializeChannelFromImport 方法根据 pointData 和业务规则设置
                    // RangeLowerLimitValue, RangeUpperLimitValue,
                    // SLLSetValueNumber, SLSetValueNumber, SHSetValueNumber, SHHSetValueNumber,
                    // ExpectedValue, ActualValue, Deviation, DeviationPercent,
                    // Value0Percent, Value25Percent, Value50Percent, Value75Percent, Value100Percent,
                    // ... 其他数值和状态属性 ...
                };

                // 调用 IChannelStateManager 来初始化通道的业务状态
                // 这里传入 pointData，以便 IChannelStateManager 可以访问Excel的原始值进行解析和状态初始化
                _channelStateManager.InitializeChannelFromImport(channel, pointData, importTime);

                allChannels.Add(channel);
            }

            // 可选：如果需要持久化这些新创建的通道，在这里调用仓储的保存方法
            // await _repository.AddChannelMappingsAsync(allChannels);

            return allChannels;
        }

        /// <summary>
        /// (修改现有方法) 分配测试通道，并使用IChannelStateManager重置状态。
        /// </summary>
        public async Task<ObservableCollection<ChannelMapping>> AllocateChannelsTestAsync(ObservableCollection<ChannelMapping> channels)
        {
            if (channels == null || !channels.Any() || _testPlcConfig == null || _testPlcConfig.CommentsTables == null || !_testPlcConfig.CommentsTables.Any())
            {
                System.Diagnostics.Debug.WriteLine("AllocateChannelsTestAsync: 输入参数无效或测试PLC配置为空。");
                return channels; // 返回原始集合或进行错误处理
            }

            // 获取配置的通道信息
            var configChannels = GetChannelCountsFromConfig();

            var aiChannelsToAllocate = channels.Where(c => c.ModuleType == "AI" || c.ModuleType == "AINone").ToList();
            var aoChannelsToAllocate = channels.Where(c => c.ModuleType == "AO" || c.ModuleType == "AONone").ToList();
            var diChannelsToAllocate = channels.Where(c => c.ModuleType == "DI" || c.ModuleType == "DINone").ToList();
            var doChannelsToAllocate = channels.Where(c => c.ModuleType == "DO" || c.ModuleType == "DONone").ToList();
            
            System.Diagnostics.Debug.WriteLine($"准备分配: AI={aiChannelsToAllocate.Count}, AO={aoChannelsToAllocate.Count}, DI={diChannelsToAllocate.Count}, DO={doChannelsToAllocate.Count}");


            // AI 被测 -> AO 测试 (AI-AO)
            AllocateChannelsWithConfigAndApplyState(aiChannelsToAllocate, configChannels.aoChannels.ToList(), configChannels.totalAoChannels);
            // AO 被测 -> AI 测试 (AO-AI)
            AllocateChannelsWithConfigAndApplyState(aoChannelsToAllocate, configChannels.aiChannels.ToList(), configChannels.totalAiChannels);
            // DI 被测 -> DO 测试 (DI-DO)
            AllocateChannelsWithConfigAndApplyState(diChannelsToAllocate, configChannels.doChannels.ToList(), configChannels.totalDoChannels);
            // DO 被测 -> DI 测试 (DO-DI)
            AllocateChannelsWithConfigAndApplyState(doChannelsToAllocate, configChannels.diChannels.ToList(), configChannels.totalDiChannels);
            
            // 可选: 如果需要持久化分配信息，可以在这里调用仓储更新
            // await _repository.UpdateChannelMappingsAsync(channels);

            System.Diagnostics.Debug.WriteLine("AllocateChannelsTestAsync: 通道分配完成。");
            return channels; // 返回修改后的集合
        }

        /// <summary>
        /// 使用配置中的通道信息分配通道，并调用IChannelStateManager应用状态
        /// </summary>
        private void AllocateChannelsWithConfigAndApplyState(
            List<ChannelMapping> channelsToAllocate, 
            List<ComparisonTable> testChannelMappings, 
            int totalTestChannelsForType)
        {
            if (channelsToAllocate == null || !channelsToAllocate.Any() || testChannelMappings == null || !testChannelMappings.Any() || totalTestChannelsForType <= 0)
            {
                return;
            }

            try
            {
                int batchCount = (int)Math.Ceiling((double)channelsToAllocate.Count / totalTestChannelsForType);
                
                for (int i = 0; i < channelsToAllocate.Count; i++)
                {
                    var channel = channelsToAllocate[i];
                    if (channel == null) continue;

                    int batchNumber = i / totalTestChannelsForType + 1;
                    int indexInBatch = i % totalTestChannelsForType;

                    if (indexInBatch >= testChannelMappings.Count)
                    {
                        System.Diagnostics.Debug.WriteLine($"警告：通道 {channel.VariableName} ({channel.ChannelTag}) 无法分配测试PLC通道，索引 {indexInBatch} 超出可用测试通道范围 {testChannelMappings.Count}。");
                        // 对于无法分配的通道，可以考虑清除其分配信息或标记为错误
                        _channelStateManager.ClearAllocationInfo(channel); // 清除可能存在的旧分配并重置状态
                        continue;
                    }
                    
                    var testChannelMapping = testChannelMappings[indexInBatch];
                    if (testChannelMapping == null)
                    {
                         System.Diagnostics.Debug.WriteLine($"警告：通道 {channel.VariableName} ({channel.ChannelTag}) 的目标测试PLC映射在索引 {indexInBatch} 处为空。");
                        _channelStateManager.ClearAllocationInfo(channel);
                        continue;
                    }
                    
                    string testBatchName = $"批次{batchNumber}";
                    string testPlcChannelTag = testChannelMapping.ChannelAddress; // 通常是 TagName
                    string testPlcCommAddress = testChannelMapping.CommunicationAddress;

                    // 调用 IChannelStateManager 来应用分配信息并更新状态
                    _channelStateManager.ApplyAllocationInfo(channel, testBatchName, testPlcChannelTag, testPlcCommAddress);
                    
                    // Debug.WriteLine($"通道 {channel.VariableName} ({channel.ChannelTag}) 分配到 {testBatchName}, 测试PLC: {testPlcChannelTag} ({testPlcCommAddress})");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AllocateChannelsWithConfigAndApplyState 发生错误: {ex.Message}");
                // 考虑对列表中的通道进行回滚或错误标记
                foreach(var ch in channelsToAllocate)
                {
                    if(ch != null) _channelStateManager.ClearAllocationInfo(ch); // 出错时清除所有本次尝试分配的通道
                }
            }
        }

        /// <summary>
        /// 清除所有通道分配信息
        /// </summary>
        /// <param name="channels">需要清除分配信息的通道集合</param>
        /// <returns>清除分配信息后的通道集合</returns>
        public async Task<ObservableCollection<ChannelMapping>> ClearAllChannelAllocationsAsync(ObservableCollection<ChannelMapping> channels)
        {
            if (channels == null || !channels.Any())
            {
                return channels;
            }

            var channelsToClear = channels.ToList(); // 操作副本以避免修改迭代中的集合问题

            foreach (var channel in channelsToClear)
            {
                if (channel != null)
                {
                    _channelStateManager.ClearAllocationInfo(channel);
                }
            }
            
            // 可选: 如果需要持久化清除操作，在这里调用仓储更新
            // await _repository.UpdateChannelMappingsAsync(channelsToClear); // 或者标记为已删除分配等

            System.Diagnostics.Debug.WriteLine($"ClearAllChannelAllocationsAsync: 清除了 {channelsToClear.Count} 个通道的分配信息。");
            // 由于 ChannelMapping 对象是引用类型，channels 集合中的对象已经被修改。
            // 如果 ObservableCollection 需要强制刷新UI，可能需要额外操作，但通常属性更改会通知。
            return channels; 
        }
    }
}
