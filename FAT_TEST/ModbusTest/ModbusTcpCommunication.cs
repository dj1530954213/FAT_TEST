using System;
using DCMCAJ;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FatFullVersion.IServices;

namespace ModbusTest
{
    public class ModbusTcpCommunication:IPlcCommunication
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
            modbus.DataFormat = DCMCAJ.Core.DataFormat.ABCD;
            //modbus.BroadcastStation = -1;
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

        public Task<double> ReadAnalogValueAsync(string address)
        {
            throw new NotImplementedException();
        }

        public Task<bool> WriteAnalogValueAsync(string address, double value)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ReadDigitalValueAsync(string address)
        {
            throw new NotImplementedException();
        }

        public Task<bool> WriteDigitalValueAsync(string address, bool value)
        {
            throw new NotImplementedException();
        }

        public bool IsConnected { get; set; }
        public string GetPlcInfo()
        {
            throw new NotImplementedException();
        }
    }
}
