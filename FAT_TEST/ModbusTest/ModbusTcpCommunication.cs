using System;
using DCMCAJ;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FatFullVersion.IServices;

namespace ModbusTest
{
    public class ModbusTcpCommunication
    {
        private DCMCAJ.ModBus.ModbusTcpNet modbus;
        public ModbusTcpCommunication()
        {
            modbus = new DCMCAJ.ModBus.ModbusTcpNet();
        }
        public async Task<bool> ConnectAsync()
        {
            modbus.Station = 1;
            modbus.AddressStartWithZero = false;
            modbus.IsCheckMessageId = true;
            modbus.IsStringReverse = false;
            modbus.DataFormat = DCMCAJ.Core.DataFormat.CDAB;
            modbus.CommunicationPipe = new DCMCAJ.Core.Pipe.PipeTcpNet("127.0.0.1", 502)
            {
                ConnectTimeOut = 5000,    // 连接超时时间，单位毫秒
                ReceiveTimeOut = 10000,    // 接收设备数据反馈的超时时间
                SleepTime = 0,
                SocketKeepAliveTime = -1,
                IsPersistentConnection = true,
            };
            try
            {
                var result = await modbus.ConnectServerAsync();
                if (result.IsSuccess)
                {
                    IsConnected = true;
                    return true;
                }
                else
                {
                    IsConnected = false;
                    return false;
                }
            }
            catch (Exception e)
            {
                throw;
            }
        }

        public async Task<bool> DisconnectAsync()
        {
            try
            {
                if (IsConnected)
                {
                    var result = await modbus.ConnectCloseAsync();
                    if (result.IsSuccess)
                    {
                        IsConnected = true;
                        return true;
                    }
                    else
                    {
                        IsConnected = false;
                        return false;

                    }
                }
                else
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public async Task<float> ReadAnalogValueAsync(string address)
        {
            var result = await modbus.ReadFloatAsync(address);
            if (result.IsSuccess)
            {
                return result.Content;
            }
            else
            {
                throw new Exception("读取失败");
            }
        }

        public async Task<bool> WriteAnalogValueAsync(string address, float value)
        {
            var result = await modbus.WriteAsync(address,value);
            if (result.IsSuccess)
            {
                return true;
            }
            else
            {
                throw new Exception("写入失败");
            }
        }

        public async Task<bool> ReadDigitalValueAsync(string address)
        {
            var result = await modbus.ReadBoolAsync(address);
            if (result.IsSuccess)
            {
                return result.Content;
            }
            else
            {
                throw new Exception("读取失败");
            }
        }

        public async Task<bool> WriteDigitalValueAsync(string address, bool value)
        {
            var result = await modbus.WriteAsync(address, value);
            if (result.IsSuccess)
            {
                return true;
            }
            else
            {
                throw new Exception("写入失败");
            }
        }

        public bool IsConnected { get; set; }
        public string GetPlcInfo()
        {
            throw new NotImplementedException();
        }
    }
}
