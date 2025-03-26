using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FatFullVersion.Entities;

namespace FatFullVersion.IServices
{
    /// <summary>
    /// 仓储层接口，提供数据持久化服务
    /// </summary>
    public interface IRepository
    {
        /// <summary>
        /// 获取PLC连接配置
        /// </summary>
        /// <returns>PLC连接配置</returns>
        Task<PlcConnectionConfig> GetPlcConnectionConfigAsync();

        /// <summary>
        /// 保存PLC连接配置
        /// </summary>
        /// <param name="config">PLC连接配置</param>
        /// <returns>保存操作是否成功</returns>
        Task<bool> SavePlcConnectionConfigAsync(PlcConnectionConfig config);
    }
}
