using System.Collections.ObjectModel;
using FatFullVersion.Models;
using FatFullVersion.Services;
using FatFullVersion.IServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using System.Collections.Generic;
using FatFullVersion.Entities;
using FatFullVersion.Entities.ValueObject;
using System.Linq;
using System;

namespace Fat_UnitTest.Services
{
    [TestClass]
    public class ChannelMappingServiceTests
    {
        /// <summary>
        /// 测试用的虚拟Repository实现
        /// </summary>
        private class DummyRepository : IRepository
        {
            #region 数据库初始化
            public Task<bool> InitializeDatabaseAsync()
            {
                return Task.FromResult(true);
            }
            #endregion

            #region PLC连接配置操作
            public Task<PlcConnectionConfig> GetTestPlcConnectionConfigAsync()
            {
                return Task.FromResult(new PlcConnectionConfig());
            }

            public Task<PlcConnectionConfig> GetTargetPlcConnectionConfigAsync()
            {
                return Task.FromResult(new PlcConnectionConfig());
            }

            public Task<bool> SavePlcConnectionConfigAsync(PlcConnectionConfig config)
            {
                return Task.FromResult(true);
            }

            public Task<List<PlcConnectionConfig>> GetAllPlcConnectionConfigsAsync()
            {
                return Task.FromResult(new List<PlcConnectionConfig>());
            }
            #endregion

            #region 通道比较表操作
            public Task<string> GetPlcCommunicationAddress(string channelTag)
            {
                return Task.FromResult(string.Empty);
            }

            public Task<List<ComparisonTable>> GetComparisonTablesAsync()
            {
                return Task.FromResult(new List<ComparisonTable>());
            }

            public Task<bool> AddComparisonTableAsync(ComparisonTable comparisonTable)
            {
                return Task.FromResult(true);
            }

            public Task<bool> AddComparisonTablesAsync(List<ComparisonTable> comparisonTables)
            {
                return Task.FromResult(true);
            }

            public Task<bool> UpdateComparisonTableAsync(ComparisonTable comparisonTable)
            {
                return Task.FromResult(true);
            }

            public Task<bool> DeleteComparisonTableAsync(int id)
            {
                return Task.FromResult(true);
            }

            public Task<bool> SaveAllComparisonTablesAsync(List<ComparisonTable> comparisonTables)
            {
                return Task.FromResult(true);
            }
            #endregion

            #region 测试记录操作
            public Task<bool> SaveTestRecordsAsync(IEnumerable<ChannelMapping> records)
            {
                return Task.FromResult(true);
            }

            public Task<bool> SaveTestRecordAsync(ChannelMapping record)
            {
                return Task.FromResult(true);
            }

            public Task<bool> SaveHardPointTestResultsAsync(IEnumerable<ChannelMapping> records)
            {
                return Task.FromResult(true);
            }

            public Task<bool> UpdateRetestResultAsync(ChannelMapping record)
            {
                return Task.FromResult(true);
            }

            public Task<List<ChannelMapping>> GetTestRecordsByTagAsync(string testTag)
            {
                return Task.FromResult(new List<ChannelMapping>());
            }

            public Task<List<string>> GetAllTestTagsAsync()
            {
                return Task.FromResult(new List<string>());
            }

            public Task<bool> DeleteTestRecordsByTagAsync(string testTag)
            {
                return Task.FromResult(true);
            }

            public Task<List<ChannelMapping>> GetAllTestRecordsAsync()
            {
                return Task.FromResult(new List<ChannelMapping>());
            }
            #endregion

            #region 废弃的方法 - 向后兼容性
            [Obsolete("已废弃，请使用SaveTestRecordsAsync")]
            public Task<bool> SaveTestRecordsWithSqlAsync(IEnumerable<ChannelMapping> records)
            {
                return SaveTestRecordsAsync(records);
            }

            [Obsolete("已废弃，请使用SaveTestRecordAsync")]
            public Task<bool> SaveTestRecordWithSqlAsync(ChannelMapping record)
            {
                return SaveTestRecordAsync(record);
            }
            #endregion
        }

        private readonly IChannelStateManager _stateManager = new ChannelStateManager();

        [TestMethod]
        public void AllocateChannels_Should_Assign_Batch_When_No_PLC_Mappings()
        {
            // Arrange
            var service = new ChannelMappingService(new DummyRepository(), _stateManager);
            var channels = new ObservableCollection<ChannelMapping>
            {
                new ChannelMapping { ModuleType = "AI", VariableName = "PT_1" },
                new ChannelMapping { ModuleType = "DI", VariableName = "DI_1" }
            };

            // Act
            var result = service.AllocateChannelsTestAsync(channels).Result;

            // Assert
            foreach (var ch in result)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(ch.TestBatch), $"{ch.VariableName} 未分配批次");
            }
        }
    }
} 