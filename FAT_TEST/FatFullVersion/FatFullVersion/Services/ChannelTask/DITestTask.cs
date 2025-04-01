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
    /// DI测试任务实现类
    /// </summary>
    public class DITestTask : TestTask
    {
        /// <summary>
        /// 创建DI测试任务实例
        /// </summary>
        /// <param name="id">任务ID</param>
        /// <param name="channelMapping">通道映射信息</param>
        /// <param name="testPlcCommunication">测试PLC通信实例</param>
        /// <param name="targetPlcCommunication">被测PLC通信实例</param>
        public DITestTask(
            string id,
            ChannelMapping channelMapping,
            IPlcCommunication testPlcCommunication,
            IPlcCommunication targetPlcCommunication)
            : base(id, channelMapping, testPlcCommunication, targetPlcCommunication)
        {
        }

        /// <summary>
        /// 执行DI测试逻辑
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
                // DI测试：测试PLC设置开关量信号，然后检查被测PLC是否正确接收
                
                // 取消检查
                cancellationToken.ThrowIfCancellationRequested();
                    
                // 暂停检查
                await CheckAndWaitForResumeAsync(cancellationToken);
                
                bool allTestsPassed = true;
                Result.Status = "";
                
                // 创建详细测试日志
                StringBuilder detailedTestLog = new StringBuilder();
                
                // 测试信号为1（闭合/接通）
                var writeHighResult = await TestPlcCommunication.WriteDigitalValueAsync(
                    ChannelMapping.TestPLCCommunicationAddress.Substring(1), 
                    true);
                    
                if (!writeHighResult.IsSuccess)
                {
                    detailedTestLog.AppendLine($"写入高信号失败: {writeHighResult.ErrorMessage}");
                    allTestsPassed = false;
                }
                else
                {
                    // 等待信号稳定
                    await Task.Delay(2000, cancellationToken);
                    
                    // 读取被测PLC的值
                    var readHighResult = await TargetPlcCommunication.ReadDigitalValueAsync(
                        ChannelMapping.PlcCommunicationAddress.Substring(1));
                        
                    if (!readHighResult.IsSuccess)
                    {
                        detailedTestLog.AppendLine($"读取高信号失败: {readHighResult.ErrorMessage}");
                        allTestsPassed = false;
                    }
                    else
                    {
                        bool actualHighValue = readHighResult.Data;
                        
                        if (actualHighValue)
                        {
                            detailedTestLog.AppendLine("高信号测试通过");
                        }
                        else
                        {
                            detailedTestLog.AppendLine("高信号测试失败: 期望值为true，实际值为false");
                            allTestsPassed = false;
                        }
                        
                        // 更新测试结果
                        Result.ExpectedValue = 1;
                        Result.ActualValue = actualHighValue ? 1 : 0;
                    }
                }
                
                // 测试信号为0（断开）
                if (allTestsPassed)
                {
                    // 取消检查
                    cancellationToken.ThrowIfCancellationRequested();
                        
                    // 暂停检查
                    await CheckAndWaitForResumeAsync(cancellationToken);
                    
                    var writeLowResult = await TestPlcCommunication.WriteDigitalValueAsync(
                        ChannelMapping.TestPLCCommunicationAddress.Substring(1), 
                        false);
                        
                    if (!writeLowResult.IsSuccess)
                    {
                        detailedTestLog.AppendLine($"写入低信号失败: {writeLowResult.ErrorMessage}");
                        allTestsPassed = false;
                    }
                    else
                    {
                        // 等待信号稳定
                        await Task.Delay(2000, cancellationToken);
                        
                        // 读取被测PLC的值
                        var readLowResult = await TargetPlcCommunication.ReadDigitalValueAsync(
                            ChannelMapping.PlcCommunicationAddress.Substring(1));
                            
                        if (!readLowResult.IsSuccess)
                        {
                            detailedTestLog.AppendLine($"读取低信号失败: {readLowResult.ErrorMessage}");
                            allTestsPassed = false;
                        }
                        else
                        {
                            bool actualLowValue = readLowResult.Data;
                            
                            if (!actualLowValue)
                            {
                                detailedTestLog.AppendLine("低信号测试通过");
                            }
                            else
                            {
                                detailedTestLog.AppendLine("低信号测试失败: 期望值为false，实际值为true");
                                allTestsPassed = false;
                            }
                            
                            // 更新测试结果
                            Result.ExpectedValue = 0;
                            Result.ActualValue = actualLowValue ? 1 : 0;
                        }
                    }
                }
                
                // 保存详细测试日志
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
                // 测试完成后，将测试信号复位为0
                try
                {
                    await TestPlcCommunication.WriteDigitalValueAsync(
                        ChannelMapping.TestPLCCommunicationAddress.Substring(1), 
                        false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"复位DI通道失败: {ex.Message}");
                    if (string.IsNullOrEmpty(Result.ErrorMessage))
                        Result.ErrorMessage = $"复位失败: {ex.Message}";
                    else
                        Result.ErrorMessage += $"\n复位失败: {ex.Message}";
                }
            }
        }
    }
}