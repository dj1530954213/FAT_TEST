using Prism.Mvvm;
using System.Collections.ObjectModel;
using Prism.Commands;
using System;
using System.Windows.Media;
using System.Linq;
using System.Windows.Data;
using System.Globalization;
using System.Windows;
using FatFullVersion.Models;
using FatFullVersion.Services.Interfaces;
using FatFullVersion.IServices;
using System.Collections.Generic;
using System.Threading.Tasks;
using FatFullVersion.Entities;
using FatFullVersion.Entities.ValueObject;
using FatFullVersion.Entities.EntitiesEnum;

namespace FatFullVersion.ViewModels
{
    public class DataEditViewModel : BindableBase
    {
        #region 属性和字段

        private readonly IPointDataService _pointDataService;
        private readonly IChannelMappingService _channelMappingService;

        private string _message;
        public string Message
        {
            get { return _message; }
            set { SetProperty(ref _message, value); }
        }

        // 当前状态消息
        private string _statusMessage;
        public string StatusMessage
        {
            get { return _statusMessage; }
            set { SetProperty(ref _statusMessage, value); }
        }

        // 加载状态
        private bool _isLoading;
        public bool IsLoading
        {
            get { return _isLoading; }
            set { SetProperty(ref _isLoading, value); }
        }

        // AI通道数据
        private ObservableCollection<ChannelMapping> _aiChannels;
        public ObservableCollection<ChannelMapping> AIChannels
        {
            get { return _aiChannels; }
            set { SetProperty(ref _aiChannels, value); }
        }

        // AO通道数据
        private ObservableCollection<ChannelMapping> _aoChannels;
        public ObservableCollection<ChannelMapping> AOChannels
        {
            get { return _aoChannels; }
            set { SetProperty(ref _aoChannels, value); }
        }

        // DI通道数据
        private ObservableCollection<ChannelMapping> _diChannels;
        public ObservableCollection<ChannelMapping> DIChannels
        {
            get { return _diChannels; }
            set { SetProperty(ref _diChannels, value); }
        }

        // DO通道数据
        private ObservableCollection<ChannelMapping> _doChannels;
        public ObservableCollection<ChannelMapping> DOChannels
        {
            get { return _doChannels; }
            set { SetProperty(ref _doChannels, value); }
        }

        // 当前选择的通道类型（用于统一DataGrid）
        private string _selectedChannelType;
        public string SelectedChannelType
        {
            get { return _selectedChannelType; }
            set 
            { 
                if (SetProperty(ref _selectedChannelType, value))
                {
                    UpdateCurrentChannels();
                }
            }
        }

        // 当前显示的通道列表
        private ObservableCollection<ChannelMapping> _currentChannels;
        public ObservableCollection<ChannelMapping> CurrentChannels
        {
            get { return _currentChannels; }
            set { SetProperty(ref _currentChannels, value); }
        }

        // 测试结果数据
        private ObservableCollection<TestResult> _testResults;
        public ObservableCollection<TestResult> TestResults
        {
            get { return _testResults; }
            set
            {
                SetProperty(ref _testResults, value);
                //修改测试结果数据集时调用此方法更新统计数据
                UpdatePointStatistics();
            }
        }

        // 批次相关数据
        private ObservableCollection<BatchInfo> _batches;
        public ObservableCollection<BatchInfo> Batches
        {
            get { return _batches; }
            set { SetProperty(ref _batches, value); }
        }

        private BatchInfo _selectedBatch;
        public BatchInfo SelectedBatch
        {
            get { return _selectedBatch; }
            set { SetProperty(ref _selectedBatch, value); }
        }

        private bool _isBatchSelectionOpen;
        public bool IsBatchSelectionOpen
        {
            get { return _isBatchSelectionOpen; }
            set { SetProperty(ref _isBatchSelectionOpen, value); }
        }

        private string _selectedResultFilter;
        public string SelectedResultFilter
        {
            get { return _selectedResultFilter; }
            set 
            { 
                if (SetProperty(ref _selectedResultFilter, value))
                {
                    // 应用结果过滤
                    ApplyResultFilter();
                }
            }
        }

