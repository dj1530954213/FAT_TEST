using FatFullVersion.Optimizations;
using FatFullVersion.Services;
using FatFullVersion.Services.Interfaces;
using FatFullVersion.Views;
using Prism.Ioc;
using Prism.Modularity;
using System.Windows;
using FatFullVersion.IServices;

namespace FatFullVersion
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        protected override Window CreateShell()
        {
            var window = Container.Resolve<MainWindow>();
            
            // 启用内存优化
            if (window != null)
            {
                MemoryOptimizations.EnableOptimizations(window);
            }
            
            return window;
        }
        
        //依赖注入点
        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<IMessageService, MessageService>();
            //注册点位表处理服务，选择Excel实现
            containerRegistry.RegisterSingleton<IPointDataService, ExcelPointDataService>();
            
            // 注册通道映射服务
            containerRegistry.RegisterSingleton<IChannelMappingService, ChannelMappingService>();
            
            // 注册视图
            containerRegistry.RegisterForNavigation<DataEditView>();
        }
        
        //模块注册点
        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
        }
    }
}
