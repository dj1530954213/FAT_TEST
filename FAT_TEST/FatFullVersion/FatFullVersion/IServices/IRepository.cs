using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FatFullVersion.Entities;
using FatFullVersion.Entities.ValueObject;

namespace FatFullVersion.IServices
{
    /// <summary>
    /// 仓储层接口，提供数据持久化服务
    /// </summary>
    public interface IRepository
    {
        /// <summary>
        /// 初始化数据库
        /// </summary>
        /// <returns>初始化是否成功</returns>
        Task<bool> InitializeDatabaseAsync();

        #region PLC连接配置操作

        /// <summary>
        /// 获取测试PLC连接配置
        /// </summary>
        /// <returns>PLC连接配置</returns>
        Task<PlcConnectionConfig> GetTestPlcConnectionConfigAsync();

        /// <summary>
        /// 获取被测PLC连接配置
        /// </summary>
        /// <returns>PLC连接配置</returns>
        Task<PlcConnectionConfig> GetTargetPlcConnectionConfigAsync();

        /// <summary>
        /// 保存PLC连接配置
        /// </summary>
        /// <param name="config">PLC连接配置</param>
        /// <returns>保存操作是否成功</returns>
        Task<bool> SavePlcConnectionConfigAsync(PlcConnectionConfig config);

        /// <summary>
        /// 获取所有PLC连接配置
        /// </summary>
        /// <returns>PLC连接配置列表</returns>
        Task<List<PlcConnectionConfig>> GetAllPlcConnectionConfigsAsync();

        #endregion

        #region 通道比较表操作

        /// <summary>
        /// 通过通道位号获取通讯地址
        /// </summary>
        /// <param name="channelTag">通道位号</param>
        /// <returns>通讯地址</returns>
        Task<string> GetPlcCommunicationAddress(string channelTag);

        /// <summary>
        /// 获取所有测试PLC的通道与通讯地址的对应关系
        /// </summary>
        /// <returns>通道比较表列表</returns>
        Task<List<ComparisonTable>> GetComparisonTablesAsync();

        /// <summary>
        /// 添加通道比较表项
        /// </summary>
        /// <param name="comparisonTable">通道比较表项</param>
        /// <returns>添加操作是否成功</returns>
        Task<bool> AddComparisonTableAsync(ComparisonTable comparisonTable);

        /// <summary>
        /// 添加多个通道比较表项
        /// </summary>
        /// <param name="comparisonTables">通道比较表项列表</param>
        /// <returns>添加操作是否成功</returns>
        Task<bool> AddComparisonTablesAsync(List<ComparisonTable> comparisonTables);

        /// <summary>
        /// 更新通道比较表项
        /// </summary>
        /// <param name="comparisonTable">通道比较表项</param>
        /// <returns>更新操作是否成功</returns>
        Task<bool> UpdateComparisonTableAsync(ComparisonTable comparisonTable);

        /// <summary>
        /// 删除通道比较表项
        /// </summary>
        /// <param name="id">通道比较表项ID</param>
        /// <returns>删除操作是否成功</returns>
        Task<bool> DeleteComparisonTableAsync(int id);

        /// <summary>
        /// 保存所有通道比较表项（删除旧数据并添加新数据）
        /// </summary>
        /// <param name="comparisonTables">通道比较表项列表</param>
        /// <returns>保存操作是否成功</returns>
        Task<bool> SaveAllComparisonTablesAsync(List<ComparisonTable> comparisonTables);

        #endregion
    }
}
