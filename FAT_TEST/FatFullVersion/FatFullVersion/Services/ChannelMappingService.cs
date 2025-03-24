using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FatFullVersion.IServices;
using FatFullVersion.Models;
using FatFullVersion.Entities;
using FatFullVersion.Entities.EntitiesEnum;
using FatFullVersion.Entities.ValueObject;

namespace FatFullVersion.Services
{
    /// <summary>
    /// 通道映射服务实现类，提供通道分配和管理功能的具体实现
    /// </summary>
    public class ChannelMappingService : IChannelMappingService
    {
        /// <summary>
        /// 默认测试PLC配置，如果没有配置文件或数据库中的配置，则使用此默认值
        /// </summary>
        private readonly (int AoModules, int AoChannelsPerModule, int AiModules, int AiChannelsPerModule,
                          int DoModules, int DoChannelsPerModule, int DiModules, int DiChannelsPerModule) _defaultTestPlcConfig
            = (2, 4, 2, 4, 2, 8, 2, 8);

        /// <summary>
        /// 当前使用的测试PLC配置
        /// </summary>
        private TestPlcConfig _testPlcConfig;

        /// <summary>
        /// 构造函数
        /// </summary>
        public ChannelMappingService()
        {
            // 初始化默认PLC配置
            _testPlcConfig = new TestPlcConfig
            {
                BrandType = PlcBrandTypeEnum.Micro850,
                IpAddress = "192.168.1.1",
                CommentsTables = new List<ComparisonTable>()
            };

            // 默认添加一些通道映射关系
            InitializeDefaultChannelMappings();
        }

        /// <summary>
        /// 初始化默认的通道映射关系,测试使用后续使用数据库接口获取
        /// </summary>
        private void InitializeDefaultChannelMappings()
        {
            // 添加默认AI通道
            for (int i = 1; i <= _defaultTestPlcConfig.AiModules; i++)
            {
                for (int j = 1; j <= _defaultTestPlcConfig.AiChannelsPerModule; j++)
                {
                    _testPlcConfig.CommentsTables.Add(new ComparisonTable(
                        $"AI{i}_{j}", 
                        $"AI{i}.{j}", 
                        TestPlcChannelType.AI));
                }
            }

            // 添加默认AO通道
            for (int i = 1; i <= _defaultTestPlcConfig.AoModules; i++)
            {
                for (int j = 1; j <= _defaultTestPlcConfig.AoChannelsPerModule; j++)
                {
                    _testPlcConfig.CommentsTables.Add(new ComparisonTable(
                        $"AO{i}_{j}", 
                        $"AO{i}.{j}", 
                        TestPlcChannelType.AO));
                }
            }

            // 添加默认DI通道
            for (int i = 1; i <= _defaultTestPlcConfig.DiModules; i++)
            {
                for (int j = 1; j <= _defaultTestPlcConfig.DiChannelsPerModule; j++)
                {
                    _testPlcConfig.CommentsTables.Add(new ComparisonTable(
                        $"DI{i}_{j}", 
                        $"DI{i}.{j}", 
                        TestPlcChannelType.DI));
                }
            }

            // 添加默认DO通道
            for (int i = 1; i <= _defaultTestPlcConfig.DoModules; i++)
            {
                for (int j = 1; j <= _defaultTestPlcConfig.DoChannelsPerModule; j++)
                {
                    _testPlcConfig.CommentsTables.Add(new ComparisonTable(
                        $"DO{i}_{j}", 
                        $"DO{i}.{j}", 
                        TestPlcChannelType.DO));
                }
            }
        }

