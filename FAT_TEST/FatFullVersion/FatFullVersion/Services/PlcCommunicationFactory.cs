using System;
using FatFullVersion.Entities.EntitiesEnum;
using FatFullVersion.IServices;

namespace FatFullVersion.Services
{
    /// <summary>
    /// PLC通信工厂类，用于创建和管理不同类型的PLC通信实例
    /// </summary>
    public class PlcCommunicationFactory
    {
        private readonly IRepository _repository;
        private readonly PlcType _plcType;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="repository">仓储层接口</param>
        /// <param name="plcType">PLC类型</param>
        public PlcCommunicationFactory(IRepository repository, PlcType plcType)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _plcType = plcType;
        }

        /// <summary>
        /// 创建PLC通信实例
        /// </summary>
        /// <returns>PLC通信实例</returns>
        public IPlcCommunication CreatePlcCommunication()
        {
            return new ModbusTcpCommunication(_repository);
        }
    }
} 