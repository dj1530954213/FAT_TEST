using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            var existing = await _context.PlcConnections.FindAsync(config.Id);
            if (existing == null)
                _context.PlcConnections.Add(config);
            else
                _context.Entry(existing).CurrentValues.SetValues(config);

            return await _context.SaveChangesAsync() > 0;
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
            var all = await _context.ComparisonTables.ToListAsync();
            _context.ComparisonTables.RemoveRange(all);
            _context.ComparisonTables.AddRange(comparisonTables);
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
