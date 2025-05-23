using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using FatFullVersion.IServices;
using FatFullVersion.Models;
using FatFullVersion.Shared;

namespace FatFullVersion.Services
{
    /// <summary>
    /// 测试记录服务实现类
    /// 负责测试记录的保存、恢复和管理
    /// </summary>
    public class TestRecordService : ITestRecordService
    {
        private readonly IRepository _repository;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="repository">数据仓储服务</param>
        public TestRecordService(IRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        /// <summary>
        /// 保存测试记录 - 通用方法
        /// </summary>
        /// <param name="channelMappings">通道映射数据集合</param>
        /// <param name="testTag">测试标识，如果为null则使用测试记录中的标识</param>
        /// <returns>操作是否成功</returns>
        public async Task<bool> SaveTestRecordsAsync(IEnumerable<ChannelMapping> channelMappings, string testTag = null)
        {
            try
            {
                // 没有记录时返回成功
                if (channelMappings == null || !channelMappings.Any())
                    return true;

                // 如果提供了测试标识，为所有记录设置统一的标识
                var records = channelMappings.ToList();
                if (!string.IsNullOrEmpty(testTag))
                {
                    foreach (var record in records)
                    {
                        record.TestTag = testTag;
                    }
                }

                // 使用优化后的EF Core方法保存记录
                return await _repository.SaveTestRecordsAsync(records);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存测试记录时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// 保存单个测试记录 - 手动测试场景
        /// </summary>
        /// <param name="channelMapping">通道映射数据</param>
        /// <returns>操作是否成功</returns>
        public async Task<bool> SaveTestRecordAsync(ChannelMapping channelMapping)
        {
            try
            {
                if (channelMapping == null)
                    return false;

                return await _repository.SaveTestRecordAsync(channelMapping);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存单个测试记录时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// 批量保存硬点自动测试完成的记录 - 新增优化方法
        /// </summary>
        /// <param name="channelMappings">通道映射数据集合</param>
        /// <param name="testTag">测试标识</param>
        /// <returns>操作是否成功</returns>
        public async Task<bool> SaveHardPointTestResultsAsync(IEnumerable<ChannelMapping> channelMappings, string testTag = null)
        {
            try
            {
                if (channelMappings == null || !channelMappings.Any())
                    return true;

                var records = channelMappings.ToList();
                if (!string.IsNullOrEmpty(testTag))
                {
                    foreach (var record in records)
                    {
                        record.TestTag = testTag;
                    }
                }

                return await _repository.SaveHardPointTestResultsAsync(records);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"批量保存硬点测试结果时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// 更新单个通道的复测结果 - 复测场景优化
        /// </summary>
        /// <param name="channelMapping">通道映射数据</param>
        /// <returns>操作是否成功</returns>
        public async Task<bool> UpdateRetestResultAsync(ChannelMapping channelMapping)
        {
            try
            {
                if (channelMapping == null)
                    return false;

                return await _repository.UpdateRetestResultAsync(channelMapping);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"更新复测结果时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// 恢复指定测试标识的测试记录
        /// </summary>
        /// <param name="testTag">测试标识</param>
        /// <returns>恢复的测试记录集合</returns>
        public async Task<List<ChannelMapping>> RestoreTestRecordsAsync(string testTag)
        {
            try
            {
                if (string.IsNullOrEmpty(testTag))
                    return new List<ChannelMapping>();

                return await _repository.GetTestRecordsByTagAsync(testTag);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"恢复测试记录时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<ChannelMapping>();
            }
        }

        /// <summary>
        /// 获取所有测试批次标识及其信息
        /// </summary>
        /// <returns>测试批次信息列表</returns>
        public async Task<List<TestBatchInfo>> GetAllTestBatchesAsync()
        {
            try
            {
                // 获取所有测试标识
                var testTags = await _repository.GetAllTestTagsAsync();
                var result = new List<TestBatchInfo>();

                // 获取每个测试批次的详细信息
                foreach (var tag in testTags)
                {
                    var records = await _repository.GetTestRecordsByTagAsync(tag);
                    if (!records.Any()) continue;

                    var batchInfo = new TestBatchInfo
                    {
                        TestTag = tag,
                        CreatedTime = records.Min(r => r.CreatedTime),
                        LastUpdatedTime = records.Max(r => r.UpdatedTime),
                        TotalCount = records.Count,
                        TestedCount = records.Count(r => r.OverallStatus != OverallResultStatus.NotTested && r.OverallStatus != OverallResultStatus.InProgress),
                        PassedCount = records.Count(r => r.OverallStatus == OverallResultStatus.Passed),
                        FailedCount = records.Count(r => r.OverallStatus == OverallResultStatus.Failed)
                    };

                    result.Add(batchInfo);
                }

                return result;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"获取测试批次信息时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<TestBatchInfo>();
            }
        }

        /// <summary>
        /// 删除测试批次
        /// </summary>
        /// <param name="testTag">测试标识</param>
        /// <returns>操作是否成功</returns>
        public async Task<bool> DeleteTestBatchAsync(string testTag)
        {
            try
            {
                if (string.IsNullOrEmpty(testTag))
                    return false;

                return await _repository.DeleteTestRecordsByTagAsync(testTag);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"删除测试批次时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
    }
} 