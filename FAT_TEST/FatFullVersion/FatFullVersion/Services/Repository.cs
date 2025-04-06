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
        public async Task<PlcConnectionConfig> GetTestPlcConnectionConfigAsync()
        {
            // 在实际应用中，这里应该从数据库、配置文件等读取配置
            return await Task.FromResult(_plcConnectionConfig);
        }

        public Task<PlcConnectionConfig> GetTargetPlcConnectionConfigAsync()
        {
            throw new NotImplementedException();
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
                new ComparisonTable("AI1_1","40101",TestPlcChannelType.AI),
                new ComparisonTable("AI1_2","40103",TestPlcChannelType.AI),
                new ComparisonTable("AI1_3","40105",TestPlcChannelType.AI),
                new ComparisonTable("AI1_4","40107",TestPlcChannelType.AI),

                new ComparisonTable("AO1_1","40111",TestPlcChannelType.AO),
                new ComparisonTable("AO1_2","40113",TestPlcChannelType.AO),
                new ComparisonTable("AO2_1","40115",TestPlcChannelType.AO),
                new ComparisonTable("AO2_2","40117",TestPlcChannelType.AO),
                new ComparisonTable("AO3_1","40119",TestPlcChannelType.AO),
                new ComparisonTable("AO3_2","40121",TestPlcChannelType.AO),

                new ComparisonTable("DI1_1","00101",TestPlcChannelType.DI),
                new ComparisonTable("DI1_2","00102",TestPlcChannelType.DI),
                new ComparisonTable("DI1_3","00103",TestPlcChannelType.DI),
                new ComparisonTable("DI1_4","00104",TestPlcChannelType.DI),
                new ComparisonTable("DI1_5","00105",TestPlcChannelType.DI),
                new ComparisonTable("DI1_6","00106",TestPlcChannelType.DI),
                new ComparisonTable("DI1_7","00107",TestPlcChannelType.DI),
                new ComparisonTable("DI1_8","00108",TestPlcChannelType.DI),
                new ComparisonTable("DI1_9","00109",TestPlcChannelType.DI),
                new ComparisonTable("DI1_10","00110",TestPlcChannelType.DI),
                new ComparisonTable("DI1_11","00111",TestPlcChannelType.DI),
                new ComparisonTable("DI1_12","00112",TestPlcChannelType.DI),
                new ComparisonTable("DI1_13","00113",TestPlcChannelType.DI),
                new ComparisonTable("DI1_14","00114",TestPlcChannelType.DI),
                new ComparisonTable("DI1_15","00115",TestPlcChannelType.DI),
                new ComparisonTable("DI1_16","00116",TestPlcChannelType.DI),
                new ComparisonTable("DI1_17","00117",TestPlcChannelType.DI),
                new ComparisonTable("DI1_18","00118",TestPlcChannelType.DI),
                new ComparisonTable("DI1_19","00119",TestPlcChannelType.DI),
                new ComparisonTable("DI1_20","00120",TestPlcChannelType.DI),
                new ComparisonTable("DI1_21","00121",TestPlcChannelType.DI),
                new ComparisonTable("DI1_22","00122",TestPlcChannelType.DI),
                new ComparisonTable("DI1_23","00123",TestPlcChannelType.DI),
                new ComparisonTable("DI1_24","00124",TestPlcChannelType.DI),
                new ComparisonTable("DI1_25","00125",TestPlcChannelType.DI),
                new ComparisonTable("DI1_26","00126",TestPlcChannelType.DI),
                new ComparisonTable("DI1_27","00127",TestPlcChannelType.DI),
                new ComparisonTable("DI1_28","00128",TestPlcChannelType.DI),

                new ComparisonTable("DO1_1","00131",TestPlcChannelType.DO),
                new ComparisonTable("DO1_2","00132",TestPlcChannelType.DO),
                new ComparisonTable("DO1_3","00133",TestPlcChannelType.DO),
                new ComparisonTable("DO1_4","00134",TestPlcChannelType.DO),
                new ComparisonTable("DO1_5","00135",TestPlcChannelType.DO),
                new ComparisonTable("DO1_6","00136",TestPlcChannelType.DO),
                new ComparisonTable("DO1_7","00137",TestPlcChannelType.DO),
                new ComparisonTable("DO1_8","00138",TestPlcChannelType.DO),
                new ComparisonTable("DO1_9","00139",TestPlcChannelType.DO),
                new ComparisonTable("DO1_10","00140",TestPlcChannelType.DO),
                new ComparisonTable("DO1_11","00141",TestPlcChannelType.DO),
                new ComparisonTable("DO1_12","00142",TestPlcChannelType.DO),
                new ComparisonTable("DO1_13","00143",TestPlcChannelType.DO),
                new ComparisonTable("DO1_14","00144",TestPlcChannelType.DO),
                new ComparisonTable("DO1_15","00145",TestPlcChannelType.DO),
                new ComparisonTable("DO1_16","00146",TestPlcChannelType.DO),
                new ComparisonTable("DO1_17","00147",TestPlcChannelType.DO),
                new ComparisonTable("DO1_18","00148",TestPlcChannelType.DO),
                new ComparisonTable("DO1_19","00149",TestPlcChannelType.DO),
                new ComparisonTable("DO1_20","00150",TestPlcChannelType.DO),
            };
            return result;
        }
    }
}
