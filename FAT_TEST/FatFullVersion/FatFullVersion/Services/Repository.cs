using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using FatFullVersion.Data;
using FatFullVersion.Entities;
using FatFullVersion.Entities.EntitiesEnum;
using FatFullVersion.Entities.ValueObject;
using FatFullVersion.IServices;
using FatFullVersion.Models;
using Microsoft.EntityFrameworkCore;

namespace FatFullVersion.Services
{
    /// <summary>
    /// 数据访问仓储类
    /// 提供对数据库的CRUD操作
    /// </summary>
    public class Repository : IRepository
    {
        private readonly ApplicationDbContext _context;

        public Repository(ApplicationDbContext context)
        {
            _context = context;
            _context.Database.EnsureCreated(); // 确保数据库和表已创建
            // 检查连接字符串配置
            var connectionString = _context.Database.GetConnectionString();
            Console.WriteLine($"当前连接字符串: {connectionString}");
            var tableExists = _context.Database.ExecuteSqlRawAsync("SELECT count(*) FROM sqlite_master WHERE type='table' AND name='ChannelMappings'");
            Console.WriteLine($"ChannelMappings表存在: {tableExists.Result > 0}");
        }

        public async Task<bool> InitializeDatabaseAsync()
        {
            try
            {
                return await _context.Database.EnsureCreatedAsync();
            }
            catch
            {
                return false;
            }
        }

        public async Task<PlcConnectionConfig> GetTestPlcConnectionConfigAsync()
        {
            return await _context.PlcConnections.FirstOrDefaultAsync(p => p.IsTestPlc) ?? new();
        }

        public async Task<PlcConnectionConfig> GetTargetPlcConnectionConfigAsync()
        {
            return await _context.PlcConnections.FirstOrDefaultAsync(p => !p.IsTestPlc) ?? new();
        }

        public async Task<bool> SavePlcConnectionConfigAsync(PlcConnectionConfig config)
        {
            try
            {
                var existing = await _context.PlcConnections.FindAsync(config.Id);
                if (existing == null)
                    _context.PlcConnections.Add(config);
                else
                    _context.Entry(existing).CurrentValues.SetValues(config);

                return await _context.SaveChangesAsync() > 0;
            }
            catch (Exception e)
            {
                MessageBox.Show($"保存数据出现错误:{e.Message}");
                return false;
            }
        }

        public async Task<List<PlcConnectionConfig>> GetAllPlcConnectionConfigsAsync()
        {
            return await _context.PlcConnections.ToListAsync();
        }

        public async Task<string> GetPlcCommunicationAddress(string channelTag)
        {
            return (await _context.ComparisonTables
                .FirstOrDefaultAsync(c => c.ChannelAddress == channelTag))?.CommunicationAddress ?? string.Empty;
        }

        public async Task<List<ComparisonTable>> GetComparisonTablesAsync()
        {
            return await _context.ComparisonTables.ToListAsync();
        }

