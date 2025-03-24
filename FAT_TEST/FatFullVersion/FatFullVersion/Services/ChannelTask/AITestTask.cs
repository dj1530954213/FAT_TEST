using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FatFullVersion.IServices;

namespace FatFullVersion.Models
{
    /// <summary>
    /// AI测试任务实现类
    /// </summary>
    public class AITestTask : TestTask
    {
        /// <summary>
        /// 创建AI测试任务实例
        /// </summary>
        /// <param name="id">任务ID</param>
        /// <param name="channelMapping">通道映射信息</param>
        /// <param name="testPlcCommunication">测试PLC通信实例</param>
        /// <param name="targetPlcCommunication">被测PLC通信实例</param>
        public AITestTask(
            string id,
            ChannelMapping channelMapping,
            IPlcCommunication testPlcCommunication,
            IPlcCommunication targetPlcCommunication)
            : base(id, channelMapping, testPlcCommunication, targetPlcCommunication)
        {
        }

        /// <summary>
        /// 执行AI测试逻辑
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>异步任务</returns>
        protected override async Task ExecuteTestAsync(CancellationToken cancellationToken)
        {
            // 确保连接已建立
            if (!TestPlcCommunication.IsConnected)
            {
                await TestPlcCommunication.ConnectAsync();
            }

            if (!TargetPlcCommunication.IsConnected)
            {
                await TargetPlcCommunication.ConnectAsync();
            }

            try
            {
                // AI测试流程：由测试PLC输出模拟量信号，然后检查被测PLC是否正确接收
                // 测试多个不同的信号值（0%、25%、50%、75%、100%等）

                // 定义测试信号值（根据工程单位和量程计算）
                double minValue = ChannelMapping.LowLowLimit;
                double maxValue = ChannelMapping.HighHighLimit;
                double range = maxValue - minValue;

                // 依次测试不同百分比的信号值
                double[] percentages = { 0, 25, 50, 75, 100 };
                
                foreach (var percentage in percentages)
                {
                    // 取消检查
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    // 暂停检查
                    await CheckAndWaitForResumeAsync(cancellationToken);
                    
                    // 计算当前测试值
                    double testValue = minValue + (range * percentage / 100);
                    
                    // 写入测试值到测试PLC
                    await TestPlcCommunication.WriteAnalogValueAsync(ChannelMapping.TestPLCCommunicationAddress, testValue);
                    
                    // 等待信号稳定(大约3秒)
                    await Task.Delay(3000, cancellationToken);
                    
                    // 读取被测PLC的值
                    double actualValue = await TargetPlcCommunication.ReadAnalogValueAsync(ChannelMapping.VariableName);
                    
                    // 更新测试结果
                    Result.ExpectedValue = testValue;
                    Result.ActualValue = actualValue;
                    
                    // 计算偏差是否在容许范围内
                    double deviation = Math.Abs(actualValue - testValue);
                    double deviationPercent = (testValue != 0) ? (deviation / Math.Abs(testValue)) * 100 : 0;
                    
                    // 根据偏差判断是否通过测试
                    // 假设允许偏差为1%
                    const double allowedDeviation = 1.0;
                    
                    if (deviationPercent <= allowedDeviation)
                    {
                        Result.Status = $"{percentage}%测试通过";
                    }
                    else
                    {
                        Result.Status = $"{percentage}%测试失败：偏差{deviationPercent:F2}%超出范围";
                        break; // 如果测试失败，则结束后续测试
                    }
                    
                    // 短暂延时再进行下一个测试点
                    await Task.Delay(1000, cancellationToken);
                }
                
                // 所有测试点通过后，将最终状态设置为通过
                if (Result.Status.Contains("通过"))
                {
                    Result.Status = "通过";
                }
            }
            catch (OperationCanceledException)
            {
                // 任务被取消，不做特殊处理，直接向上抛出
                throw;
            }
            catch (Exception ex)
            {
                // 其他异常，记录错误消息
                Result.Status = "失败";
                Result.ErrorMessage = ex.Message;
                throw;
            }
            finally
            {
                // 结束测试时，将测试PLC输出复位到0%
                try
                {
                    await TestPlcCommunication.WriteAnalogValueAsync(ChannelMapping.TestPLCCommunicationAddress, ChannelMapping.LowLowLimit);
                }
                catch
                {
                    // 忽略复位过程中的异常
                }
            }
        }
    }
} 