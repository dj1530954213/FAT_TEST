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
using FatFullVersion.Services;

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

        // 所有通道的合并集合，用于内部操作
        private ObservableCollection<ChannelMapping> _allChannels;
        public ObservableCollection<ChannelMapping> AllChannels
        {
            get { return _allChannels; }
            set { SetProperty(ref _allChannels, value); }
        }

        // 测试结果数据
        private ObservableCollection<ChannelMapping> _testResults;
        public ObservableCollection<ChannelMapping> TestResults
        {
            get { return _testResults; }
            set
            {
                SetProperty(ref _testResults, value);
                //修改测试结果数据集时调用此方法更新统计数据
                UpdatePointStatistics();
            }
        }

        private ChannelMapping _currentTestResult;
        /// <summary>
        /// 当前测试结果
        /// </summary>
        public ChannelMapping CurrentTestResult
        {
            get { return _currentTestResult; }
            set 
            { 
                if (_currentTestResult != value)
                {
                    _currentTestResult = value;
                    System.Diagnostics.Debug.WriteLine($"CurrentTestResult changed: {_currentTestResult?.VariableName}, HardPointTestResult: {_currentTestResult?.HardPointTestResult}");
                    RaisePropertyChanged(nameof(CurrentTestResult)); 
                }
            }
        }

        // 批次相关数据
        private ObservableCollection<BatchInfo> _batches;
        public ObservableCollection<BatchInfo> Batches
        {
            get { return _batches; }
            set { SetProperty(ref _batches, value); }
        }

        //选择的当前批次信息
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
        public DelegateCommand<ChannelMapping> RetestCommand { get; private set; }
        public DelegateCommand<ChannelMapping> MoveUpCommand { get; private set; }
        public DelegateCommand<ChannelMapping> MoveDownCommand { get; private set; }
        public DelegateCommand ConfirmBatchSelectionCommand { get; private set; }
        public DelegateCommand CancelBatchSelectionCommand { get; private set; }
        public DelegateCommand AllocateChannelsCommand { get; private set; }
        public DelegateCommand ClearChannelAllocationsCommand { get; private set; }

        // 添加原始通道集合属性
        private ObservableCollection<ChannelMapping> _originalAIChannels;
        private ObservableCollection<ChannelMapping> _originalAOChannels;
        private ObservableCollection<ChannelMapping> _originalDIChannels;
        private ObservableCollection<ChannelMapping> _originalDOChannels;

        /// <summary>
        /// AI手动测试窗口是否打开
        /// </summary>
        private bool _isAIManualTestOpen;
        public bool IsAIManualTestOpen
        {
            get => _isAIManualTestOpen;
            set => SetProperty(ref _isAIManualTestOpen, value);
        }

        /// <summary>
        /// DI手动测试窗口是否打开
        /// </summary>
        private bool _isDIManualTestOpen;
        public bool IsDIManualTestOpen
        {
            get => _isDIManualTestOpen;
            set => SetProperty(ref _isDIManualTestOpen, value);
        }

        /// <summary>
        /// AI设定值
        /// </summary>
        private string _aiSetValue;
        public string AISetValue
        {
            get => _aiSetValue;
            set => SetProperty(ref _aiSetValue, value);
        }

        /// <summary>
        /// 当前选中的通道
        /// </summary>
        private ChannelMapping _selectedChannel;
        public ChannelMapping SelectedChannel
        {
            get => _selectedChannel;
            set => SetProperty(ref _selectedChannel, value);
        }

        /// <summary>
        /// 打开手动测试窗口命令
        /// </summary>
        public DelegateCommand<ChannelMapping> OpenAIManualTestCommand { get; private set; }
        public DelegateCommand<ChannelMapping> OpenDIManualTestCommand { get; private set; }
        public DelegateCommand<ChannelMapping> OpenDOManualTestCommand { get; private set; }
        public DelegateCommand<ChannelMapping> OpenAOManualTestCommand { get; private set; }
        public DelegateCommand CloseAIManualTestCommand { get; private set; }
        public DelegateCommand CloseDIManualTestCommand { get; private set; }
        public DelegateCommand CloseDOManualTestCommand { get; private set; }
        public DelegateCommand CloseAOManualTestCommand { get; private set; }

        /// <summary>
        /// 发送AI测试值命令
        /// </summary>
        public DelegateCommand SendAITestValueCommand { get; private set; }

        /// <summary>
        /// 确认AI值命令
        /// </summary>
        public DelegateCommand<ChannelMapping> ConfirmAIValueCommand { get; private set; }

        /// <summary>
        /// 发送AI高报命令
        /// </summary>
        public DelegateCommand SendAIHighAlarmCommand { get; private set; }

        /// <summary>
        /// 复位AI高报命令
        /// </summary>
        public DelegateCommand ResetAIHighAlarmCommand { get; private set; }

        /// <summary>
        /// 确认AI高报命令
        /// </summary>
        public DelegateCommand<ChannelMapping> ConfirmAIHighAlarmCommand { get; private set; }

        /// <summary>
        /// 发送AI低报命令
        /// </summary>
        public DelegateCommand SendAILowAlarmCommand { get; private set; }

        /// <summary>
        /// 复位AI低报命令
        /// </summary>
        public DelegateCommand ResetAILowAlarmCommand { get; private set; }

        /// <summary>
        /// 确认AI低报命令
        /// </summary>
        public DelegateCommand<ChannelMapping> ConfirmAILowAlarmCommand { get; private set; }

        /// <summary>
        /// 发送AI维护功能命令
        /// </summary>
        public DelegateCommand SendAIMaintenanceCommand { get; private set; }

        /// <summary>
        /// 复位AI维护功能命令
        /// </summary>
        public DelegateCommand ResetAIMaintenanceCommand { get; private set; }

        /// <summary>
        /// 确认AI维护功能命令
        /// </summary>
        public DelegateCommand<ChannelMapping> ConfirmAIMaintenanceCommand { get; private set; }

        /// <summary>
        /// 发送DI测试命令
        /// </summary>
        public DelegateCommand SendDITestCommand { get; private set; }

        /// <summary>
        /// 复位DI命令
        /// </summary>
        public DelegateCommand ResetDICommand { get; private set; }

        /// <summary>
        /// 确认DI命令
        /// </summary>
        public DelegateCommand<ChannelMapping> ConfirmDICommand { get; private set; }

        private bool _isDOManualTestOpen;
        public bool IsDOManualTestOpen
        {
            get { return _isDOManualTestOpen; }
            set { SetProperty(ref _isDOManualTestOpen, value); }
        }

        private bool _isAOManualTestOpen;
        public bool IsAOManualTestOpen
        {
            get { return _isAOManualTestOpen; }
            set { SetProperty(ref _isAOManualTestOpen, value); }
        }

        // 监测状态
        private string _diMonitorStatus = "请开始监测";
        public string DIMonitorStatus
        {
            get { return _diMonitorStatus; }
            set { SetProperty(ref _diMonitorStatus, value); }
        }

        private string _doMonitorStatus = "请开始监测";
        public string DOMonitorStatus
        {
            get { return _doMonitorStatus; }
            set { SetProperty(ref _doMonitorStatus, value); }
        }

        private string _aoMonitorStatus = "请开始监测";
        public string AOMonitorStatus
        {
            get { return _aoMonitorStatus; }
            set { SetProperty(ref _aoMonitorStatus, value); }
        }

        // 当前值
        private string _diCurrentValue;
        public string DICurrentValue
        {
            get { return _diCurrentValue; }
            set { SetProperty(ref _diCurrentValue, value); }
        }

        private string _doCurrentValue;
        public string DOCurrentValue
        {
            get { return _doCurrentValue; }
            set { SetProperty(ref _doCurrentValue, value); }
        }

        private string _aoCurrentValue;
        public string AOCurrentValue
        {
            get { return _aoCurrentValue; }
            set { SetProperty(ref _aoCurrentValue, value); }
        }

        // 命令
        public DelegateCommand<ChannelMapping> StartDOMonitorCommand { get; private set; }
        public DelegateCommand<ChannelMapping> ConfirmDOCommand { get; private set; }

        public DelegateCommand<ChannelMapping> StartAOMonitorCommand { get; private set; }
        public DelegateCommand<ChannelMapping> ConfirmAOCommand { get; private set; }

        private ChannelMapping _currentChannel;
        public ChannelMapping CurrentChannel
        {
            get => _currentChannel;
            set => SetProperty(ref _currentChannel, value);
        }

        // 添加原始通道集合属性
        private ObservableCollection<ChannelMapping> _originalAllChannels;

        #endregion

        public DataEditViewModel(IPointDataService pointDataService, IChannelMappingService channelMappingService)
        {
            _pointDataService = pointDataService;
            _channelMappingService = channelMappingService;

            // 初始化命令
            ImportConfigCommand = new DelegateCommand(ImportConfig);
            SelectBatchCommand = new DelegateCommand(SelectBatch);
            FinishWiringCommand = new DelegateCommand(FinishWiring);
            StartTestCommand = new DelegateCommand(StartTest);
            RetestCommand = new DelegateCommand<ChannelMapping>(Retest);
            MoveUpCommand = new DelegateCommand<ChannelMapping>(ExecuteMoveUp);
            MoveDownCommand = new DelegateCommand<ChannelMapping>(ExecuteMoveDown);
            ConfirmBatchSelectionCommand = new DelegateCommand(ConfirmBatchSelection);
            CancelBatchSelectionCommand = new DelegateCommand(CancelBatchSelection);
            AllocateChannelsCommand = new DelegateCommand(ExecuteAllocateChannels);
            ClearChannelAllocationsCommand = new DelegateCommand(ClearChannelAllocationsAsync);

            // 初始化手动测试相关命令
            OpenAIManualTestCommand = new DelegateCommand<ChannelMapping>(ExecuteOpenAIManualTest);
            OpenDIManualTestCommand = new DelegateCommand<ChannelMapping>(ExecuteOpenDIManualTest);
            OpenDOManualTestCommand = new DelegateCommand<ChannelMapping>(ExecuteOpenDOManualTest);
            OpenAOManualTestCommand = new DelegateCommand<ChannelMapping>(ExecuteOpenAOManualTest);
            CloseAIManualTestCommand = new DelegateCommand(ExecuteCloseAIManualTest);
            CloseDIManualTestCommand = new DelegateCommand(ExecuteCloseDIManualTest);
            CloseDOManualTestCommand = new DelegateCommand(ExecuteCloseDOManualTest);
            CloseAOManualTestCommand = new DelegateCommand(ExecuteCloseAOManualTest);

            // AI手动测试命令
            SendAITestValueCommand = new DelegateCommand(ExecuteSendAITestValue);
            ConfirmAIValueCommand = new DelegateCommand<ChannelMapping>(ExecuteConfirmAIValue);
            SendAIHighAlarmCommand = new DelegateCommand(ExecuteSendAIHighAlarm);
            ResetAIHighAlarmCommand = new DelegateCommand(ExecuteResetAIHighAlarm);
            ConfirmAIHighAlarmCommand = new DelegateCommand<ChannelMapping>(ExecuteConfirmAIHighAlarm);
            SendAILowAlarmCommand = new DelegateCommand(ExecuteSendAILowAlarm);
            ResetAILowAlarmCommand = new DelegateCommand(ExecuteResetAILowAlarm);
            ConfirmAILowAlarmCommand = new DelegateCommand<ChannelMapping>(ExecuteConfirmAILowAlarm);
            SendAIMaintenanceCommand = new DelegateCommand(ExecuteSendAIMaintenance);
            ResetAIMaintenanceCommand = new DelegateCommand(ExecuteResetAIMaintenance);
            ConfirmAIMaintenanceCommand = new DelegateCommand<ChannelMapping>(ExecuteConfirmAIMaintenance);

            // DI手动测试命令
            SendDITestCommand = new DelegateCommand(ExecuteSendDITest);
            ResetDICommand = new DelegateCommand(ExecuteResetDI);
            ConfirmDICommand = new DelegateCommand<ChannelMapping>(ExecuteConfirmDI);

            // DO手动测试命令
            StartDOMonitorCommand = new DelegateCommand<ChannelMapping>(ExecuteStartDOMonitor);
            ConfirmDOCommand = new DelegateCommand<ChannelMapping>(ExecuteConfirmDO);

            // AO手动测试命令
            StartAOMonitorCommand = new DelegateCommand<ChannelMapping>(ExecuteStartAOMonitor);
            ConfirmAOCommand = new DelegateCommand<ChannelMapping>(ExecuteConfirmAO);

            // 初始化集合
            AIChannels = new ObservableCollection<ChannelMapping>();
            AOChannels = new ObservableCollection<ChannelMapping>();
            DIChannels = new ObservableCollection<ChannelMapping>();
            DOChannels = new ObservableCollection<ChannelMapping>();
            AllChannels = new ObservableCollection<ChannelMapping>();
            CurrentChannels = new ObservableCollection<ChannelMapping>();
            TestResults = new ObservableCollection<ChannelMapping>();
            Batches = new ObservableCollection<BatchInfo>();

            // 初始化其他属性
            SelectedChannelType = "AI通道";
            IsBatchSelectionOpen = false;
            SelectedResultFilter = "全部";
            TotalPointCount = "0";
            TestedPointCount = "0";
            WaitingPointCount = "0";
            SuccessPointCount = "0";
            FailurePointCount = "0";
        }
        /// <summary>
        /// 选择批次后将选择的批次信息放置在当前的显示区域中
        /// </summary>
        private void UpdateCurrentChannels()
        {
            if (string.IsNullOrEmpty(SelectedChannelType) || AllChannels == null)
                return;

            switch (SelectedChannelType)
            {
                case "AI通道":
                    CurrentChannels = new ObservableCollection<ChannelMapping>(
                        AllChannels.Where(c => c.ModuleType?.ToLower() == "ai"));
                    break;
                case "AO通道":
                    CurrentChannels = new ObservableCollection<ChannelMapping>(
                        AllChannels.Where(c => c.ModuleType?.ToLower() == "ao"));
                    break;
                case "DI通道":
                    CurrentChannels = new ObservableCollection<ChannelMapping>(
                        AllChannels.Where(c => c.ModuleType?.ToLower() == "di"));
                    break;
                case "DO通道":
                    CurrentChannels = new ObservableCollection<ChannelMapping>(
                        AllChannels.Where(c => c.ModuleType?.ToLower() == "do"));
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
                    
                    // 清空原始通道集合引用
                    _originalAIChannels = null;
                    _originalAOChannels = null;
                    _originalDIChannels = null;
                    _originalDOChannels = null;
                    
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
                IsLoading = true;
                StatusMessage = "正在处理导入数据...";

                // 分类存储各类点表
                var aiPoints = importedData.Where(p => p.ModuleType?.ToLower() == "ai").ToList();
                var aoPoints = importedData.Where(p => p.ModuleType?.ToLower() == "ao").ToList();
                var diPoints = importedData.Where(p => p.ModuleType?.ToLower() == "di").ToList();
                var doPoints = importedData.Where(p => p.ModuleType?.ToLower() == "do").ToList();

                // 清空现有通道数据
                AIChannels.Clear();
                AOChannels.Clear();
                DIChannels.Clear();
                DOChannels.Clear();
                TestResults.Clear();

                // 添加AI通道
                foreach (var point in aiPoints)
                {
                    var channel = new ChannelMapping
                    {
                        ChannelTag = point.ChannelTag,
                        VariableName = point.VariableName,
                        ModuleType = point.ModuleType,
                        // 设置通道映射的其他属性
                        SLLSetValue = point.SLLSetValue,
                        SLLSetValueNumber = point.SLLSetValueNumber,
                        SLSetValue = point.SLSetValue,
                        SLSetValueNumber = point.SLSetValueNumber,
                        SHSetValue = point.SHSetValue,
                        SHSetValueNumber = point.SHSetValueNumber,
                        SHHSetValue = point.SHHSetValue,
                        SHHSetValueNumber = point.SHHSetValueNumber
                    };
                    AIChannels.Add(channel);
                    AllChannels.Add(channel);

                    // 创建对应的测试结果
                    var result = new ChannelMapping
                    {
                        TestId = TestResults.Count + 1,
                        VariableName = point.VariableName,
                        ModuleType = point.ModuleType,
                        DataType = point.DataType,
                        ChannelTag = point.ChannelTag,
                        RangeLowerLimitValue = (float)point.LowLowLimit,
                        RangeUpperLimitValue = (float)point.HighHighLimit,
                        // AI点位不初始化百分比值，这些值将在测试过程中填充
                        TestResultStatus = 0, // 未测试
                        ResultText = "未测试"
                    };
                    TestResults.Add(result);
                }

                // 添加AO通道
                foreach (var point in aoPoints)
                {
                    var channel = new ChannelMapping
                    {
                        ChannelTag = point.ChannelTag,
                        VariableName = point.VariableName,
                        ModuleType = point.ModuleType,
                        // 设置通道映射的其他属性
                        SLLSetValue = point.SLLSetValue,
                        SLLSetValueNumber = point.SLLSetValueNumber,
                        SLSetValue = point.SLSetValue,
                        SLSetValueNumber = point.SLSetValueNumber,
                        SHSetValue = point.SHSetValue,
                        SHSetValueNumber = point.SHSetValueNumber,
                        SHHSetValue = point.SHHSetValue,
                        SHHSetValueNumber = point.SHHSetValueNumber
                    };
                    AOChannels.Add(channel);
                    AllChannels.Add(channel);

                    // 创建对应的测试结果
                    var result = new ChannelMapping
                    {
                        TestId = TestResults.Count + 1,
                        VariableName = point.VariableName,
                        ModuleType = point.ModuleType,
                        DataType = point.DataType,
                        ChannelTag = point.ChannelTag,
                        RangeLowerLimitValue = (float)point.LowLowLimit,
                        RangeUpperLimitValue = (float)point.HighHighLimit,
                        // AO点位不初始化百分比值，这些值将在测试过程中填充
                        TestResultStatus = 0, // 未测试
                        ResultText = "未测试"
                    };
                    TestResults.Add(result);
                }

                // 添加DI通道
                foreach (var point in diPoints)
                {
                    var channel = new ChannelMapping
                    {
                        ChannelTag = point.ChannelTag,
                        VariableName = point.VariableName,
                        ModuleType = point.ModuleType
                    };
                    DIChannels.Add(channel);
                    AllChannels.Add(channel);

                    // 创建对应的测试结果
                    var result = new ChannelMapping
                    {
                        TestId = TestResults.Count + 1,
                        VariableName = point.VariableName,
                        ModuleType = point.ModuleType,
                        DataType = point.DataType,
                        ChannelTag = point.ChannelTag,
                        TestResultStatus = 0, // 未测试
                        ResultText = "未测试",
                        // 为DI点位设置NaN值
                        RangeLowerLimitValue = float.NaN,
                        RangeUpperLimitValue = float.NaN, 
                        Value0Percent = double.NaN,
                        Value25Percent = double.NaN,
                        Value50Percent = double.NaN,
                        Value75Percent = double.NaN,
                        Value100Percent = double.NaN,
                        LowLowAlarmStatus = "N/A",
                        LowAlarmStatus = "N/A",
                        HighAlarmStatus = "N/A",
                        HighHighAlarmStatus = "N/A",
                        MaintenanceFunction = "N/A"
                    };
                    TestResults.Add(result);
                }

                // 添加DO通道
                foreach (var point in doPoints)
                {
                    var channel = new ChannelMapping
                    {
                        ChannelTag = point.ChannelTag,
                        VariableName = point.VariableName,
                        ModuleType = point.ModuleType
                    };
                    DOChannels.Add(channel);
                    AllChannels.Add(channel);

                    // 创建对应的测试结果
                    var result = new ChannelMapping
                    {
                        TestId = TestResults.Count + 1,
                        VariableName = point.VariableName,
                        ModuleType = point.ModuleType,
                        DataType = point.DataType,
                        ChannelTag = point.ChannelTag,
                        TestResultStatus = 0, // 未测试
                        ResultText = "未测试",
                        // 为DO点位设置NaN值
                        RangeLowerLimitValue = float.NaN,
                        RangeUpperLimitValue = float.NaN, 
                        Value0Percent = double.NaN,
                        Value25Percent = double.NaN,
                        Value50Percent = double.NaN,
                        Value75Percent = double.NaN,
                        Value100Percent = double.NaN,
                        LowLowAlarmStatus = "N/A",
                        LowAlarmStatus = "N/A",
                        HighAlarmStatus = "N/A",
                        HighHighAlarmStatus = "N/A",
                        MaintenanceFunction = "N/A"
                    };
                    TestResults.Add(result);
                }
                //当Excel中点位解析完成后并且已经初始化完ChannelMapping后调用自动分配程序分配点位
                var channelsMappingResult = await _channelMappingService.AllocateChannelsTestAsync(AIChannels, AOChannels, DIChannels, DOChannels, TestResults);
                //通道分配完成之后同步更新结果表位中的对应数据
                _channelMappingService.SyncChannelAllocation(channelsMappingResult.AI, channelsMappingResult.AO, channelsMappingResult.DI,
                    channelsMappingResult.DO, TestResults);
                //通知前端页面更新数据
                RaisePropertyChanged(nameof(TestResults));

                // 更新当前显示的通道
                UpdateCurrentChannels();

                // 更新点位统计数据
                UpdatePointStatistics();

                Message = $"已导入 {importedData.Count()} 条数据";
                StatusMessage = string.Empty;
            }
            catch (Exception ex)
            {
                StatusMessage = string.Empty;
                Message = $"导入数据处理错误: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
        /// <summary>
        /// 点击批次选择窗口后执行的逻辑
        /// </summary>
        private async void SelectBatch()
        {
            try
            {
                // 使用原始通道集合来更新批次信息，确保批次列表完整
                if (_originalAIChannels != null && _originalAOChannels != null && 
                    _originalDIChannels != null && _originalDOChannels != null)
                {
                    // 使用通道映射服务提取批次信息
                    Batches = new ObservableCollection<BatchInfo>(
                        await _channelMappingService.ExtractBatchInfoAsync(
                            _originalAIChannels, 
                            _originalAOChannels, 
                            _originalDIChannels, 
                            _originalDOChannels));
                }
                else
                {
                    // 首次使用当前通道集合
                    await UpdateBatchInfoAsync();
                }
                
                // 显示批次选择窗口
                IsBatchSelectionOpen = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"获取批次信息失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        /// <summary>
        /// 确认选择批次信息，同步更新下方的当前批次的通道对应关系
        /// </summary>
        private void ConfirmBatchSelection()
        {
            if (SelectedBatch != null)
            {
                // 关闭批次选择窗口
                IsBatchSelectionOpen = false;

                // 首次保存原始通道集合
                if (_originalAIChannels == null)
                {
                    _originalAIChannels = new ObservableCollection<ChannelMapping>(AIChannels);
                    _originalAOChannels = new ObservableCollection<ChannelMapping>(AOChannels);
                    _originalDIChannels = new ObservableCollection<ChannelMapping>(DIChannels);
                    _originalDOChannels = new ObservableCollection<ChannelMapping>(DOChannels);
                    
                    // 保存原始的合并集合
                    _originalAllChannels = new ObservableCollection<ChannelMapping>(AllChannels);
                }

                // 根据选择的批次筛选相关的测试结果
                var batchResults = TestResults.Where(r => r.TestBatch == SelectedBatch.BatchName).ToList();
                
                // 如果当前批次没有测试结果，可能是因为TestResults中的TestBatch未同步
                // 手动从通道映射中同步一次
                if (batchResults.Count == 0)
                {
                    // 查找匹配批次的通道
                    var batchChannels = AllChannels.Where(c => c.TestBatch == SelectedBatch.BatchName).ToList();
                    
                    // 同步TestBatch到TestResults
                    foreach (var channel in batchChannels)
                    {
                        var result = TestResults.FirstOrDefault(r => 
                            r.VariableName == channel.VariableName && 
                            r.ChannelTag == channel.ChannelTag);
                            
                        if (result != null)
                        {
                            result.TestBatch = channel.TestBatch;
                            result.TestPLCChannelTag = channel.TestPLCChannelTag;
                        }
                    }
                    
                    // 重新获取批次结果
                    batchResults = TestResults.Where(r => r.TestBatch == SelectedBatch.BatchName).ToList();
                }
                
                // 获取当前批次中的所有通道ID
                var batchChannelVariableNames = batchResults.Select(r => r.VariableName).ToHashSet();
                
                // 筛选合并集合中的通道
                AllChannels = new ObservableCollection<ChannelMapping>(
                    _originalAllChannels.Where(c => batchChannelVariableNames.Contains(c.VariableName)));
                
                // 同时更新分类集合，保持UI显示与内部数据一致
                AIChannels = new ObservableCollection<ChannelMapping>(
                    AllChannels.Where(c => c.ModuleType?.ToLower() == "ai"));
                
                AOChannels = new ObservableCollection<ChannelMapping>(
                    AllChannels.Where(c => c.ModuleType?.ToLower() == "ao"));
                
                DIChannels = new ObservableCollection<ChannelMapping>(
                    AllChannels.Where(c => c.ModuleType?.ToLower() == "di"));
                
                DOChannels = new ObservableCollection<ChannelMapping>(
                    AllChannels.Where(c => c.ModuleType?.ToLower() == "do"));
                
                // 更新当前显示的通道
                UpdateCurrentChannels();
                
                // 触发测试结果的更新显示
                RaisePropertyChanged(nameof(TestResults));
                
                Message = $"已选择批次: {SelectedBatch.BatchName}";
            }
            else
            {
                MessageBox.Show("请先选择一个批次", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
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

        private async void StartTest()
        {
            // 开始测试的实现
            Message = "开始测试";

            // 收集当前批次名称
            HashSet<string> affectedBatchNames = new HashSet<string>();

            // 模拟测试，随机生成一些测试结果状态
            Random random = new Random();
            foreach (var result in TestResults)
            {
                result.TestResultStatus = random.Next(0, 3); // 0:未测试, 1:通过, 2:失败
                result.TestTime = DateTime.Now;
                result.ResultText = result.TestResultStatus == 1 ? "通过" : (result.TestResultStatus == 2 ? "失败" : "未测试");
                
                // 记录受影响的批次
                if (!string.IsNullOrEmpty(result.TestBatch))
                {
                    affectedBatchNames.Add(result.TestBatch);
                }
            }

            // 更新批次信息
            await UpdateBatchInfoAsync();

            RaisePropertyChanged(nameof(TestResults));
            
            // 更新点位统计数据
            UpdatePointStatistics();
        }

        private async void Retest(ChannelMapping result)
        {
            if (result == null) return;

            // 复测逻辑实现
            Message = $"复测 {result.TestId}";

            // 模拟复测：随机生成新的测试结果
            Random random = new Random();
            result.TestResultStatus = random.Next(1, 3); // 1:通过, 2:失败
            result.TestTime = DateTime.Now;
            result.ResultText = result.TestResultStatus == 1 ? "通过" : "失败";

            // 更新批次信息
            await UpdateBatchInfoAsync();

            RaisePropertyChanged(nameof(TestResults));
            
            // 更新点位统计数据
            UpdatePointStatistics();
        }
        #endregion        

        #region 测试数据初始化
        /// <summary>
        /// 从测试分配服务中的ChannelMapping提取相关信息形成批次相关信息
        /// </summary>
        /// <returns></returns>
        private async Task UpdateBatchInfoAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "正在更新批次信息...";

                // 同步确保TestResults中的批次信息和PLC通道信息是最新的
                foreach (var channel in AllChannels)
                {
                    if (!string.IsNullOrEmpty(channel.TestBatch))
                    {
                        var result = TestResults.FirstOrDefault(r => 
                            r.VariableName == channel.VariableName && 
                            r.ChannelTag == channel.ChannelTag);
                            
                        if (result != null)
                        {
                            result.TestBatch = channel.TestBatch;
                            result.TestPLCChannelTag = channel.TestPLCChannelTag;
                        }
                    }
                }

                // 使用通道映射服务提取批次信息
                Batches = new ObservableCollection<BatchInfo>(
                    await _channelMappingService.ExtractBatchInfoAsync(
                        AIChannels, 
                        AOChannels, 
                        DIChannels, 
                        DOChannels));

                // 通知UI更新
                RaisePropertyChanged(nameof(Batches));
                RaisePropertyChanged(nameof(TestResults));
                
                StatusMessage = string.Empty;
            }
            catch (Exception ex)
            {
                StatusMessage = string.Empty;
                Message = $"更新批次信息失败: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void InitializeBatchData()
        {
            // 初始化批次数据
            Batches = new ObservableCollection<BatchInfo>();
            
            // 尝试从通道映射信息中提取批次信息
            await UpdateBatchInfoAsync();
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

        #region 手动测试相关按钮

        /// <summary>
        /// 清除所有通道分配信息
        /// </summary>
        private async void ClearChannelAllocationsAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "正在清除通道分配信息...";

                // 清空原始通道集合引用
                _originalAIChannels = null;
                _originalAOChannels = null;
                _originalDIChannels = null;
                _originalDOChannels = null;

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
                //foreach (var result in TestResults)
                //{
                //    result.TestPlcChannel = string.Empty;
                //    result.BatchName = string.Empty;
                //}

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
        /// 执行打开手动测试窗口
        /// </summary>
        private void ExecuteOpenAIManualTest(ChannelMapping channel)
        {
            if (channel == null) return;
            
            // 设置当前通道
            CurrentChannel = channel;
            
            // 输出调试信息
            System.Diagnostics.Debug.WriteLine($"打开AI手动测试窗口: {channel.VariableName}");
            
            // 初始化或获取测试结果
            InitializeTestResult(channel);
            
            // 设置界面初始状态
            AISetValue = "0.0";
            
            // 打开窗口
            IsAIManualTestOpen = true;
        }

        /// <summary>
        /// 执行关闭AI手动测试窗口
        /// </summary>
        private void ExecuteCloseAIManualTest()
        {
            IsAIManualTestOpen = false;
        }

        /// <summary>
        /// 执行关闭DI手动测试窗口
        /// </summary>
        private void ExecuteCloseDIManualTest()
        {
            IsDIManualTestOpen = false;
        }

        /// <summary>
        /// 执行发送AI测试值
        /// </summary>
        private void ExecuteSendAITestValue()
        {
            // TODO: 实现发送AI测试值的逻辑
        }

        /// <summary>
        /// 执行确认AI值
        /// </summary>
        private void ExecuteConfirmAIValue(ChannelMapping channel)
        {
            // 确保我们有一个通道
            if (channel == null && CurrentChannel != null)
            {
                channel = CurrentChannel;
            }
            
            if (channel == null)
            {
                System.Diagnostics.Debug.WriteLine("ExecuteConfirmAIValue: 通道为null");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"ExecuteConfirmAIValue: 确认通道 {channel.VariableName}");
            
            // 设置当前通道状态为已通过
            channel.MonitorStatus = "已通过";
            
            // 确保我们有一个测试结果
            if (CurrentTestResult == null)
            {
                InitializeTestResult(channel);
            }
            
            if (CurrentTestResult != null)
            {
                CurrentTestResult.HardPointTestResult = "已通过";
                CurrentTestResult.ResultText = "AI硬点测试通过";
                CurrentTestResult.TestResultStatus = 1; // 成功
                CurrentTestResult.TestTime = DateTime.Now;
                
                // 通知UI更新
                RaisePropertyChanged(nameof(CurrentTestResult));
                
                // 更新统计数据
                UpdatePointStatistics();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("ExecuteConfirmAIValue: CurrentTestResult为null");
            }
        }

        /// <summary>
        /// 执行发送AI高报
        /// </summary>
        private void ExecuteSendAIHighAlarm()
        {
            // TODO: 实现发送AI高报的逻辑
        }

        /// <summary>
        /// 执行复位AI高报
        /// </summary>
        private void ExecuteResetAIHighAlarm()
        {
            // TODO: 实现复位AI高报的逻辑
        }

        /// <summary>
        /// 执行确认AI高报
        /// </summary>
        private void ExecuteConfirmAIHighAlarm(ChannelMapping channel)
        {
            // 确保我们有一个通道
            if (channel == null && CurrentChannel != null)
            {
                channel = CurrentChannel;
            }
            
            if (channel == null)
            {
                System.Diagnostics.Debug.WriteLine("ExecuteConfirmAIHighAlarm: 通道为null");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"ExecuteConfirmAIHighAlarm: 确认通道 {channel.VariableName}");
            
            // 设置当前通道状态为已通过
            channel.MonitorStatus = "已通过";
            
            // 确保我们有一个测试结果
            if (CurrentTestResult == null)
            {
                InitializeTestResult(channel);
            }
            
            if (CurrentTestResult != null)
            {
                CurrentTestResult.HighAlarmStatus = "已通过";
                CurrentTestResult.ResultText = "AI高报测试通过";
                CurrentTestResult.TestTime = DateTime.Now;
                
                // 通知UI更新
                RaisePropertyChanged(nameof(CurrentTestResult));
                
                // 更新统计数据
                UpdatePointStatistics();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("ExecuteConfirmAIHighAlarm: CurrentTestResult为null");
            }
        }

        /// <summary>
        /// 执行发送AI低报
        /// </summary>
        private void ExecuteSendAILowAlarm()
        {
            // TODO: 实现发送AI低报的逻辑
        }

        /// <summary>
        /// 执行复位AI低报
        /// </summary>
        private void ExecuteResetAILowAlarm()
        {
            // TODO: 实现复位AI低报的逻辑
        }

        /// <summary>
        /// 执行确认AI低报
        /// </summary>
        private void ExecuteConfirmAILowAlarm(ChannelMapping channel)
        {
            // 确保我们有一个通道
            if (channel == null && CurrentChannel != null)
            {
                channel = CurrentChannel;
            }
            
            if (channel == null)
            {
                System.Diagnostics.Debug.WriteLine("ExecuteConfirmAILowAlarm: 通道为null");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"ExecuteConfirmAILowAlarm: 确认通道 {channel.VariableName}");
            
            // 设置当前通道状态为已通过
            channel.MonitorStatus = "已通过";
            
            // 确保我们有一个测试结果
            if (CurrentTestResult == null)
            {
                InitializeTestResult(channel);
            }
            
            if (CurrentTestResult != null)
            {
                CurrentTestResult.LowAlarmStatus = "已通过";
                CurrentTestResult.ResultText = "AI低报测试通过";
                CurrentTestResult.TestTime = DateTime.Now;
                
                // 通知UI更新
                RaisePropertyChanged(nameof(CurrentTestResult));
                
                // 更新统计数据
                UpdatePointStatistics();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("ExecuteConfirmAILowAlarm: CurrentTestResult为null");
            }
        }

        /// <summary>
        /// 执行发送AI维护功能
        /// </summary>
        private void ExecuteSendAIMaintenance()
        {
            if (CurrentTestResult != null)
            {
                CurrentTestResult.MaintenanceFunction = "已通过";
            }
        }

        /// <summary>
        /// 执行复位AI维护功能
        /// </summary>
        private void ExecuteResetAIMaintenance()
        {
            // TODO: 实现复位AI维护功能的逻辑
        }

        /// <summary>
        /// 执行确认AI维护功能
        /// </summary>
        private void ExecuteConfirmAIMaintenance(ChannelMapping channel)
        {
            // 确保我们有一个通道
            if (channel == null && CurrentChannel != null)
            {
                channel = CurrentChannel;
            }
            
            if (channel == null)
            {
                System.Diagnostics.Debug.WriteLine("ExecuteConfirmAIMaintenance: 通道为null");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"ExecuteConfirmAIMaintenance: 确认通道 {channel.VariableName}");
            
            // 设置当前通道状态为已通过
            channel.MonitorStatus = "已通过";
            
            // 确保我们有一个测试结果
            if (CurrentTestResult == null)
            {
                InitializeTestResult(channel);
            }
            
            if (CurrentTestResult != null)
            {
                CurrentTestResult.MaintenanceFunction = "已通过";
                CurrentTestResult.ResultText = "AI维护功能测试通过";
                CurrentTestResult.TestTime = DateTime.Now;
                
                // 通知UI更新
                RaisePropertyChanged(nameof(CurrentTestResult));
                
                // 更新统计数据
                UpdatePointStatistics();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("ExecuteConfirmAIMaintenance: CurrentTestResult为null");
            }
        }

        /// <summary>
        /// 执行发送DI测试
        /// </summary>
        private void ExecuteSendDITest()
        {
            // TODO: 实现发送DI测试的逻辑
        }

        /// <summary>
        /// 执行复位DI
        /// </summary>
        private void ExecuteResetDI()
        {
            // TODO: 实现复位DI的逻辑
        }

        /// <summary>
        /// 执行确认DI
        /// </summary>
        private void ExecuteConfirmDI(ChannelMapping channel)
        {
            // 确保我们有一个通道
            if (channel == null && CurrentChannel != null)
            {
                channel = CurrentChannel;
            }
            
            if (channel == null)
            {
                System.Diagnostics.Debug.WriteLine("ExecuteConfirmDI: 通道为null");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"ExecuteConfirmDI: 确认通道 {channel.VariableName}");
            
            // 设置当前通道状态为已通过
            channel.MonitorStatus = "已通过";
            
            // 确保我们有一个测试结果
            if (CurrentTestResult == null)
            {
                InitializeTestResult(channel);
            }
            
            if (CurrentTestResult != null)
            {
                CurrentTestResult.HardPointTestResult = "已通过";
                CurrentTestResult.ResultText = "DI硬点测试通过";
                CurrentTestResult.TestResultStatus = 1; // 成功
                CurrentTestResult.TestTime = DateTime.Now;
                
                // 通知UI更新
                RaisePropertyChanged(nameof(CurrentTestResult));
                
                // 更新统计数据
                UpdatePointStatistics();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("ExecuteConfirmDI: CurrentTestResult为null");
            }
        }

        /// <summary>
        /// 初始化或获取当前通道的测试结果
        /// </summary>
        /// <param name="channel">当前通道</param>
        private void InitializeTestResult(ChannelMapping channel)
        {
            if (channel == null) return;
            
            System.Diagnostics.Debug.WriteLine($"InitializeTestResult for channel: {channel.VariableName}");
            
            // 查找该通道是否已有测试结果
            var existingResult = TestResults.FirstOrDefault(r => 
                r.VariableName == channel.VariableName && 
                r.ChannelTag == channel.ChannelTag);
                
            if (existingResult != null)
            {
                // 确保TestPLCChannelTag已更新
                if (string.IsNullOrEmpty(existingResult.TestPLCChannelTag) && !string.IsNullOrEmpty(channel.TestPLCChannelTag))
                {
                    existingResult.TestPLCChannelTag = channel.TestPLCChannelTag;
                }
                
                CurrentTestResult = existingResult;
                System.Diagnostics.Debug.WriteLine("使用已有测试结果");
            }
            else
            {
                // 创建新的测试结果
                var newResult = new ChannelMapping
                {
                    TestId = TestResults.Count + 1,
                    TestBatch = channel.TestBatch,
                    VariableName = channel.VariableName,
                    ModuleType = channel.ModuleType,
                    DataType = GetValueTypeForModule(channel.ModuleType),
                    TestPLCChannelTag = channel.TestPLCChannelTag,
                    ChannelTag = channel.ChannelTag,
                    TestTime = DateTime.Now,
                    TestResultStatus = 0, // 待测试
                    HardPointTestResult = "未测试",
                    HighAlarmStatus = "未测试",
                    LowAlarmStatus = "未测试",
                    MaintenanceFunction = "未测试",
                    ResultText = "等待测试"
                };
                
                // 设置DI/DO类型的NaN值
                if (channel.ModuleType?.ToLower() == "di" || channel.ModuleType?.ToLower() == "do")
                {
                    newResult.RangeLowerLimitValue = float.NaN;
                    newResult.RangeUpperLimitValue = float.NaN;
                    newResult.Value0Percent = double.NaN;
                    newResult.Value25Percent = double.NaN;
                    newResult.Value50Percent = double.NaN;
                    newResult.Value75Percent = double.NaN;
                    newResult.Value100Percent = double.NaN;
                    // 设置所有报警状态为N/A
                    newResult.LowLowAlarmStatus = "N/A";
                    newResult.LowAlarmStatus = "N/A";
                    newResult.HighAlarmStatus = "N/A"; 
                    newResult.HighHighAlarmStatus = "N/A";
                    newResult.MaintenanceFunction = "N/A";
                }
                
                TestResults.Add(newResult);
                CurrentTestResult = newResult;
                System.Diagnostics.Debug.WriteLine("创建新测试结果");
            }
            
            // 更新UI
            RaisePropertyChanged(nameof(CurrentTestResult));
            RaisePropertyChanged(nameof(TestResults));
        }

        /// <summary>
        /// 获取模块类型对应的数据类型
        /// </summary>
        private string GetValueTypeForModule(string moduleType)
        {
            if (string.IsNullOrEmpty(moduleType))
                return "Unknown";

            switch (moduleType.ToLower())
            {
                case "ai":
                case "ao":
                    return "Real";
                case "di":
                case "do":
                    return "Bool";
                default:
                    return "Unknown";
            }
        }

        #endregion

        #region 新命令实现

        private void ExecuteStartDOMonitor(ChannelMapping channel)
        {
            if (channel == null) return;
            
            // 更新当前通道
            CurrentChannel = channel;
            
            // 设置监测状态
            channel.MonitorStatus = "正在检测";
            DOMonitorStatus = "正在检测";
            DOCurrentValue = "1"; // 假设DO值为1
            
            // 初始化测试结果
            InitializeTestResult(channel);
            
            // 设置当前测试结果状态
            if (CurrentTestResult != null)
            {
                CurrentTestResult.HardPointTestResult = "测试中";
                RaisePropertyChanged(nameof(CurrentTestResult));
            }
            
            // 通知UI更新
            RaisePropertyChanged(nameof(DOMonitorStatus));
            RaisePropertyChanged(nameof(DOCurrentValue));
        }

        private void ExecuteStartAOMonitor(ChannelMapping channel)
        {
            if (channel == null) return;
            
            // 更新当前通道
            CurrentChannel = channel;
            
            // 设置监测状态
            channel.MonitorStatus = "正在检测";
            AOMonitorStatus = "正在检测";
            AOCurrentValue = "4.00 mA"; // 假设AO值为4mA
            
            // 初始化测试结果
            InitializeTestResult(channel);
            
            // 设置当前测试结果状态
            if (CurrentTestResult != null)
            {
                CurrentTestResult.HardPointTestResult = "测试中";
                RaisePropertyChanged(nameof(CurrentTestResult));
            }
            
            // 通知UI更新
            RaisePropertyChanged(nameof(AOMonitorStatus));
            RaisePropertyChanged(nameof(AOCurrentValue));
        }

        private void ExecuteConfirmDO(ChannelMapping channel)
        {
            // 确保我们有一个通道
            if (channel == null && CurrentChannel != null)
            {
                channel = CurrentChannel;
            }
            
            if (channel == null)
            {
                System.Diagnostics.Debug.WriteLine("ExecuteConfirmDO: 通道为null");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"ExecuteConfirmDO: 确认通道 {channel.VariableName}");
            
            // 设置当前通道状态和窗口状态
            channel.MonitorStatus = "已通过";
            DOMonitorStatus = "已通过";
            
            // 确保我们有一个测试结果
            if (CurrentTestResult == null)
            {
                InitializeTestResult(channel);
            }
            
            if (CurrentTestResult != null)
            {
                CurrentTestResult.HardPointTestResult = "已通过";
                CurrentTestResult.ResultText = "DO硬点测试通过";
                CurrentTestResult.TestResultStatus = 1; // 成功
                CurrentTestResult.TestTime = DateTime.Now;
                
                // 通知UI更新
                RaisePropertyChanged(nameof(CurrentTestResult));
                RaisePropertyChanged(nameof(DOMonitorStatus));
                
                // 更新统计数据
                UpdatePointStatistics();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("ExecuteConfirmDO: CurrentTestResult为null");
            }
        }

        private void ExecuteConfirmAO(ChannelMapping channel)
        {
            // 确保我们有一个通道
            if (channel == null && CurrentChannel != null)
            {
                channel = CurrentChannel;
            }
            
            if (channel == null)
            {
                System.Diagnostics.Debug.WriteLine("ExecuteConfirmAO: 通道为null");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"ExecuteConfirmAO: 确认通道 {channel.VariableName}");
            
            // 设置当前通道状态和窗口状态
            channel.MonitorStatus = "已通过";
            AOMonitorStatus = "已通过";
            
            // 确保我们有一个测试结果
            if (CurrentTestResult == null)
            {
                InitializeTestResult(channel);
            }
            
            if (CurrentTestResult != null)
            {
                CurrentTestResult.HardPointTestResult = "已通过";
                CurrentTestResult.ResultText = "AO硬点测试通过";
                CurrentTestResult.TestResultStatus = 1; // 成功
                CurrentTestResult.TestTime = DateTime.Now;
                
                // 通知UI更新
                RaisePropertyChanged(nameof(CurrentTestResult));
                RaisePropertyChanged(nameof(AOMonitorStatus));
                
                // 更新统计数据
                UpdatePointStatistics();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("ExecuteConfirmAO: CurrentTestResult为null");
            }
        }

        private void ExecuteCloseDOManualTest()
        {
            IsDOManualTestOpen = false;
        }

        private void ExecuteCloseAOManualTest()
        {
            IsAOManualTestOpen = false;
        }

        /// <summary>
        /// 执行打开DI手动测试窗口
        /// </summary>
        private void ExecuteOpenDIManualTest(ChannelMapping channel)
        {
            if (channel == null) return;
            
            // 设置当前通道
            CurrentChannel = channel;
            
            // 输出调试信息
            System.Diagnostics.Debug.WriteLine($"打开DI手动测试窗口: {channel.VariableName}");
            
            // 初始化或获取测试结果
            InitializeTestResult(channel);
            
            // 打开窗口
            IsDIManualTestOpen = true;
        }

        /// <summary>
        /// 执行打开DO手动测试窗口
        /// </summary>
        private void ExecuteOpenDOManualTest(ChannelMapping channel)
        {
            if (channel == null) return;
            
            // 设置当前通道
            CurrentChannel = channel;
            
            // 输出调试信息
            System.Diagnostics.Debug.WriteLine($"打开DO手动测试窗口: {channel.VariableName}");
            
            // 初始化或获取测试结果
            InitializeTestResult(channel);
            
            // 设置界面初始状态
            DOMonitorStatus = "请开始监测";
            DOCurrentValue = "--";
            
            // 通知UI更新
            RaisePropertyChanged(nameof(DOMonitorStatus));
            RaisePropertyChanged(nameof(DOCurrentValue));
            RaisePropertyChanged(nameof(CurrentChannel));
            
            // 打开窗口
            IsDOManualTestOpen = true;
        }

        /// <summary>
        /// 执行打开AO手动测试窗口
        /// </summary>
        private void ExecuteOpenAOManualTest(ChannelMapping channel)
        {
            if (channel == null) return;
            
            // 设置当前通道
            CurrentChannel = channel;
            
            // 输出调试信息
            System.Diagnostics.Debug.WriteLine($"打开AO手动测试窗口: {channel.VariableName}");
            
            // 初始化或获取测试结果
            InitializeTestResult(channel);
            
            // 设置界面初始状态
            AOMonitorStatus = "请开始监测";
            AOCurrentValue = "--";
            
            // 通知UI更新
            RaisePropertyChanged(nameof(AOMonitorStatus));
            RaisePropertyChanged(nameof(AOCurrentValue));
            RaisePropertyChanged(nameof(CurrentChannel));
            
            // 打开窗口
            IsAOManualTestOpen = true;
        }

        private void ExecuteMoveUp(ChannelMapping channel)
        {
            if (channel == null || CurrentChannels == null || CurrentChannels.Count <= 1)
                return;

            int currentIndex = CurrentChannels.IndexOf(channel);
            if (currentIndex <= 0)
                return;

            CurrentChannels.Move(currentIndex, currentIndex - 1);
        }

        private void ExecuteMoveDown(ChannelMapping channel)
        {
            if (channel == null || CurrentChannels == null || CurrentChannels.Count <= 1)
                return;

            int currentIndex = CurrentChannels.IndexOf(channel);
            if (currentIndex == -1 || currentIndex >= CurrentChannels.Count - 1)
                return;

            CurrentChannels.Move(currentIndex, currentIndex + 1);
        }

        private void ExecuteAllocateChannels()
        {
            // TODO: 实现通道分配逻辑
            Message = "通道分配功能待实现";
        }

        #endregion
    }
    
    // 批次信息类
    public class BatchInfo
    {
        public string BatchName { get; set; }
        public DateTime CreationDate { get; set; }
        public int ItemCount { get; set; }
        public string Status { get; set; }
        public DateTime? FirstTestTime { get; set; }
        public DateTime? LastTestTime { get; set; }

        public BatchInfo(string batchName, int itemCount)
        {
            BatchName = batchName;
            ItemCount = itemCount;
            CreationDate = DateTime.Now;
            Status = "未开始";
        }
    }
}
