using System;
using System.IO;
using FatFullVersion.Entities;
using FatFullVersion.Entities.ValueObject;
using FatFullVersion.Models;
using Microsoft.EntityFrameworkCore;

namespace FatFullVersion.Data
{
    /// <summary>
    /// 应用程序数据库上下文类
    /// 负责管理数据库连接和实体映射
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        public DbSet<PlcConnectionConfig> PlcConnections { get; set; }
        public DbSet<ComparisonTable> ComparisonTables { get; set; }
        public DbSet<ChannelMapping> ChannelMappings { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }
    }
} 