        /// <summary>
        /// 使用新的测试PLC配置进行通道分配
        /// </summary>
        /// <param name="aiChannels">被测PLC的AI通道列表</param>
        /// <param name="aoChannels">被测PLC的AO通道列表</param>
        /// <param name="diChannels">被测PLC的DI通道列表</param>
        /// <param name="doChannels">被测PLC的DO通道列表</param>
        /// <param name="testPlcConfig">测试PLC配置信息</param>
        /// <returns>分配通道后的通道映射信息</returns>
        public async Task<(IEnumerable<ChannelMapping> AI, IEnumerable<ChannelMapping> AO, IEnumerable<ChannelMapping> DI, IEnumerable<ChannelMapping> DO)> 
            AllocateChannelsAsync(
                IEnumerable<ChannelMapping> aiChannels,
                IEnumerable<ChannelMapping> aoChannels,
                IEnumerable<ChannelMapping> diChannels,
                IEnumerable<ChannelMapping> doChannels,
                TestPlcConfig testPlcConfig)
        {
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
                var aoMappings = testPlcConfig.CommentsTables
                    .Where(t => t.ChannelType == TestPlcChannelType.AO)
                    .ToList();
                var aiMappings = testPlcConfig.CommentsTables
                    .Where(t => t.ChannelType == TestPlcChannelType.AI)
                    .ToList();
                var doMappings = testPlcConfig.CommentsTables
                    .Where(t => t.ChannelType == TestPlcChannelType.DO)
                    .ToList();
                var diMappings = testPlcConfig.CommentsTables
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
        /// 使用新的测试PLC配置进行通道分配(测试)
        /// </summary>
        /// <param name="aiChannels">被测PLC的AI通道列表</param>
        /// <param name="aoChannels">被测PLC的AO通道列表</param>
        /// <param name="diChannels">被测PLC的DI通道列表</param>
        /// <param name="doChannels">被测PLC的DO通道列表</param>
        /// <param name="testPlcConfig">测试PLC配置信息</param>
        /// <returns>分配通道后的通道映射信息</returns>
        public async Task<(IEnumerable<ChannelMapping> AI, IEnumerable<ChannelMapping> AO, IEnumerable<ChannelMapping> DI, IEnumerable<ChannelMapping> DO)>
            AllocateChannelsTestAsync(
                IEnumerable<ChannelMapping> aiChannels,
                IEnumerable<ChannelMapping> aoChannels,
                IEnumerable<ChannelMapping> diChannels,
                IEnumerable<ChannelMapping> doChannels)
        {
            // 设置当前使用的配置
            await SetTestPlcConfigAsync(_testPlcConfig);

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
        /// 使用配置中的通道信息分配通道
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
                return;

            // 计算需要分配的批次数
            int batchCount = (int)Math.Ceiling((double)channels.Count / totalTestChannels);
            
            // 为每个通道分配批次和测试PLC通道
            for (int i = 0; i < channels.Count; i++)
            {
                // 计算批次号（从1开始）
                int batchNumber = i / totalTestChannels + 1;
                
                // 计算在当前批次中的索引位置
                int indexInBatch = i % totalTestChannels;
                
                // 如果索引超出了测试通道的范围，则跳过
                if (indexInBatch >= testChannelMappings.Count)
                    continue;
                
                // 获取对应的测试通道映射
                var testChannelMapping = testChannelMappings[indexInBatch];
                
                // 更新通道信息
                channels[i].TestBatch = $"批次{batchNumber}";
                channels[i].TestPLCChannelTag = testChannelMapping.ChannelAddress;
                channels[i].TestPLCCommunicationAddress = testChannelMapping.CommunicationAddress;
            }
        }

        /// <summary>
        /// 默认通道分批功能：根据测试PLC的配置信息获取可用测试通道数，对应被测PLC进行通道自动分配
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
        public async Task<(IEnumerable<ChannelMapping> AI, IEnumerable<ChannelMapping> AO, IEnumerable<ChannelMapping> DI, IEnumerable<ChannelMapping> DO)> 
            AllocateChannelsAsync(
                IEnumerable<ChannelMapping> aiChannels,
                IEnumerable<ChannelMapping> aoChannels,
                IEnumerable<ChannelMapping> diChannels,
                IEnumerable<ChannelMapping> doChannels,
                int testAoModuleCount, int testAoChannelPerModule,
                int testAiModuleCount, int testAiChannelPerModule,
                int testDoModuleCount, int testDoChannelPerModule,
                int testDiModuleCount, int testDiChannelPerModule)
        {
            // 为避免阻塞UI线程，异步执行分配操作
            return await Task.Run(() =>
            {
                // 计算测试PLC各类型通道的总数量
                int totalTestAoChannels = testAoModuleCount * testAoChannelPerModule;
                int totalTestAiChannels = testAiModuleCount * testAiChannelPerModule;
                int totalTestDoChannels = testDoModuleCount * testDoChannelPerModule;
                int totalTestDiChannels = testDiModuleCount * testDiChannelPerModule;

                // 转换为列表以便修改
                var aiList = aiChannels.ToList();
                var aoList = aoChannels.ToList();
                var diList = diChannels.ToList();
                var doList = doChannels.ToList();

                // 1. 为AI通道分配批次和测试PLC的AO通道(AI-AO)
                AllocateChannels(aiList, totalTestAoChannels, "AO", testAoModuleCount, testAoChannelPerModule);

                // 2. 为AO通道分配批次和测试PLC的AI通道(AO-AI)
                AllocateChannels(aoList, totalTestAiChannels, "AI", testAiModuleCount, testAiChannelPerModule);

                // 3. 为DI通道分配批次和测试PLC的DO通道(DI-DO)
                AllocateChannels(diList, totalTestDoChannels, "DO", testDoModuleCount, testDoChannelPerModule);

                // 4. 为DO通道分配批次和测试PLC的DI通道(DO-DI)
                AllocateChannels(doList, totalTestDiChannels, "DI", testDiModuleCount, testDiChannelPerModule);

                return (aiList, aoList, diList, doList);
            });
        }

        /// <summary>
        /// 为指定类型的通道分配测试PLC通道和批次
        /// </summary>
        /// <param name="channels">通道列表</param>
        /// <param name="totalTestChannels">测试PLC通道总数</param>
        /// <param name="testChannelType">测试PLC通道类型</param>
        /// <param name="moduleCount">模块数量</param>
        /// <param name="channelsPerModule">每个模块的通道数</param>
        private void AllocateChannels(List<ChannelMapping> channels, int totalTestChannels, string testChannelType, int moduleCount, int channelsPerModule)
        {
            if (channels == null || channels.Count == 0)
                return;

            // 计算需要分配的批次数
            int batchCount = (int)Math.Ceiling((double)channels.Count / totalTestChannels);
            
            // 为每个通道分配批次和测试PLC通道
            for (int i = 0; i < channels.Count; i++)
            {
                // 计算批次号（从1开始）
                int batchNumber = i / totalTestChannels + 1;
                
                // 计算在当前批次中的索引位置
                int indexInBatch = i % totalTestChannels;
                
                // 计算模块号（从1开始）
                int moduleNumber = indexInBatch / channelsPerModule + 1;
                
                // 计算在当前模块中的通道号（从1开始）
                int channelNumberInModule = indexInBatch % channelsPerModule + 1;
                
                // 更新通道信息
                channels[i].TestBatch = $"批次{batchNumber}";
                channels[i].TestPLCChannelTag = $"{testChannelType}{moduleNumber}_{channelNumberInModule}";
                channels[i].TestPLCCommunicationAddress = $"{testChannelType}{moduleNumber}.{channelNumberInModule}";
            }
        }

        /// <summary>
        /// 单个通道的对应关系修改：当默认通道分配好后用户有可能需要调整对应关系
        /// </summary>
        /// <param name="targetChannel">要修改的被测PLC通道</param>
        /// <param name="newTestPlcChannel">新分配的测试PLC通道标识</param>
        /// <param name="newTestPlcCommAddress">新分配的测试PLC通讯地址</param>
        /// <param name="allChannels">所有通道的集合，用于查找和更新原有的映射关系</param>
        /// <returns>修改后的目标通道信息</returns>
        public async Task<ChannelMapping> UpdateChannelMappingAsync(
            ChannelMapping targetChannel, 
            string newTestPlcChannel, 
            string newTestPlcCommAddress,
            IEnumerable<ChannelMapping> allChannels)
        {
            return await Task.Run(() =>
            {
                if (targetChannel == null)
                    throw new ArgumentNullException(nameof(targetChannel));

                // 查找使用了这个新测试PLC通道的现有映射
                var existingMapping = allChannels?.FirstOrDefault(c => 
                    c.TestPLCChannelTag == newTestPlcChannel && 
                    c.TestPLCCommunicationAddress == newTestPlcCommAddress && 
                    c != targetChannel);

                // 如果找到，清除该映射的测试PLC信息
                if (existingMapping != null)
                {
                    existingMapping.TestPLCChannelTag = string.Empty;
                    existingMapping.TestPLCCommunicationAddress = string.Empty;
                    // 注意：不清除批次信息，因为可能同一批次中的其他通道仍然在使用
                }

                // 更新目标通道的测试PLC信息
                targetChannel.TestPLCChannelTag = newTestPlcChannel;
                targetChannel.TestPLCCommunicationAddress = newTestPlcCommAddress;

                return targetChannel;
            });
        }

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
        public async Task<TestPlcConfig> GetTestPlcConfigAsync()
        {
            return await Task.Run(() => _testPlcConfig);
        }

        /// <summary>
        /// 获取测试PLC通道的配置信息
        /// </summary>
        /// <returns>测试PLC的通道配置信息</returns>
        public async Task<(int AoModules, int AoChannelsPerModule, int AiModules, int AiChannelsPerModule, 
                int DoModules, int DoChannelsPerModule, int DiModules, int DiChannelsPerModule)> 
            GetTestPlcConfigurationAsync()
        {
            // 优先使用当前配置中的通道数据
            return await Task.Run(() =>
            {
                if (_testPlcConfig != null && _testPlcConfig.CommentsTables != null && _testPlcConfig.CommentsTables.Any())
                {
                    // 计算各类型通道的数量和模块数
                    var channelCounts = GetChannelCountsFromConfig();
                    return (
                        GetModuleCount(channelCounts.aoChannels), GetChannelsPerModule(channelCounts.aoChannels),
                        GetModuleCount(channelCounts.aiChannels), GetChannelsPerModule(channelCounts.aiChannels),
                        GetModuleCount(channelCounts.doChannels), GetChannelsPerModule(channelCounts.doChannels),
                        GetModuleCount(channelCounts.diChannels), GetChannelsPerModule(channelCounts.diChannels)
                    );
                }
                
                // 如果没有配置或配置为空，使用默认配置
                return _defaultTestPlcConfig;
            });
        }

        /// <summary>
        /// 从配置中获取通道数量统计
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
        private int GetModuleCount(IEnumerable<ComparisonTable> channels)
        {
            if (channels == null || !channels.Any())
                return 0;

            // 从通道地址中提取模块编号
            var moduleNumbers = channels
                .Select(c => 
                {
                    // 提取模块编号，格式为"XX1_2"，其中1为模块编号
                    var parts = c.ChannelAddress.Split('_');
                    if (parts.Length > 0)
                    {
                        var typeAndModule = parts[0]; // 例如"AO1"
                        var moduleNumberStr = string.Empty;
                        
                        // 去除类型前缀，保留数字部分
                        for (int i = 0; i < typeAndModule.Length; i++)
                        {
                            if (char.IsDigit(typeAndModule[i]))
                            {
                                moduleNumberStr = typeAndModule.Substring(i);
                                break;
                            }
                        }

                        if (!string.IsNullOrEmpty(moduleNumberStr) && int.TryParse(moduleNumberStr, out int moduleNumber))
                        {
                            return moduleNumber;
                        }
                    }
                    return 0;
                })
                .Where(m => m > 0)
                .Distinct()
                .ToList();

            return moduleNumbers.Count;
        }

        /// <summary>
        /// 计算每个模块的通道数量
        /// </summary>
        /// <param name="channels">通道列表</param>
        /// <returns>每个模块的平均通道数</returns>
        private int GetChannelsPerModule(IEnumerable<ComparisonTable> channels)
        {
            if (channels == null || !channels.Any())
                return 0;

            // 提取模块编号和通道在模块中的序号
            var moduleChannels = new Dictionary<int, List<int>>();
            foreach (var channel in channels)
            {
                // 提取模块编号和通道编号，格式为"XX1_2"，其中1为模块编号，2为通道编号
                var parts = channel.ChannelAddress.Split('_');
                if (parts.Length >= 2)
                {
                    var typeAndModule = parts[0]; // 例如"AO1"
                    var moduleNumberStr = string.Empty;
                    
                    // 去除类型前缀，保留数字部分
                    for (int i = 0; i < typeAndModule.Length; i++)
                    {
                        if (char.IsDigit(typeAndModule[i]))
                        {
                            moduleNumberStr = typeAndModule.Substring(i);
                            break;
                        }
                    }

                    if (!string.IsNullOrEmpty(moduleNumberStr) && int.TryParse(moduleNumberStr, out int moduleNumber) &&
                        int.TryParse(parts[1], out int channelNumber))
                    {
                        if (!moduleChannels.ContainsKey(moduleNumber))
                        {
                            moduleChannels[moduleNumber] = new List<int>();
                        }
                        moduleChannels[moduleNumber].Add(channelNumber);
                    }
                }
            }

            // 如果没有有效的模块通道数据，则返回0
            if (moduleChannels.Count == 0)
                return 0;

            // 计算每个模块的通道数量平均值
            return (int)Math.Ceiling(moduleChannels.Values.Average(list => list.Count));
        }

        /// <summary>
        /// 获取已被分配的测试PLC通道列表
        /// </summary>
        /// <param name="allChannels">所有通道的集合</param>
        /// <returns>已分配的测试PLC通道信息</returns>
        public async Task<IEnumerable<(string ChannelType, string ChannelTag, string CommAddress)>> GetAllocatedTestChannelsAsync(
            IEnumerable<ChannelMapping> allChannels)
        {
            return await Task.Run(() =>
            {
                var allocatedChannels = allChannels
                    .Where(c => !string.IsNullOrEmpty(c.TestPLCChannelTag) && !string.IsNullOrEmpty(c.TestPLCCommunicationAddress))
                    .Select(c => (
                        // 从TestPLCChannelTag提取通道类型（如"AO"、"AI"等）
                        ChannelType: c.TestPLCChannelTag.Substring(0, 2),
                        ChannelTag: c.TestPLCChannelTag,
                        CommAddress: c.TestPLCCommunicationAddress
                    ))
                    .ToList();

                return allocatedChannels;
            });
        }

        /// <summary>
        /// 清除所有通道分配信息
        /// </summary>
        /// <param name="channels">需要清除分配信息的通道集合</param>
        /// <returns>清除分配信息后的通道集合</returns>
        public async Task<IEnumerable<ChannelMapping>> ClearAllChannelAllocationsAsync(IEnumerable<ChannelMapping> channels)
        {
            return await Task.Run(() =>
            {
                var updatedChannels = channels.ToList();
                
                foreach (var channel in updatedChannels)
                {
                    channel.TestPLCChannelTag = string.Empty;
                    channel.TestPLCCommunicationAddress = string.Empty;
                    channel.TestBatch = string.Empty;
                }
                
                return updatedChannels;
            });
        }
    }
}
