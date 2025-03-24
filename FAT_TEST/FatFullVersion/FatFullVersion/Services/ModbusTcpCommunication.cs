using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FatFullVersion.IServices;

namespace FatFullVersion.Services
{
    public class ModbusTcpCommunication:IPlcCommunication
    {
        private readonly IRepository _repository;

        public ModbusTcpCommunication(IRepository repository)
        {
            _repository = repository;
        }
        public Task<bool> ConnectAsync()
        {
            throw new NotImplementedException();
        }

        public Task<bool> DisconnectAsync()
        {
            throw new NotImplementedException();
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

        public bool IsConnected { get; }
        public string GetPlcInfo()
        {
            throw new NotImplementedException();
        }
    }
}
