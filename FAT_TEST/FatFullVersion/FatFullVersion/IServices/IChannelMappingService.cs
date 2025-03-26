using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FatFullVersion.Models;
using FatFullVersion.Entities;
using FatFullVersion.ViewModels;

namespace FatFullVersion.IServices
{
    /// <summary>
    /// 通道映射服务接口，提供通道分配和管理功能
    /// </summary>
    public interface IChannelMappingService
    {
        /// <summary>
        /// 默认通道分批功能：根据测试PLC的配置信息获取可用测试通道数，对应被测PLC进行通道自动分配
        /// </summary>
        /// <param name="aiChannels">被测PLC的AI通道列表</param>
        /// <param name="aoChannels">被测PLC的AO通道列表</param>
        /// <param name="diChannels">被测PLC的DI通道列表</param>
        /// <param name="doChannels">被测PLC的DO通道列表</param>
        /// <param name="testPlcConfig">测试PLC配置信息</param>
        /// <returns>分配通道后的通道映射信息</returns>
        Task<(IEnumerable<ChannelMapping> AI, IEnumerable<ChannelMapping> AO, IEnumerable<ChannelMapping> DI, IEnumerable<ChannelMapping> DO)> 
            AllocateChannelsAsync(
                IEnumerable<ChannelMapping> aiChannels,
                IEnumerable<ChannelMapping> aoChannels,
                IEnumerable<ChannelMapping> diChannels,
                IEnumerable<ChannelMapping> doChannels,
                TestPlcConfig testPlcConfig);

        /// <summary>
        /// 测试使用的分配方法，后续替换为AllocateChannelsAsync
        /// </summary>
        /// <param name="aiChannels">被测PLC的AI通道列表</param>
        /// <param name="aoChannels">被测PLC的AO通道列表</param>
        /// <param name="diChannels">被测PLC的DI通道列表</param>
        /// <param name="doChannels">被测PLC的DO通道列表</param>
        /// <param name="testPlcConfig">测试PLC配置信息</param>
        /// <returns>分配通道后的通道映射信息</returns>
        Task<(IEnumerable<ChannelMapping> AI, IEnumerable<ChannelMapping> AO, IEnumerable<ChannelMapping> DI, IEnumerable<ChannelMapping> DO)>
            AllocateChannelsTestAsync(
                IEnumerable<ChannelMapping> aiChannels,
                IEnumerable<ChannelMapping> aoChannels,
                IEnumerable<ChannelMapping> diChannels,
                IEnumerable<ChannelMapping> doChannels);

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
        Task<(IEnumerable<ChannelMapping> AI, IEnumerable<ChannelMapping> AO, IEnumerable<ChannelMapping> DI, IEnumerable<ChannelMapping> DO)> 
            AllocateChannelsAsync(
                IEnumerable<ChannelMapping> aiChannels,
                IEnumerable<ChannelMapping> aoChannels,
                IEnumerable<ChannelMapping> diChannels,
                IEnumerable<ChannelMapping> doChannels,
                int testAoModuleCount, int testAoChannelPerModule,
                int testAiModuleCount, int testAiChannelPerModule,
                int testDoModuleCount, int testDoChannelPerModule,
                int testDiModuleCount, int testDiChannelPerModule);

        /// <summary>
        /// 单个通道的对应关系修改：当默认通道分配好后用户有可能需要调整对应关系
        /// </summary>
        /// <param name="targetChannel">要修改的被测PLC通道</param>
        /// <param name="newTestPlcChannel">新分配的测试PLC通道标识</param>
        /// <param name="newTestPlcCommAddress">新分配的测试PLC通讯地址</param>
        /// <param name="allChannels">所有通道的集合，用于查找和更新原有的映射关系</param>
        /// <returns>修改后的目标通道信息</returns>
        Task<ChannelMapping> UpdateChannelMappingAsync(
            ChannelMapping targetChannel, 
            string newTestPlcChannel, 
            string newTestPlcCommAddress,
            IEnumerable<ChannelMapping> allChannels);

        void AllocateResult(IEnumerable<ChannelMapping> aiChannels,
            IEnumerable<ChannelMapping> aoChannels,
            IEnumerable<ChannelMapping> diChannels,
            IEnumerable<ChannelMapping> doChannels,
            IEnumerable<TestResult> testResults);

        /// <summary>
        /// 设置当前使用的测试PLC配置
        /// </summary>
        /// <param name="config">测试PLC配置</param>
        /// <returns>操作是否成功</returns>
        Task<bool> SetTestPlcConfigAsync(TestPlcConfig config);

        /// <summary>
        /// 获取当前使用的测试PLC配置
        /// </summary>
        /// <returns>测试PLC配置</returns>
        Task<TestPlcConfig> GetTestPlcConfigAsync();

        /// <summary>
        /// 获取测试PLC通道的配置信息
        /// </summary>
        /// <returns>测试PLC的通道配置信息</returns>
        Task<(int AoModules, int AoChannelsPerModule, int AiModules, int AiChannelsPerModule, 
               int DoModules, int DoChannelsPerModule, int DiModules, int DiChannelsPerModule)> 
            GetTestPlcConfigurationAsync();

        /// <summary>
        /// 获取已被分配的测试PLC通道列表
        /// </summary>
        /// <returns>已分配的测试PLC通道信息</returns>
        Task<IEnumerable<(string ChannelType, string ChannelTag, string CommAddress)>> GetAllocatedTestChannelsAsync(
            IEnumerable<ChannelMapping> allChannels);

        /// <summary>
        /// 清除所有通道分配信息
        /// </summary>
        /// <param name="channels">需要清除分配信息的通道集合</param>
        /// <returns>清除分配信息后的通道集合</returns>
        Task<IEnumerable<ChannelMapping>> ClearAllChannelAllocationsAsync(IEnumerable<ChannelMapping> channels);

        /// <summary>
        /// 从通道映射信息中提取批次信息并管理批次状态
        /// </summary>
        /// <param name="aiChannels">AI通道列表</param>
        /// <param name="aoChannels">AO通道列表</param>
        /// <param name="diChannels">DI通道列表</param>
        /// <param name="doChannels">DO通道列表</param>
        /// <param name="testResults">测试结果列表</param>
        /// <returns>提取的批次信息集合</returns>
        Task<IEnumerable<ViewModels.BatchInfo>> ExtractBatchInfoAsync(
            IEnumerable<ChannelMapping> aiChannels,
            IEnumerable<ChannelMapping> aoChannels,
            IEnumerable<ChannelMapping> diChannels,
            IEnumerable<ChannelMapping> doChannels,
            IEnumerable<TestResult> testResults);
    }
}