        public async Task<bool> AddComparisonTableAsync(ComparisonTable comparisonTable)
        {
            _context.ComparisonTables.Add(comparisonTable);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> AddComparisonTablesAsync(List<ComparisonTable> comparisonTables)
        {
            _context.ComparisonTables.AddRange(comparisonTables);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateComparisonTableAsync(ComparisonTable comparisonTable)
        {
            var existing = await _context.ComparisonTables.FindAsync(comparisonTable.Id);
            if (existing == null) return false;

            _context.Entry(existing).CurrentValues.SetValues(comparisonTable);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteComparisonTableAsync(int id)
        {
            var item = await _context.ComparisonTables.FindAsync(id);
            if (item == null) return false;

            _context.ComparisonTables.Remove(item);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> SaveAllComparisonTablesAsync(List<ComparisonTable> comparisonTables)
        {
            try
            {
                // 获取数据库中所有的记录
                var existingTables = await _context.ComparisonTables.ToListAsync();

                // 删除不存在于 comparisonTables 中的记录
                var tablesToRemove = existingTables.Where(existingTable => !comparisonTables.Any(newTable => newTable.Id == existingTable.Id)).ToList();
                _context.ComparisonTables.RemoveRange(tablesToRemove);

                // 更新已有的记录和插入新记录
                foreach (var comparisonTable in comparisonTables)
                {
                    var existingTable = existingTables.FirstOrDefault(x => x.Id == comparisonTable.Id);
                    if (existingTable != null)
                    {
                        // 如果表格已经存在，更新它
                        _context.Entry(existingTable).CurrentValues.SetValues(comparisonTable);
                    }
                    else
                    {
                        // 如果表格不存在，插入新记录
                        _context.ComparisonTables.Add(comparisonTable);
                    }
                }

                // 保存更改
                return await _context.SaveChangesAsync()>=0;
            }
            catch (Exception e)
            {
                return false;
            }
        }
        
        #region 测试记录操作
        
        /// <summary>
        /// 保存测试记录集合
        /// </summary>
        /// <param name="records">测试记录集合</param>
        /// <returns>保存操作是否成功</returns>
        public async Task<bool> SaveTestRecordsAsync(IEnumerable<ChannelMapping> records)
        {
            try
            {
                if (records == null || !records.Any())
                    return true;

                foreach (var record in records)
                {
                    // 确保每条记录都有Guid作为Id
                    if (record.Id == Guid.Empty)
                    {
                        record.Id = Guid.NewGuid();
                    }

                    // 更新时间戳
                    record.UpdatedTime = DateTime.Now;

                    // 处理可能存在的NaN值，将其转换为null
                    // 检查所有数值类型的属性，替换NaN值
                    ProcessNanValues(record);

                    // 检查记录是否已存在
                    var existing = await _context.ChannelMappings.FindAsync(record.Id);
                    if (existing != null)
                    {
                        // 更新现有记录
                        _context.Entry(existing).CurrentValues.SetValues(record);
                    }
                    else
                    {
                        // 添加新记录
                        _context.ChannelMappings.Add(record);
                    }
                }

                return await _context.SaveChangesAsync() > 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存测试记录时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            finally
            {
                foreach (var record in records)
                {
                    RestoreNanValues(record);
                }
            }
        }
        
        /// <summary>
        /// 保存单个测试记录
        /// </summary>
        /// <param name="record">测试记录</param>
        /// <returns>保存操作是否成功</returns>
        public async Task<bool> SaveTestRecordAsync(ChannelMapping record)
        {
            try
            {
                if (record == null)
                    return false;
                
                // 确保记录有Guid作为Id
                if (record.Id == Guid.Empty)
                {
                    record.Id = Guid.NewGuid();
                }
                
                // 更新时间戳
                record.UpdatedTime = DateTime.Now;
                
                // 处理NaN值
                ProcessNanValues(record);
                
                // 检查记录是否已存在
                var existing = await _context.ChannelMappings.FindAsync(record.Id);
                if (existing != null)
                {
                    // 更新现有记录
                    _context.Entry(existing).CurrentValues.SetValues(record);
                }
                else
                {
                    // 添加新记录
                    _context.ChannelMappings.Add(record);
                }
                
                return await _context.SaveChangesAsync() > 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存单个测试记录时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
        
        /// <summary>
        /// 根据测试标识获取测试记录
        /// </summary>
        /// <param name="testTag">测试标识</param>
        /// <returns>测试记录集合</returns>
        public async Task<List<ChannelMapping>> GetTestRecordsByTagAsync(string testTag)
        {
            try
            {
                if (string.IsNullOrEmpty(testTag))
                    return new List<ChannelMapping>();
                
                var records = await _context.ChannelMappings
                    .Where(c => c.TestTag == testTag)
                    .ToListAsync();
                
                // 将数据库中的null值转换回NaN
                foreach (var record in records)
                {
                    RestoreNanValues(record);
                }
                
                return records;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"获取测试记录时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<ChannelMapping>();
            }
        }
        
        /// <summary>
        /// 获取所有不同的测试标识
        /// </summary>
        /// <returns>测试标识集合</returns>
        public async Task<List<string>> GetAllTestTagsAsync()
        {
            try
            {
                return await _context.ChannelMappings
                    .Where(c => !string.IsNullOrEmpty(c.TestTag))
                    .Select(c => c.TestTag)
                    .Distinct()
                    .OrderByDescending(tag => tag) // 按照标签降序排序(通常新的测试记录标签更大)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"获取测试标识时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<string>();
            }
        }
        
        /// <summary>
        /// 根据测试标识删除测试记录
        /// </summary>
        /// <param name="testTag">测试标识</param>
        /// <returns>删除操作是否成功</returns>
        public async Task<bool> DeleteTestRecordsByTagAsync(string testTag)
        {
            try
            {
                if (string.IsNullOrEmpty(testTag))
                    return false;
                
                // 获取符合条件的记录
                var records = await _context.ChannelMappings
                    .Where(c => c.TestTag == testTag)
                    .ToListAsync();
                
                if (!records.Any())
                    return true; // 没有找到记录，视为成功
                
                // 删除所有符合条件的记录
                _context.ChannelMappings.RemoveRange(records);
                
                return await _context.SaveChangesAsync() > 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"删除测试记录时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
        
        #endregion

        /// <summary>
        /// 处理ChannelMapping对象中的NaN值，将其转换为-999999999以便存储到数据库
        /// </summary>
        /// <param name="record">待处理的通道映射对象</param>
        private void ProcessNanValues(ChannelMapping record)
        {
            // 将NaN值转换为-999999999
            // float类型字段处理
            if (float.IsNaN(record.RangeLowerLimitValue))
                record.RangeLowerLimitValue = -999999999;
            
            if (float.IsNaN(record.RangeUpperLimitValue))
                record.RangeUpperLimitValue = -999999999;
            
            if (float.IsNaN(record.SLLSetValueNumber))
                record.SLLSetValueNumber = -999999999;
            
            if (float.IsNaN(record.SLSetValueNumber))
                record.SLSetValueNumber = -999999999;
            
            if (float.IsNaN(record.SHSetValueNumber))
                record.SHSetValueNumber = -999999999;
            
            if (float.IsNaN(record.SHHSetValueNumber))
                record.SHHSetValueNumber = -999999999;
            
            // double类型字段处理
            if (double.IsNaN(record.ExpectedValue))
                record.ExpectedValue = -999999999;
            
            if (double.IsNaN(record.ActualValue))
                record.ActualValue = -999999999;
            
            if (double.IsNaN(record.Value0Percent))
                record.Value0Percent = -999999999;
            
            if (double.IsNaN(record.Value25Percent))
                record.Value25Percent = -999999999;
            
            if (double.IsNaN(record.Value50Percent))
                record.Value50Percent = -999999999;
            
            if (double.IsNaN(record.Value75Percent))
                record.Value75Percent = -999999999;
            
            if (double.IsNaN(record.Value100Percent))
                record.Value100Percent = -999999999;
        }
        
        /// <summary>
        /// 将数据库中读取的ChannelMapping对象中的-999999999值转换回NaN
        /// </summary>
        /// <param name="record">待处理的通道映射对象</param>
        private void RestoreNanValues(ChannelMapping record)
        {
            // 将-999999999转换回NaN
            // 处理float类型的字段
            if (record.RangeLowerLimitValue == -999999999)
                record.RangeLowerLimitValue = float.NaN;
            
            if (record.RangeUpperLimitValue == -999999999)
                record.RangeUpperLimitValue = float.NaN;
            
            if (record.SLLSetValueNumber == -999999999)
                record.SLLSetValueNumber = float.NaN;
            
            if (record.SLSetValueNumber == -999999999)
                record.SLSetValueNumber = float.NaN;
            
            if (record.SHSetValueNumber == -999999999)
                record.SHSetValueNumber = float.NaN;
            
            if (record.SHHSetValueNumber == -999999999)
                record.SHHSetValueNumber = float.NaN;
            
            // 处理double类型的字段
            if (record.ExpectedValue == -999999999)
                record.ExpectedValue = double.NaN;
            
            if (record.ActualValue == -999999999)
                record.ActualValue = double.NaN;
            
            if (record.Value0Percent == -999999999)
                record.Value0Percent = double.NaN;
            
            if (record.Value25Percent == -999999999)
                record.Value25Percent = double.NaN;
            
            if (record.Value50Percent == -999999999)
                record.Value50Percent = double.NaN;
            
            if (record.Value75Percent == -999999999)
                record.Value75Percent = double.NaN;
            
            if (record.Value100Percent == -999999999)
                record.Value100Percent = double.NaN;
        }

        /// <summary>
        /// 获取所有测试记录
        /// </summary>
        /// <returns>所有测试记录集合</returns>
        public async Task<List<ChannelMapping>> GetAllTestRecordsAsync()
        {
            try
            {
                var records = await _context.ChannelMappings.ToListAsync();
                
                // 将数据库中的null值转换回NaN
                foreach (var record in records)
                {
                    RestoreNanValues(record);
                }
                
                return records;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"获取所有测试记录时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<ChannelMapping>();
            }
        }
    }
}