        // 点位统计数据
        private string _totalPointCount;
        public string TotalPointCount
        {
            get { return _totalPointCount; }
            set { SetProperty(ref _totalPointCount, value); }
        }

        private string _testedPointCount;
        public string TestedPointCount
        {
            get { return _testedPointCount; }
            set { SetProperty(ref _testedPointCount, value); }
        }

        private string _waitingPointCount;
        public string WaitingPointCount
        {
            get { return _waitingPointCount; }
            set { SetProperty(ref _waitingPointCount, value); }
        }

        private string _successPointCount;
        public string SuccessPointCount
        {
            get { return _successPointCount; }
            set { SetProperty(ref _successPointCount, value); }
        }

        private string _failurePointCount;
        public string FailurePointCount
        {
            get { return _failurePointCount; }
            set { SetProperty(ref _failurePointCount, value); }
        }

        // 命令
        public DelegateCommand ImportConfigCommand { get; private set; }
        public DelegateCommand SelectBatchCommand { get; private set; }
        public DelegateCommand FinishWiringCommand { get; private set; }
        public DelegateCommand StartTestCommand { get; private set; }
        public DelegateCommand<TestResult> RetestCommand { get; private set; }
        public DelegateCommand<ChannelMapping> MoveUpCommand { get; private set; }
        public DelegateCommand<ChannelMapping> MoveDownCommand { get; private set; }
        public DelegateCommand ConfirmBatchSelectionCommand { get; private set; }
        public DelegateCommand CancelBatchSelectionCommand { get; private set; }
        public DelegateCommand AllocateChannelsCommand { get; private set; }
        public DelegateCommand ClearChannelAllocationsCommand { get; private set; }

        #endregion

        public DataEditViewModel(IPointDataService pointDataService, IChannelMappingService channelMappingService)
        {
            _pointDataService = pointDataService ?? throw new ArgumentNullException(nameof(pointDataService));
            _channelMappingService = channelMappingService ?? throw new ArgumentNullException(nameof(channelMappingService));
            
            Message = "View A from your Prism Module";

            // 初始化命令
            ImportConfigCommand = new DelegateCommand(ImportConfig);
            SelectBatchCommand = new DelegateCommand(SelectBatch);
            FinishWiringCommand = new DelegateCommand(FinishWiring);
            StartTestCommand = new DelegateCommand(StartTest);
            RetestCommand = new DelegateCommand<TestResult>(Retest);
            MoveUpCommand = new DelegateCommand<ChannelMapping>(MoveUp);
            MoveDownCommand = new DelegateCommand<ChannelMapping>(MoveDown);
            ConfirmBatchSelectionCommand = new DelegateCommand(ConfirmBatchSelection);
            CancelBatchSelectionCommand = new DelegateCommand(CancelBatchSelection);
            AllocateChannelsCommand = new DelegateCommand(AllocateChannelsAsync);
            ClearChannelAllocationsCommand = new DelegateCommand(ClearChannelAllocationsAsync);

            // 初始化各种集合
            AIChannels = new ObservableCollection<ChannelMapping>();
            AOChannels = new ObservableCollection<ChannelMapping>();
            DIChannels = new ObservableCollection<ChannelMapping>();
            DOChannels = new ObservableCollection<ChannelMapping>();
            TestResults = new ObservableCollection<TestResult>();
            Batches = new ObservableCollection<BatchInfo>();
            
            // 初始化测试数据
            //InitializeTestData();
            
            // 初始化批次数据
            InitializeBatchData();
            
            // 默认选择AI通道
            SelectedChannelType = "AI通道";
            UpdateCurrentChannels();
            
            // 默认选择"全部"过滤
            SelectedResultFilter = "全部";

            // 更新点位统计数据
            UpdatePointStatistics();
        }

        private void UpdateCurrentChannels()
        {
            if (string.IsNullOrEmpty(SelectedChannelType))
                return;

            switch (SelectedChannelType)
            {
                case "AI通道":
                    CurrentChannels = AIChannels;
                    break;
                case "AO通道":
                    CurrentChannels = AOChannels;
                    break;
                case "DI通道":
                    CurrentChannels = DIChannels;
                    break;
                case "DO通道":
                    CurrentChannels = DOChannels;
                    break;
                default:
                    CurrentChannels = new ObservableCollection<ChannelMapping>();
                    break;
            }
        }

