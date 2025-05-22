using System;
using System.Threading;
using System.Threading.Tasks;
using FatFullVersion.IServices;
using FatFullVersion.Models;
using FatFullVersion.Services.Interfaces;

namespace FatFullVersion.Services
{
    /// <summary>
    /// ManualTestIoService 实现周期性 PLC 读写，用于手动测试阶段的数据交互。
    /// 当前仅实现 AI 报警设定值监控功能。
    /// </summary>
    public class ManualTestIoService : IManualTestIoService
    {
        private readonly IPlcCommunication _plc;
        private CancellationTokenSource _cts;
        private Task _monitorTask;
        private ChannelMapping _channel;
        private Action<float?, float?, float?, float?> _updateAction;

        public ManualTestIoService(IPlcCommunication targetPlc)
        {
            _plc = targetPlc ?? throw new ArgumentNullException(nameof(targetPlc));
        }

        public void StartAlarmValueMonitoring(ChannelMapping channel, Action<float?, float?, float?, float?> updateAction)
        {
            StopAll();
            if (channel == null || updateAction == null) return;

            _channel = channel;
            _updateAction = updateAction;
            _cts = new CancellationTokenSource();
            _monitorTask = Task.Run(() => MonitorLoopAsync(_cts.Token));
        }

        public void StopAll()
        {
            if (_cts != null)
            {
                try { _cts.Cancel(); } catch { }
                _cts = null;
            }
        }

        private async Task MonitorLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    float? sl  = await ReadAnalogAsync(_channel?.SLSetPointCommAddress);
                    float? sll = await ReadAnalogAsync(_channel?.SLLSetPointCommAddress);
                    float? sh  = await ReadAnalogAsync(_channel?.SHSetPointCommAddress);
                    float? shh = await ReadAnalogAsync(_channel?.SHHSetPointCommAddress);

                    _updateAction?.Invoke(sl, sll, sh, shh);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ManualTestIoService Error: {ex.Message}");
                }
                await Task.Delay(500, token).ContinueWith(_ => { });
            }
        }

        private async Task<float?> ReadAnalogAsync(string address)
        {
            try
            {
                if (string.IsNullOrEmpty(address)) return null;
                var result = await _plc.ReadAnalogValueAsync(address.Substring(1));
                return result.IsSuccess ? result.Data : (float?)null;
            }
            catch
            {
                return null;
            }
        }
    }
} 