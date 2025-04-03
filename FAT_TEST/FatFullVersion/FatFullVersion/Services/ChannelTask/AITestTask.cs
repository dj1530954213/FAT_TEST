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
                float minValue = ChannelMapping.RangeLowerLimitValue;
                float maxValue = ChannelMapping.RangeUpperLimitValue;
                float range = maxValue - minValue;

                // 依次测试不同百分比的信号值
                float[] percentages = { 0, 25, 50, 75, 100 };
                
                bool allTestsPassed = true;
                // 测试前清除原来的测试记录
                Result.Status = "";
                // 保存详细的测试过程记录
                StringBuilder detailedTestLog = new StringBuilder();
                
                for (int i = 0; i < percentages.Length; i++)
                {
                    var percentage = percentages[i];
                    
                    // 取消检查
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    // 暂停检查
                    await CheckAndWaitForResumeAsync(cancellationToken);
                    
                    // 计算当前测试值
                    float testValue = minValue + (range * percentage / 100);
                    
                    // 写入测试值到测试PLC
                    var writeResult = await TestPlcCommunication.WriteAnalogValueAsync(ChannelMapping.TestPLCCommunicationAddress.Substring(1), testValue);
                    if (!writeResult.IsSuccess)
                    {
                        detailedTestLog.AppendLine($"写入测试值失败：{writeResult.ErrorMessage}");
                        allTestsPassed = false;
                        break;
                    }
                    
                    // 等待信号稳定(大约3秒)
                    await Task.Delay(3000, cancellationToken);
                    
                    // 读取被测PLC的值
                    var readResult = await TargetPlcCommunication.ReadAnalogValueAsync(ChannelMapping.PlcCommunicationAddress.Substring(1));
                    if (!readResult.IsSuccess)
                    {
                        detailedTestLog.AppendLine($"读取被测PLC值失败：{readResult.ErrorMessage}");
                        allTestsPassed = false;
                        break;
                    }
                    
                    float actualValue = readResult.Data;
                    
                    // 存储各个百分比点位的值
                    switch (percentage)
                    {
                        case 0:
                            Result.Value0Percent = actualValue;
                            Console.WriteLine($"存储0%值: {actualValue}");
                            break;
                        case 25:
                            Result.Value25Percent = actualValue;
                            Console.WriteLine($"存储25%值: {actualValue}");
                            break;
                        case 50:
                            Result.Value50Percent = actualValue;
                            Console.WriteLine($"存储50%值: {actualValue}");
                            break;
                        case 75:
                            Result.Value75Percent = actualValue;
                            Console.WriteLine($"存储75%值: {actualValue}");
                            break;
                        case 100:
                            Result.Value100Percent = actualValue;
                            Console.WriteLine($"存储100%值: {actualValue}");
                            break;
                    }
                    
                    // 更新测试结果
                    Result.ExpectedValue = testValue;
                    Result.ActualValue = actualValue;
                    
                    // 计算偏差是否在容许范围内
                    float deviation = Math.Abs(actualValue - testValue);
                    float deviationPercent = (testValue != 0) ? (deviation / Math.Abs(testValue)) * 100 : 0;
                    
                    // 根据偏差判断是否通过测试
                    // 假设允许偏差为1%
                    const float allowedDeviation = 1.0f;
                    
                    if (deviationPercent <= allowedDeviation)
                    {
                        detailedTestLog.AppendLine($"{percentage}%测试通过");
                    }
                    else
                    {
                        detailedTestLog.AppendLine($"{percentage}%测试失败：偏差{deviationPercent:F2}%超出范围");
                        allTestsPassed = false;
                        // 不中断测试流程，继续测试其它点位
                    }
                    
                    // 短暂延时再进行下一个测试点
                    await Task.Delay(1000, cancellationToken);
                }
                
                // 确保百分比测试值能够持久化到通道映射中
                ChannelMapping.Value0Percent = Result.Value0Percent;
                ChannelMapping.Value25Percent = Result.Value25Percent;
                ChannelMapping.Value50Percent = Result.Value50Percent;
                ChannelMapping.Value75Percent = Result.Value75Percent;
                ChannelMapping.Value100Percent = Result.Value100Percent;
                
                // 保存详细日志到错误信息字段，便于查看
                Result.ErrorMessage = detailedTestLog.ToString();
                
                // 设置最终测试状态 - 只显示通过或失败
                if (allTestsPassed)
                {
                    Result.Status = "通过";
                    ChannelMapping.HardPointTestResult = "通过";
                }
                else
                {
                    Result.Status = "失败";
                    ChannelMapping.HardPointTestResult = "失败";
                    ChannelMapping.TestResultStatus = 2;
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
                ChannelMapping.HardPointTestResult = "失败";
                throw;
            }
            finally
            {
                // 结束测试时，将测试PLC输出复位到0%
                try
                {
                    var resetResult = await TestPlcCommunication.WriteAnalogValueAsync(ChannelMapping.TestPLCCommunicationAddress.Substring(1), ChannelMapping.RangeLowerLimitValue);
                    if (!resetResult.IsSuccess)
                    {
                        // 记录复位失败但不影响测试结果
                        if (string.IsNullOrEmpty(Result.ErrorMessage))
                            Result.ErrorMessage = $"复位失败：{resetResult.ErrorMessage}";
                        else
                            Result.ErrorMessage += $"\n复位失败：{resetResult.ErrorMessage}";
                    }
                }
                catch
                {
                    // 忽略复位过程中的异常
                }
            }
        }
    }
} 