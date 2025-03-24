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
    /// DO测试任务实现类
    /// </summary>
    public class DOTestTask : TestTask
    {
        /// <summary>
        /// 创建DO测试任务实例
        /// </summary>
        /// <param name="id">任务ID</param>
        /// <param name="channelMapping">通道映射信息</param>
        /// <param name="testPlcCommunication">测试PLC通信实例</param>
        /// <param name="targetPlcCommunication">被测PLC通信实例</param>
        public DOTestTask(
            string id,
            ChannelMapping channelMapping,
            IPlcCommunication testPlcCommunication,
            IPlcCommunication targetPlcCommunication)
            : base(id, channelMapping, testPlcCommunication, targetPlcCommunication)
        {
        }

        /// <summary>
        /// 执行DO测试逻辑
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
                // DO测试流程：在被测PLC上设置输出状态，然后由测试PLC读取实际输出状态

                // 测试OFF状态
                // 取消检查
                cancellationToken.ThrowIfCancellationRequested();

                // 暂停检查
                await CheckAndWaitForResumeAsync(cancellationToken);

                // 写入OFF状态到被测PLC
                await TargetPlcCommunication.WriteDigitalValueAsync(ChannelMapping.VariableName, false);

                // 等待信号稳定(大约2秒)
                await Task.Delay(2000, cancellationToken);

                // 读取测试PLC的值
                bool actualOffValue = await TestPlcCommunication.ReadDigitalValueAsync(ChannelMapping.TestPLCCommunicationAddress);

                // 更新测试结果
                Result.ExpectedValue = 0; // OFF状态
                Result.ActualValue = actualOffValue ? 1 : 0;

                // 检查OFF状态是否正确
                bool offTestPassed = !actualOffValue; // 预期是false

                // 测试ON状态
                // 取消检查
                cancellationToken.ThrowIfCancellationRequested();

                // 暂停检查
                await CheckAndWaitForResumeAsync(cancellationToken);

                // 写入ON状态到被测PLC
                await TargetPlcCommunication.WriteDigitalValueAsync(ChannelMapping.VariableName, true);

                // 等待信号稳定(大约2秒)
                await Task.Delay(2000, cancellationToken);

                // 读取测试PLC的值
                bool actualOnValue = await TestPlcCommunication.ReadDigitalValueAsync(ChannelMapping.TestPLCCommunicationAddress);

                // 更新测试结果
                Result.ExpectedValue = 1; // ON状态
                Result.ActualValue = actualOnValue ? 1 : 0;

                // 检查ON状态是否正确
                bool onTestPassed = actualOnValue; // 预期是true

                // 根据测试结果更新状态
                if (offTestPassed && onTestPassed)
                {
                    Result.Status = "通过";
                }
                else if (!offTestPassed && onTestPassed)
                {
                    Result.Status = "OFF状态测试失败";
                }
                else if (offTestPassed && !onTestPassed)
                {
                    Result.Status = "ON状态测试失败";
                }
                else
                {
                    Result.Status = "OFF和ON状态测试均失败";
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
                // 结束测试时，将被测PLC输出复位到OFF
                try
                {
                    await TargetPlcCommunication.WriteDigitalValueAsync(ChannelMapping.VariableName, false);
                }
                catch
                {
                    // 忽略复位过程中的异常
                }
            }
        }
    }
}