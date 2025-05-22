using System;
using FatFullVersion.Models;

namespace FatFullVersion.IServices
{
    /// <summary>
    /// Manual Test I/O Service
    /// 负责手动测试场景下与PLC进行数据交互，例如：
    /// 1. 周期性读取 AI/AO/DO/DI 等通道的反馈或设定值。
    /// 2. 发送数字/模拟量命令以驱动被测或测试设备。
    /// 本阶段仅实现 AI 报警设定值监控（SL/SLL/SH/SHH）。
    /// </summary>
    public interface IManualTestIoService
    {
        /// <summary>
        /// 启动对指定 AI 通道报警设定值的监控，周期 0.5s。
        /// </summary>
        /// <param name="channel">目标通道</param>
        /// <param name="updateAction">读取结果回调 (SL, SLL, SH, SHH)</param>
        void StartAlarmValueMonitoring(ChannelMapping channel, Action<float?, float?, float?, float?> updateAction);

        /// <summary>
        /// 停止所有监控/交互任务。
        /// </summary>
        void StopAll();
    }
} 