        #region 命令实现

        private async void ImportConfig()
        {
            // 防止重复点击
            IsLoading = true;
            StatusMessage = "正在导入Excel点表配置文件...";

            try
            {
                // 导入点表配置，并处理导入的数据
                if (_pointDataService != null)
                { 
                    // 使用TaskCompletionSource来处理回调形式的异步方法
                    var tcs = new TaskCompletionSource<IEnumerable<ExcelPointData>>();
                    
                    await _pointDataService.ImportPointConfigurationAsync(data => {
                        try {
                            tcs.SetResult(data);
                        } catch (Exception ex) {
                            tcs.SetException(ex);
                        }
                    });
                    
                    // 等待异步操作完成并获取结果
                    var importedData = await tcs.Task;
                    
                    // 处理导入的数据
                    if (importedData != null)
                    {
                        await ProcessImportedDataAsync(importedData);
                    }
                }
                else
                {
                    MessageBox.Show("无法获取点表数据服务", "导入失败", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导入配置失败: {ex.Message}", "导入失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
                StatusMessage = string.Empty;
            }
        }
        
        /// <summary>
        /// 处理导入的点表数据
        /// </summary>
        /// <param name="importedData">导入的点表数据列表</param>
        private async Task ProcessImportedDataAsync(IEnumerable<ExcelPointData> importedData)
        {
            try
            {
                // 清空现有数据
                AIChannels.Clear();
                AOChannels.Clear();
                DIChannels.Clear();
                DOChannels.Clear();
                TestResults.Clear();

                // 按模块类型分类导入的数据
                foreach (var excelPointData in importedData)
                {
                    // 创建通道映射
                    var channelMapping = new ChannelMapping
                    {
                        // ExcelPointData属性映射
                        ModuleName = excelPointData.ModuleName,
                        ModuleType = excelPointData.ModuleType,
                        PowerSupplyType = excelPointData.PowerSupplyType,
                        WireSystem = excelPointData.WireSystem,
                        Tag = excelPointData.Tag,
                        StationName = excelPointData.StationName,
                        VariableName = excelPointData.VariableName,
                        VariableDescription = excelPointData.VariableDescription,
                        DataType = excelPointData.DataType,
                        AccessProperty = excelPointData.AccessProperty,
                        SaveHistory = excelPointData.SaveHistory,
                        PowerFailureProtection = excelPointData.PowerFailureProtection,
                        ChannelTag = excelPointData.ChannelTag,
                        
                        // 量程信息
                        RangeLowerLimit = excelPointData.RangeLowerLimit,
                        RangeLowerLimitValue = excelPointData.RangeLowerLimitValue,
                        RangeUpperLimit = excelPointData.RangeUpperLimit,
                        RangeUpperLimitValue = excelPointData.RangeUpperLimitValue,
                        
                        // 报警设定
                        SLLSetValue = excelPointData.SLLSetValue,
                        SLLSetValueNumber = excelPointData.SLLSetValueNumber,
                        SLLSetPoint = excelPointData.SLLSetPoint,
                        SLLSetPointPLCAddress = excelPointData.SLLSetPointPLCAddress,
                        SLLSetPointCommAddress = excelPointData.SLLSetPointCommAddress,
                        
                        SLSetValue = excelPointData.SLSetValue,
                        SLSetValueNumber = excelPointData.SLSetValueNumber,
                        SLSetPoint = excelPointData.SLSetPoint,
                        SLSetPointPLCAddress = excelPointData.SLSetPointPLCAddress,
                        SLSetPointCommAddress = excelPointData.SLSetPointCommAddress,
                        
                        SHSetValue = excelPointData.SHSetValue,
                        SHSetValueNumber = excelPointData.SHSetValueNumber,
                        SHSetPoint = excelPointData.SHSetPoint,
                        SHSetPointPLCAddress = excelPointData.SHSetPointPLCAddress,
                        SHSetPointCommAddress = excelPointData.SHSetPointCommAddress,
                        
                        SHHSetValue = excelPointData.SHHSetValue,
                        SHHSetValueNumber = excelPointData.SHHSetValueNumber,
                        SHHSetPoint = excelPointData.SHHSetPoint,
                        SHHSetPointPLCAddress = excelPointData.SHHSetPointPLCAddress,
                        SHHSetPointCommAddress = excelPointData.SHHSetPointCommAddress,
                        
                        // 报警点位
                        LLAlarm = excelPointData.LLAlarm,
                        LLAlarmPLCAddress = excelPointData.LLAlarmPLCAddress,
                        LLAlarmCommAddress = excelPointData.LLAlarmCommAddress,
                        
                        LAlarm = excelPointData.LAlarm,
                        LAlarmPLCAddress = excelPointData.LAlarmPLCAddress,
                        LAlarmCommAddress = excelPointData.LAlarmCommAddress,
                        
                        HAlarm = excelPointData.HAlarm,
                        HAlarmPLCAddress = excelPointData.HAlarmPLCAddress,
                        HAlarmCommAddress = excelPointData.HAlarmCommAddress,
                        
                        HHAlarm = excelPointData.HHAlarm,
                        HHAlarmPLCAddress = excelPointData.HHAlarmPLCAddress,
                        HHAlarmCommAddress = excelPointData.HHAlarmCommAddress,
                        
                        // 维护相关
                        MaintenanceValueSetting = excelPointData.MaintenanceValueSetting,
                        MaintenanceValueSetPoint = excelPointData.MaintenanceValueSetPoint,
                        MaintenanceValueSetPointPLCAddress = excelPointData.MaintenanceValueSetPointPLCAddress,
                        MaintenanceValueSetPointCommAddress = excelPointData.MaintenanceValueSetPointCommAddress,
                        MaintenanceEnableSwitchPoint = excelPointData.MaintenanceEnableSwitchPoint,
                        MaintenanceEnableSwitchPointPLCAddress = excelPointData.MaintenanceEnableSwitchPointPLCAddress,
                        MaintenanceEnableSwitchPointCommAddress = excelPointData.MaintenanceEnableSwitchPointCommAddress,
                        
                        // 地址信息
                        PLCAbsoluteAddress = excelPointData.PLCAbsoluteAddress,
                        
                        // 时间信息
                        CreatedTime = DateTime.Now,
                        
                        // 新增字段
                        TestBatch = $"批次{DateTime.Now:yyyyMMdd}",
                        TestPLCChannelTag = string.Empty,
                        TestPLCCommunicationAddress = string.Empty
                    };

                    // 根据模块类型分配到对应的通道组
                    switch (excelPointData.ModuleType?.ToLower())
                    {
                        case "ai":
                            AIChannels.Add(channelMapping);
                            break;

                        case "ao":
                            AOChannels.Add(channelMapping);
                            break;

                        case "di":
                            DIChannels.Add(channelMapping);
                            break;

                        case "do":
                            DOChannels.Add(channelMapping);
                            break;

                        default:
                            // 对于不识别的模块类型，默认分到AI组
                            AIChannels.Add(channelMapping);
                            break;
                    }

                    // 将测试信息写入测试结果区域
                    TestResults.Add(new TestResult()
                    {
                        TestId = excelPointData.SerialNumber,
                        BatchName = channelMapping.TestBatch, // 使用新设置的测试批次
                        VariableName = excelPointData.VariableName,
                        PointType = excelPointData.ModuleType,
                        ValueType = excelPointData.DataType,
                        TestPlcChannel = channelMapping.TestPLCChannelTag, // 使用新添加的字段
                        TargetPlcChannel = excelPointData.ChannelTag,
                        RangeMax = excelPointData.RangeUpperLimitValue,
                        RangeMin = excelPointData.RangeLowerLimitValue,
                        ResultText = "待测试",
                        TestResultStatus = 0,
                    });
                }
                //自动分配通道服务调用
                var channelsMappingResult = await _channelMappingService.AllocateChannelsTestAsync(
                    aiChannels: AIChannels, 
                    aoChannels: AOChannels, 
                    diChannels: DIChannels, 
                    doChannels: DOChannels);
                var result = channelsMappingResult.AI.Where(a => a.TestBatch.Contains("3")).ToArray();
                //同步更新测试结果表中的测试批次于测试PLC通道
                UpdateTestResultChannels(channelsMappingResult.AI, channelsMappingResult.AO, channelsMappingResult.DI, channelsMappingResult.DO);


                // 通知UI更新
                RaisePropertyChanged(nameof(AIChannels));
                RaisePropertyChanged(nameof(AOChannels));
                RaisePropertyChanged(nameof(DIChannels));
                RaisePropertyChanged(nameof(DOChannels));
                RaisePropertyChanged(nameof(TestResults));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"处理导入数据失败: {ex.Message}", "导入失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SelectBatch()
        {
            // 显示批次选择窗口
            IsBatchSelectionOpen = true;
        }

        private void ConfirmBatchSelection()
        {
            if (SelectedBatch != null)
            {
                Message = $"已选择批次: {SelectedBatch.BatchName}";
                // 这里可以加载选定批次的数据
            }
            IsBatchSelectionOpen = false;
        }

        private void CancelBatchSelection()
        {
            IsBatchSelectionOpen = false;
        }

        private void FinishWiring()
        {
            // 通道连接完成的实现
            Message = "通道连接完成";
        }

        private void StartTest()
        {
            // 开始测试的实现
            Message = "开始测试";

            // 模拟测试，随机生成一些测试结果状态
            Random random = new Random();
            foreach (var result in TestResults)
            {
                result.TestResultStatus = random.Next(0, 3); // 0:未测试, 1:通过, 2:失败
                result.TestTime = DateTime.Now;
                result.ResultText = result.TestResultStatus == 1 ? "通过" : (result.TestResultStatus == 2 ? "失败" : "未测试");
            }

            RaisePropertyChanged(nameof(TestResults));
            
            // 更新点位统计数据
            UpdatePointStatistics();
        }

        private void Retest(TestResult result)
        {
            if (result == null) return;

            // 复测逻辑实现
            Message = $"复测 {result.TestId}";

            // 模拟复测：随机生成新的测试结果
            Random random = new Random();
            result.TestResultStatus = random.Next(1, 3); // 1:通过, 2:失败
            result.TestTime = DateTime.Now;
            result.ResultText = result.TestResultStatus == 1 ? "通过" : "失败";

            RaisePropertyChanged(nameof(TestResults));
            
            // 更新点位统计数据
            UpdatePointStatistics();
        }

        private void MoveUp(ChannelMapping channel)
        {
            if (channel == null) return;

            // 获取当前显示的通道集合
            int index = CurrentChannels.IndexOf(channel);

            if (index > 0)
            {
                CurrentChannels.Move(index, index - 1);
            }
        }

        private void MoveDown(ChannelMapping channel)
        {
            if (channel == null) return;

            // 获取当前显示的通道集合
            int index = CurrentChannels.IndexOf(channel);

            if (index < CurrentChannels.Count - 1)
            {
                CurrentChannels.Move(index, index + 1);
            }
        }

        private ObservableCollection<ChannelMapping> GetCollectionByChannel(ChannelMapping channel)
        {
            switch (channel.ModuleType?.ToLower())
            {
                case "ai": return AIChannels;
                case "ao": return AOChannels;
                case "di": return DIChannels;
                case "do": return DOChannels;
                default: return null;
            }
        }

        #endregion

        #region 测试数据初始化

        //private void InitializeTestData()
        //{
        //    // 初始化通道映射数据
        //    AIChannels = new ObservableCollection<ChannelMapping>();
        //    for (int i = 0; i < 1000; i++)
        //    {
        //        AIChannels.Add(new ChannelMapping() { TestChannel = $"AI-00{i}", TargetChannel = $"AI-10{i}", ChannelType = "AI", SignalType = "4-20mA" });
        //    }
        //    AOChannels = new ObservableCollection<ChannelMapping>();
        //    for (int i = 0; i < 1000; i++)
        //    {
        //        AOChannels.Add(new ChannelMapping() { TestChannel = $"AO-00{i}", TargetChannel = $"AO-10{i}", ChannelType = "AO", SignalType = "4-20mA" });
        //    }
        //    DIChannels = new ObservableCollection<ChannelMapping>();
        //    for (int i = 0; i < 1000; i++)
        //    {
        //        DIChannels.Add(new ChannelMapping() { TestChannel = $"DI-00{i}", TargetChannel = $"DI-10{i}", ChannelType = "DI", SignalType = "4-20mA" });
        //    }

        //    DOChannels = new ObservableCollection<ChannelMapping>();
        //    for (int i = 0; i < 1000; i++)
        //    {
        //        DOChannels.Add(new ChannelMapping() { TestChannel = $"DO-00{i}", TargetChannel = $"DO-10{i}", ChannelType = "DO", SignalType = "4-20mA" });
        //    }

        //    // 初始化测试结果数据（为所有4类各5个通道创建测试项）
        //    TestResults = new ObservableCollection<TestResult>();
            
        //    // 添加基本测试结果
        //    InitializeBasicTestResults();
        //}
        
        //private void InitializeBasicTestResults()
        //{
        //    int id = 1;
        //    Random random = new Random();
            
        //    // 为每个通道添加测试结果项
        //    foreach (var channel in AIChannels.Concat(AOChannels).Concat(DIChannels).Concat(DOChannels))
        //    {
        //        // 根据通道类型设置不同数值范围
        //        double minRange = 0;
        //        double maxRange = 100;
                
        //        if (channel.ChannelType == "AI" || channel.ChannelType == "AO")
        //        {
        //            if (channel.SignalType == "4-20mA")
        //            {
        //                minRange = 4;
        //                maxRange = 20;
        //            }
        //            else if (channel.SignalType == "0-10V")
        //            {
        //                minRange = 0;
        //                maxRange = 10;
        //            }
        //        }
                
        //        // 创建测试结果对象并添加到集合
        //        TestResults.Add(new TestResult
        //        {
        //            TestId = id++,
        //            ResultText = "未测试",
        //            TestResultStatus = 0, // 0:未测试
        //            TestTime = null,
                    
        //            // 新增字段赋值
        //            BatchName = "2023年第一批次",
        //            VariableName = $"VAR_{channel.ChannelType}_{id:D3}",
        //            PointType = channel.ChannelType,
        //            TestPlcChannel = channel.TestChannel,
        //            TargetPlcChannel = channel.TargetChannel,
        //            RangeMin = minRange,
        //            RangeMax = maxRange,
        //            Value0Percent = Math.Round(minRange, 2),
        //            Value25Percent = Math.Round(minRange + (maxRange - minRange) * 0.25, 2),
        //            Value50Percent = Math.Round(minRange + (maxRange - minRange) * 0.5, 2),
        //            Value75Percent = Math.Round(minRange + (maxRange - minRange) * 0.75, 2),
        //            Value100Percent = Math.Round(maxRange, 2),
        //            LowLowAlarmStatus = channel.ChannelType.StartsWith("D") ? "正常" : "N/A",
        //            LowAlarmStatus = channel.ChannelType.StartsWith("D") ? "正常" : "N/A",
        //            HighAlarmStatus = channel.ChannelType.StartsWith("D") ? "正常" : "N/A",
        //            HighHighAlarmStatus = channel.ChannelType.StartsWith("D") ? "正常" : "N/A",
        //            MaintenanceFunction = "已检测"
        //        });
        //    }
            
        //    // 更新点位统计数据
        //    UpdatePointStatistics();
        //}
        
        private void InitializeBatchData()
        {
            // 初始化批次数据（5个批次，每个批次20条数据）
            Batches = new ObservableCollection<BatchInfo>
            {
                new BatchInfo
                {
                    BatchId = "B001",
                    BatchName = "2023年第一批次",
                    CreationDate = new DateTime(2023, 1, 15),
                    ItemCount = 20,
                    Status = "完成"
                },
                new BatchInfo
                {
                    BatchId = "B002",
                    BatchName = "2023年第二批次",
                    CreationDate = new DateTime(2023, 3, 20),
                    ItemCount = 20,
                    Status = "完成"
                },
                new BatchInfo
                {
                    BatchId = "B003",
                    BatchName = "2023年第三批次",
                    CreationDate = new DateTime(2023, 6, 5),
                    ItemCount = 20,
                    Status = "已取消"
                },
                new BatchInfo
                {
                    BatchId = "B004",
                    BatchName = "2023年第四批次",
                    CreationDate = new DateTime(2023, 9, 12),
                    ItemCount = 20,
                    Status = "进行中"
                },
                new BatchInfo
                {
                    BatchId = "B005",
                    BatchName = "2023年第五批次",
                    CreationDate = new DateTime(2023, 12, 1),
                    ItemCount = 20,
                    Status = "未开始"
                }
            };
            
            // 默认选择第一个批次
            if (Batches.Count > 0)
                SelectedBatch = Batches[0];
                
            // 为了满足100条数据的需求，我们已经创建了20个通道（4类*5个），
            // 每个批次20条，共5个批次，所以已经有100条测试数据
        }

        // 计算并更新点位统计数据
        private void UpdatePointStatistics()
        {
            if (TestResults == null) return;
            TotalPointCount = $"全部点位数量:{TestResults.Count}";
            TestedPointCount = $"已测试点位数量:{TestResults.Count(r => r.TestResultStatus > 0)}";
            WaitingPointCount = $"待测试点位数量:{TestResults.Count(r => r.TestResultStatus == 0)}";
            SuccessPointCount = $"成功点位数量:{TestResults.Count(r => r.TestResultStatus == 1)}";
            FailurePointCount = $"失败点位数量:{TestResults.Count(r => r.TestResultStatus == 2)}";
        }

        // 添加过滤逻辑方法
        private void ApplyResultFilter()
        {
            if (string.IsNullOrEmpty(SelectedResultFilter) || TestResults == null)
                return;

            // 这里根据选择的过滤条件进行过滤
            // 可以使用CollectionViewSource来实现或者直接在界面上使用ICollectionView
            // 这是一个简单的示例
            // 在真实场景中，你可能需要更复杂的逻辑来实现数据的实际过滤
            switch (SelectedResultFilter)
            {
                case "全部":
                    // 不需要特殊处理，显示所有结果
                    break;
                case "通过":
                    // 只显示通过的测试结果
                    // 可以设置一个过滤后的集合或者使用CollectionViewSource
                    break;
                case "失败":
                    // 只显示失败的测试结果
                    break;
                case "未测试":
                    // 只显示未测试的结果
                    break;
                default:
                    break;
            }

            // 更新统计数据
            UpdatePointStatistics();
        }

        #endregion

        /// <summary>
        /// 通道自动分配的方法，根据测试PLC配置为被测PLC通道分配测试PLC通道和批次
        /// </summary>
        private async void AllocateChannelsAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "正在分配通道...";

                // 获取测试PLC配置
                var testPlcConfig = await _channelMappingService.GetTestPlcConfigAsync();

                // 如果配置为空，则创建默认配置
                if (testPlcConfig == null)
                {
                    testPlcConfig = new TestPlcConfig
                    {
                        BrandType = PlcBrandTypeEnum.Micro850,
                        IpAddress = "192.168.1.1",
                        CommentsTables = new List<ComparisonTable>()
                    };
                }

                // 执行通道分配
                var result = await _channelMappingService.AllocateChannelsAsync(
                    AIChannels, AOChannels, DIChannels, DOChannels, testPlcConfig);

                // 更新通道数据
                AIChannels = new ObservableCollection<ChannelMapping>(result.AI);
                AOChannels = new ObservableCollection<ChannelMapping>(result.AO);
                DIChannels = new ObservableCollection<ChannelMapping>(result.DI);
                DOChannels = new ObservableCollection<ChannelMapping>(result.DO);

                // 更新当前显示的通道集合
                UpdateCurrentChannels();

                // 更新测试结果中的通道信息
                UpdateTestResultChannels(result.AI,result.AO,result.DI,result.DO);

                Message = "通道分配完成";
                StatusMessage = string.Empty;
            }
            catch (Exception ex)
            {
                StatusMessage = string.Empty;
                MessageBox.Show($"通道分配失败: {ex.Message}", "操作失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 更新测试结果中的通道信息，与通道映射保持一致
        /// </summary>
        private void UpdateTestResultChannels(IEnumerable<ChannelMapping> AI, IEnumerable<ChannelMapping> AO, IEnumerable<ChannelMapping> DI, IEnumerable<ChannelMapping> DO)
        {
            // 将所有类型的通道合并为一个列表
            var allChannels = AI.Concat(AO).Concat(DI).Concat(DO).ToList();
            // 更新测试结果中的通道信息
            foreach (var result in TestResults)
            {
                // 查找对应的通道映射
                var mapping = allChannels.FirstOrDefault(c=>c.VariableName.Equals(result.VariableName));
                if (mapping != null)
                {
                    result.TestPlcChannel = mapping.TestPLCChannelTag;
                    result.BatchName = mapping.TestBatch;
                }
            }

            // 通知UI更新
            RaisePropertyChanged(nameof(TestResults));
        }

        /// <summary>
        /// 清除所有通道分配信息
        /// </summary>
        private async void ClearChannelAllocationsAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "正在清除通道分配信息...";

                // 清除AI通道分配
                AIChannels = new ObservableCollection<ChannelMapping>(
                    await _channelMappingService.ClearAllChannelAllocationsAsync(AIChannels));

                // 清除AO通道分配
                AOChannels = new ObservableCollection<ChannelMapping>(
                    await _channelMappingService.ClearAllChannelAllocationsAsync(AOChannels));

                // 清除DI通道分配
                DIChannels = new ObservableCollection<ChannelMapping>(
                    await _channelMappingService.ClearAllChannelAllocationsAsync(DIChannels));

                // 清除DO通道分配
                DOChannels = new ObservableCollection<ChannelMapping>(
                    await _channelMappingService.ClearAllChannelAllocationsAsync(DOChannels));

                // 更新当前显示的通道集合
                UpdateCurrentChannels();

                // 更新测试结果中的通道信息
                foreach (var result in TestResults)
                {
                    result.TestPlcChannel = string.Empty;
                    result.BatchName = string.Empty;
                }

                // 通知UI更新
                RaisePropertyChanged(nameof(TestResults));

                Message = "通道分配信息已清除";
                StatusMessage = string.Empty;
            }
            catch (Exception ex)
            {
                StatusMessage = string.Empty;
                MessageBox.Show($"清除通道分配信息失败: {ex.Message}", "操作失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 更新单个通道的映射关系
        /// </summary>
        /// <param name="targetChannel">目标通道</param>
        /// <param name="newTestPlcChannel">新的测试PLC通道标识</param>
        /// <param name="newTestPlcCommAddress">新的测试PLC通讯地址</param>
        /// <returns>更新操作是否成功</returns>
        public async Task<bool> UpdateChannelMappingAsync(ChannelMapping targetChannel, string newTestPlcChannel, string newTestPlcCommAddress)
        {
            try
            {
                if (targetChannel == null)
                    return false;

                IsLoading = true;
                StatusMessage = "正在更新通道映射...";

                // 获取所有通道
                var allChannels = AIChannels.Concat(AOChannels).Concat(DIChannels).Concat(DOChannels);

                // 调用服务更新通道映射
                await _channelMappingService.UpdateChannelMappingAsync(targetChannel, newTestPlcChannel, newTestPlcCommAddress, allChannels);

                // 更新测试结果中的通道信息
                UpdateTestResultChannels(AIChannels, AOChannels,DIChannels,DOChannels);

                StatusMessage = string.Empty;
                return true;
            }
            catch (Exception ex)
            {
                StatusMessage = string.Empty;
                MessageBox.Show($"更新通道映射失败: {ex.Message}", "操作失败", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
    
    // 批次信息类
    public class BatchInfo
    {
        public string BatchId { get; set; }
        public string BatchName { get; set; }
        public DateTime CreationDate { get; set; }
        public int ItemCount { get; set; }
        public string Status { get; set; }
    }
}
