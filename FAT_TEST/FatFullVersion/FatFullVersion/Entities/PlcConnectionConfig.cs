using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FatFullVersion.Entities
{
    /// <summary>
    /// PLC连接配置
    /// </summary>
    public class PlcConnectionConfig
    {
        /// <summary>
        /// PLC的IP地址
        /// </summary>
        public string IpAddress { get; set; } = "127.0.0.1";

        /// <summary>
        /// PLC的端口号
        /// </summary>
        public int Port { get; set; } = 502;

        /// <summary>
        /// PLC站号
        /// </summary>
        public byte Station { get; set; } = 1;

        /// <summary>
        /// 地址是否从0开始
        /// </summary>
        public bool AddressStartWithZero { get; set; } = false;

        /// <summary>
        /// 是否检查消息ID
        /// </summary>
        public bool IsCheckMessageId { get; set; } = true;

        /// <summary>
        /// 字符串是否需要反转
        /// </summary>
        public bool IsStringReverse { get; set; } = false;

        /// <summary>
        /// 数据格式
        /// </summary>
        public string DataFormat { get; set; } = "ABCD";

        /// <summary>
        /// 连接超时时间（毫秒）
        /// </summary>
        public int ConnectTimeOut { get; set; } = 5000;

        /// <summary>
        /// 接收超时时间（毫秒）
        /// </summary>
        public int ReceiveTimeOut { get; set; } = 10000;

        /// <summary>
        /// 连接之间的休眠时间（毫秒）
        /// </summary>
        public int SleepTime { get; set; } = 0;

        /// <summary>
        /// Socket保持连接时间（-1表示不限制）
        /// </summary>
        public int SocketKeepAliveTime { get; set; } = -1;

        /// <summary>
        /// 是否使用持久连接
        /// </summary>
        public bool IsPersistentConnection { get; set; } = true;
    }
}