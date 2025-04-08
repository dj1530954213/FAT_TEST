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
    }
}
