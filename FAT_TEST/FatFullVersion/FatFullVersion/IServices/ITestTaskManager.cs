using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FatFullVersion.Models;

namespace FatFullVersion.IServices
{
    /// <summary>
    /// 测试任务管理器接口，定义测试任务的创建、启动、停止和管理功能
    /// </summary>
    public interface ITestTaskManager : IDisposable
    {
        /// <summary>
        /// 从通道映射集合创建测试任务
        /// </summary>
        /// <param name="channelMappings">需要测试的通道映射集合</param>
        /// <returns>创建的任务ID列表</returns>
        Task<IEnumerable<string>> CreateTestTasksAsync(IEnumerable<ChannelMapping> channelMappings);

        /// <summary>
        /// 启动所有测试任务
        /// </summary>
        /// <returns>操作是否成功</returns>
        Task<bool> StartAllTasksAsync();

        /// <summary>
        /// 停止所有测试任务
        /// </summary>
        /// <returns>操作是否成功</returns>
        Task<bool> StopAllTasksAsync();

        /// <summary>
        /// 暂停所有测试任务
        /// </summary>
        /// <returns>操作是否成功</returns>
        Task<bool> PauseAllTasksAsync();

        /// <summary>
        /// 恢复所有测试任务
        /// </summary>
        /// <returns>操作是否成功</returns>
        Task<bool> ResumeAllTasksAsync();

        /// <summary>
        /// 根据ID获取测试任务
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <returns>测试任务实例，如果不存在则返回null</returns>
        TestTask GetTaskById(string taskId);

        /// <summary>
        /// 根据通道映射获取测试任务
        /// </summary>
        /// <param name="channelMapping">通道映射实例</param>
        /// <returns>测试任务实例，如果不存在则返回null</returns>
        TestTask GetTaskByChannel(ChannelMapping channelMapping);

        /// <summary>
        /// 获取所有活跃的测试任务
        /// </summary>
        /// <returns>所有活跃的测试任务集合</returns>
        IEnumerable<TestTask> GetAllTasks();

        /// <summary>
        /// 删除特定ID的测试任务
        /// </summary>
        /// <param name="taskId">待删除的任务ID</param>
        /// <returns>操作是否成功</returns>
        Task<bool> RemoveTaskAsync(string taskId);

        /// <summary>
        /// 添加新的测试任务
        /// </summary>
        /// <param name="task">要添加的测试任务</param>
        /// <returns>操作是否成功</returns>
        bool AddTask(TestTask task);

        /// <summary>
        /// 清空所有测试任务
        /// </summary>
        /// <returns>操作是否成功</returns>
        Task<bool> ClearAllTasksAsync();
    }
}
