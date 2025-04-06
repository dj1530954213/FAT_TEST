using FatFullVersion.Optimizations;
using FatFullVersion.Services;
using FatFullVersion.Services.Interfaces;
using FatFullVersion.Views;
using Prism.Ioc;
using Prism.Modularity;
using System.Windows;
using FatFullVersion.IServices;
using FatFullVersion.Entities.EntitiesEnum;
using Prism.DryIoc;
using DryIoc;
using FatFullVersion.Common;
using Prism.Events;

namespace FatFullVersion
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication
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
            var container = containerRegistry.GetContainer();

            containerRegistry.RegisterSingleton<IMessageService, MessageService>();
            //注册点位表处理服务，选择Excel实现
            containerRegistry.RegisterSingleton<IPointDataService, ExcelPointDataService>();
            
            // 注册通道映射服务
            containerRegistry.RegisterSingleton<IChannelMappingService, ChannelMappingService>();

            // 注册仓储层服务
            containerRegistry.RegisterSingleton<IRepository, Repository>();

            // 注册服务定位器
            containerRegistry.RegisterSingleton<IServiceLocator, ServiceLocator>();

            // 注册仓储实例，用于后续创建PLC通信工厂
            var repository = container.Resolve<IRepository>();

            // 注册PLC通信工厂 - 分别为测试PLC和被测PLC创建工厂
            var testPlcFactory = new PlcCommunicationFactory(repository, PlcType.TestPlc);
            var targetPlcFactory = new PlcCommunicationFactory(repository, PlcType.TargetPlc);

            // 将两个工厂注册到容器中
            container.RegisterInstance(testPlcFactory, serviceKey: "TestPlcFactory");
            container.RegisterInstance(targetPlcFactory, serviceKey: "TargetPlcFactory");

            // 分别创建测试PLC和被测PLC的通信实例
            var testPlcCommunication = testPlcFactory.CreatePlcCommunication();
            var targetPlcCommunication = targetPlcFactory.CreatePlcCommunication();

            // 将两个通信实例注册到容器中
            container.RegisterInstance<IPlcCommunication>(testPlcCommunication, serviceKey: "TestPlcCommunication");
            container.RegisterInstance<IPlcCommunication>(targetPlcCommunication, serviceKey: "TargetPlcCommunication");
            
            // 为DataEditView注册特定的PLC通信服务
            container.RegisterDelegate<IPlcCommunication>(
                factoryDelegate: r => r.Resolve<IPlcCommunication>(serviceKey: "TestPlcCommunication"), 
                serviceKey: "TestPlc");
            container.RegisterDelegate<IPlcCommunication>(
                factoryDelegate: r => r.Resolve<IPlcCommunication>(serviceKey: "TargetPlcCommunication"), 
                serviceKey: "TargetPlc");

            // 注册测试任务管理器
            containerRegistry.RegisterSingleton<ITestTaskManager, TestTaskManager>();
            
            // 注册视图用于导航
            containerRegistry.RegisterForNavigation<DataEditView>();
            
            // 直接在容器中注册 DataEditView 的自定义构造函数
            container.Register<DataEditView>(made: Parameters.Of
                .Type<IPointDataService>()
                .Type<IChannelMappingService>()
                .Type<IEventAggregator>()
                .Type<ITestTaskManager>()
                .Type<IPlcCommunication>(serviceKey: "TestPlc")
                .Type<IPlcCommunication>(serviceKey: "TargetPlc")
                .Type<IMessageService>());

            // 注册测试结果导出服务
            containerRegistry.RegisterSingleton<ITestResultExportService, TestResultExportService>();
        }
        
        //模块注册点
        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
        }
    }
}
