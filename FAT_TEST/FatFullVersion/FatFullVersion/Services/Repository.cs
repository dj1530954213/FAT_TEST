using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FatFullVersion.Entities;
using FatFullVersion.Entities.EntitiesEnum;
using FatFullVersion.Entities.ValueObject;
using FatFullVersion.IServices;

namespace FatFullVersion.Services
{
    /// <summary>
    /// 仓储层实现类，提供数据持久化服务的具体实现
    /// </summary>
    public class Repository : IRepository
    {
        // 作为示例，这里使用内存中的对象来存储连接配置
        // 真实环境中应该从配置文件、数据库等持久化存储中获取
        private PlcConnectionConfig _plcConnectionConfig;

        /// <summary>
        /// 构造函数
        /// </summary>
        public Repository()
        {
            // 初始化默认配置
            _plcConnectionConfig = new PlcConnectionConfig();
        }

        /// <summary>
        /// 获取PLC连接配置
        /// </summary>
        /// <returns>PLC连接配置</returns>
        public async Task<PlcConnectionConfig> GetPlcConnectionConfigAsync()
        {
            // 在实际应用中，这里应该从数据库、配置文件等读取配置
            return await Task.FromResult(_plcConnectionConfig);
        }

        /// <summary>
        /// 保存PLC连接配置
        /// </summary>
        /// <param name="config">PLC连接配置</param>
        /// <returns>保存操作是否成功</returns>
        public async Task<bool> SavePlcConnectionConfigAsync(PlcConnectionConfig config)
        {
            if (config == null)
                return false;

            // 在实际应用中，这里应该将配置保存到数据库、配置文件等
            _plcConnectionConfig = config;
            return await Task.FromResult(true);
        }

        public async Task<string> GetPlcCommunicationAddress(string channelTag)
        {
            switch (channelTag)
            {
                case "1":return "40001";
                case "2": return "40003";
                default: return "40001";
            }
        }

        public async Task<List<ComparisonTable>> GetComparisonTablesAsync()
        {
            var result = new List<ComparisonTable>
            {
                new ComparisonTable("AI1_1","40001",TestPlcChannelType.AI),
                new ComparisonTable("AI1_2","40003",TestPlcChannelType.AI),
                new ComparisonTable("AI1_3","40005",TestPlcChannelType.AI),
                new ComparisonTable("AI1_4","40007",TestPlcChannelType.AI),
                new ComparisonTable("AI2_1","40009",TestPlcChannelType.AI),
                new ComparisonTable("AI2_2","40011",TestPlcChannelType.AI),
                new ComparisonTable("AI2_3","40013",TestPlcChannelType.AI),
                new ComparisonTable("AI2_4","40015",TestPlcChannelType.AI),

                new ComparisonTable("AO1_1","40017",TestPlcChannelType.AO),
                new ComparisonTable("AO1_2","40019",TestPlcChannelType.AO),
                new ComparisonTable("AO1_3","40021",TestPlcChannelType.AO),
                new ComparisonTable("AO1_4","40023",TestPlcChannelType.AO),
                new ComparisonTable("AO2_1","40025",TestPlcChannelType.AO),
                new ComparisonTable("AO2_2","40027",TestPlcChannelType.AO),
                new ComparisonTable("AO2_3","40029",TestPlcChannelType.AO),
                new ComparisonTable("AO2_4","40031",TestPlcChannelType.AO),

                new ComparisonTable("DI1_1","00001",TestPlcChannelType.DI),
                new ComparisonTable("DI1_2","00002",TestPlcChannelType.DI),
                new ComparisonTable("DI1_3","00003",TestPlcChannelType.DI),
                new ComparisonTable("DI1_4","00004",TestPlcChannelType.DI),
                new ComparisonTable("DI1_5","00005",TestPlcChannelType.DI),
                new ComparisonTable("DI1_6","00006",TestPlcChannelType.DI),
                new ComparisonTable("DI1_7","00007",TestPlcChannelType.DI),
                new ComparisonTable("DI1_8","00008",TestPlcChannelType.DI),
                new ComparisonTable("DI2_1","00009",TestPlcChannelType.DI),
                new ComparisonTable("DI2_2","00010",TestPlcChannelType.DI),
                new ComparisonTable("DI2_3","00011",TestPlcChannelType.DI),
                new ComparisonTable("DI2_4","00012",TestPlcChannelType.DI),
                new ComparisonTable("DI2_5","00013",TestPlcChannelType.DI),
                new ComparisonTable("DI2_6","00014",TestPlcChannelType.DI),
                new ComparisonTable("DI2_7","00015",TestPlcChannelType.DI),
                new ComparisonTable("DI2_8","00016",TestPlcChannelType.DI),

                new ComparisonTable("DO1_1","00017",TestPlcChannelType.DO),
                new ComparisonTable("DO1_2","00018",TestPlcChannelType.DO),
                new ComparisonTable("DO1_3","00019",TestPlcChannelType.DO),
                new ComparisonTable("DO1_4","00020",TestPlcChannelType.DO),
                new ComparisonTable("DO1_5","00021",TestPlcChannelType.DO),
                new ComparisonTable("DO1_6","00022",TestPlcChannelType.DO),
                new ComparisonTable("DO1_7","00023",TestPlcChannelType.DO),
                new ComparisonTable("DO1_8","00024",TestPlcChannelType.DO),
                new ComparisonTable("DO2_1","00025",TestPlcChannelType.DO),
                new ComparisonTable("DO2_2","00026",TestPlcChannelType.DO),
                new ComparisonTable("DO2_3","00027",TestPlcChannelType.DO),
                new ComparisonTable("DO2_4","00028",TestPlcChannelType.DO),
                new ComparisonTable("DO2_5","00029",TestPlcChannelType.DO),
                new ComparisonTable("DO2_6","00030",TestPlcChannelType.DO),
                new ComparisonTable("DO2_7","00031",TestPlcChannelType.DO),
                new ComparisonTable("DO2_8","00032",TestPlcChannelType.DO),
            };
            return result;
        }
    }
}
