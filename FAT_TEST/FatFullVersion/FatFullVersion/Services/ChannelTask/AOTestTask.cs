using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FatFullVersion.IServices;
using FatFullVersion.Models;

namespace FatFullVersion.Services.ChannelTask
{
    /// <summary>
    /// AO测试任务实现类
    /// </summary>
    public class AOTestTask : TestTask
    {
        /// <summary>
        /// 创建AO测试任务实例
        /// </summary>
        /// <param name="id">任务ID</param>
        /// <param name="channelMapping">通道映射信息</param>
        /// <param name="testPlcCommunication">测试PLC通信实例</param>
        /// <param name="targetPlcCommunication">被测PLC通信实例</param>
        public AOTestTask(
            string id,
            ChannelMapping channelMapping,
            IPlcCommunication testPlcCommunication,
            IPlcCommunication targetPlcCommunication)
            : base(id, channelMapping, testPlcCommunication, targetPlcCommunication)
        {
        }

        /// <summary>
        /// 执行AO测试逻辑
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
                // 实现AO测试逻辑
                // 1. 向被测PLC的AO发送不同输出值
                // 2. 测试PLC读取该模拟量并检验是否在允许范围

                // 定义测试信号值（根据工程单位和量程计算）
                float minValue = ChannelMapping.LowLowLimit;
                float maxValue = ChannelMapping.HighHighLimit;
                float range = maxValue - minValue;

                // 依次测试不同百分比的信号值
                float[] percentages = { 0, 25, 50, 75, 100 };
                //测试前清除原有记录
                Result.Status = "";
                bool allTestsPassed = true;
                
                // 创建详细测试日志
                StringBuilder detailedTestLog = new StringBuilder();
                
                for (int i = 0; i < percentages.Length; i++)
                {
                    float percentage = percentages[i];
                    
                    // 取消检查
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    // 暂停检查
                    await CheckAndWaitForResumeAsync(cancellationToken);
                    
                    // 计算当前测试值
                    float testValue = minValue + (range * percentage / 100);
                    
                    // 向被测PLC发送AO值
                    var writeResult = await TargetPlcCommunication.WriteAnalogValueAsync(
                        ChannelMapping.PlcCommunicationAddress.Substring(1), 
                        testValue);
                        
                    if (!writeResult.IsSuccess)
                    {
                        detailedTestLog.AppendLine($"写入被测PLC失败: {writeResult.ErrorMessage}");
                        allTestsPassed = false;
                        break;
                    }
                    
                    // 等待信号稳定
                    await Task.Delay(3000, cancellationToken);
                    
                    // 测试PLC读取该值
                    var readResult = await TestPlcCommunication.ReadAnalogValueAsync(
                        ChannelMapping.TestPLCCommunicationAddress.Substring(1));
                        
                    if (!readResult.IsSuccess)
                    {
                        detailedTestLog.AppendLine($"读取测试PLC失败: {readResult.ErrorMessage}");
                        allTestsPassed = false;
                        break;
                    }
                    
                    float actualValue = readResult.Data;
                    
                    // 存储各个百分比点位的值
                    switch (percentage)
                    {
                        case 0:
                            Result.Value0Percent = actualValue;
                            Console.WriteLine($"AO存储0%值: {actualValue}");
                            break;
                        case 25:
                            Result.Value25Percent = actualValue;
                            Console.WriteLine($"AO存储25%值: {actualValue}");
                            break;
                        case 50:
                            Result.Value50Percent = actualValue;
                            Console.WriteLine($"AO存储50%值: {actualValue}");
                            break;
                        case 75:
                            Result.Value75Percent = actualValue;
                            Console.WriteLine($"AO存储75%值: {actualValue}");
                            break;
                        case 100:
                            Result.Value100Percent = actualValue;
                            Console.WriteLine($"AO存储100%值: {actualValue}");
                            break;
                    }
                    
                    // 更新测试结果
                    Result.ExpectedValue = testValue;
                    Result.ActualValue = actualValue;
                    
                    // 计算偏差
                    float deviation = Math.Abs(actualValue - testValue);
                    float deviationPercent = (testValue != 0) ? (deviation / Math.Abs(testValue)) * 100 : 0;
                    
                    // 检查偏差是否在允许范围内
                    const float allowedDeviation = 1.0f; // 1%的允许偏差
                    
                    if (deviationPercent <= allowedDeviation)
                    {
                        detailedTestLog.AppendLine($"{percentage}%测试通过");
                    }
                    else
                    {
                        detailedTestLog.AppendLine($"{percentage}%测试失败: 偏差{deviationPercent:F2}%超出范围");
                        allTestsPassed = false;
                        // 不中断测试，继续测试其它百分比点
                    }
                    
                    // 短暂延时
                    await Task.Delay(1000, cancellationToken);
                }
                
                // 确保百分比测试值能够持久化到通道映射中
                ChannelMapping.Value0Percent = Result.Value0Percent;
                ChannelMapping.Value25Percent = Result.Value25Percent;
                ChannelMapping.Value50Percent = Result.Value50Percent;
                ChannelMapping.Value75Percent = Result.Value75Percent;
                ChannelMapping.Value100Percent = Result.Value100Percent;
                
                // 保存详细测试日志
                Result.ErrorMessage = detailedTestLog.ToString();
                
                // 设置最终测试状态
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
                // 任务被取消
                throw;
            }
            catch (Exception ex)
            {
                // 其他异常
                Result.Status = "失败";
                Result.ErrorMessage = ex.Message;
                ChannelMapping.HardPointTestResult = "失败";
                throw;
            }
            finally
            {
                // 将被测PLC AO复位到0%
                try
                {
                    await TargetPlcCommunication.WriteAnalogValueAsync(
                        ChannelMapping.PlcCommunicationAddress.Substring(1), 
                        ChannelMapping.LowLowLimit);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"复位AO通道失败: {ex.Message}");
                    if (string.IsNullOrEmpty(Result.ErrorMessage))
                        Result.ErrorMessage = $"复位失败: {ex.Message}";
                    else
                        Result.ErrorMessage += $"\n复位失败: {ex.Message}";
                }
            }
        }
    }
}