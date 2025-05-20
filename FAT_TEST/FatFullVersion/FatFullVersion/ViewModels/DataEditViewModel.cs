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
using FatFullVersion.IServices;
using System.Collections.Generic;
using System.Threading.Tasks;
using FatFullVersion.Entities;
using FatFullVersion.Shared.Converters;
using FatFullVersion.Views;

namespace FatFullVersion.ViewModels
{
    public class DataEditViewModel : BindableBase
    {
        #region 属性和字段

        private readonly IPointDataService _pointDataService;
        private readonly IChannelMappingService _channelMappingService;
        private readonly ITestTaskManager _testTaskManager;
        private readonly IEventAggregator _eventAggregator;
        private readonly IPlcCommunication _testPlc;
        private readonly IPlcCommunication _targetPlc;
        private readonly IMessageService _messageService;
        private readonly ITestResultExportService _testResultExportService;
        private readonly ITestRecordService _testRecordService;
        private readonly IChannelStateManager _channelStateManager;

        private string _message;

        public string Message
        {
            get { return _message; }
            set { SetProperty(ref _message, value); }
        }

        /// <summary>
        /// 测试PLC通信实例，用于UI绑定
        /// </summary>
        public IPlcCommunication TestPlc => _testPlc;

        /// <summary>
        /// 被测PLC通信实例，用于UI绑定
        /// </summary>
        public IPlcCommunication TargetPlc => _targetPlc;

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

        /// <summary>
        /// 被测PLC通信实例
        /// </summary>
        protected readonly IPlcCommunication TargetPlcCommunication;

        // 当前显示的通道列表（用于UI展示）
        private ObservableCollection<ChannelMapping> _currentChannels;

        public ObservableCollection<ChannelMapping> CurrentChannels
        {
            get { return _currentChannels; }
            private set { SetProperty(ref _currentChannels, value); }
        }

        // 所有通道的数据源
        private ObservableCollection<ChannelMapping> _allChannels;

        /// <summary>
        /// 所有通道数据的主数据源
        /// </summary>
        public ObservableCollection<ChannelMapping> AllChannels
        {
            get { return _allChannels; }
            set
            {
                if (SetProperty(ref _allChannels, value))
                {
                    // 当AllChannels更新时，同步更新当前显示的通道
                    UpdateCurrentChannels();
                }
            }
        }

        /// <summary>
        /// 原始通道数据（用于批次选择）
        /// </summary>
        private ObservableCollection<ChannelMapping> _originalAllChannels;

        private ObservableCollection<ChannelMapping> OriginalAllChannels
        {
            get { return _originalAllChannels; }
            set { SetProperty(ref _originalAllChannels, value); }
        }

        /// <summary>
        /// 获取所有AI类型的通道
        /// </summary>
        public IEnumerable<ChannelMapping> GetAIChannels() => AllChannels?.Where(c => c.ModuleType?.ToUpper() == "AI");

        /// <summary>
        /// 获取所有AO类型的通道
        /// </summary>
        public IEnumerable<ChannelMapping> GetAOChannels() => AllChannels?.Where(c => c.ModuleType?.ToUpper() == "AO");

        /// <summary>
        /// 获取所有DI类型的通道
        /// </summary>
        public IEnumerable<ChannelMapping> GetDIChannels() => AllChannels?.Where(c => c.ModuleType?.ToUpper() == "DI");

        /// <summary>
        /// 获取所有DO类型的通道
        /// </summary>
        public IEnumerable<ChannelMapping> GetDOChannels() => AllChannels?.Where(c => c.ModuleType?.ToUpper() == "DO");

        /// <summary>
        /// 获取所有AI无源类型的通道
        /// </summary>
        public IEnumerable<ChannelMapping> GetAINoneChannels() => AllChannels?.Where(c => c.ModuleType?.ToUpper() == "AINONE");

        /// <summary>
        /// 获取所有AO无源类型的通道
        /// </summary>
        public IEnumerable<ChannelMapping> GetAONoneChannels() => AllChannels?.Where(c => c.ModuleType?.ToUpper() == "AONONE");

        /// <summary>
        /// 获取所有DI无源类型的通道
        /// </summary>
        public IEnumerable<ChannelMapping> GetDINoneChannels() => AllChannels?.Where(c => c.ModuleType?.ToUpper() == "DINONE");

        /// <summary>
        /// 获取所有DO无源类型的通道
        /// </summary>
        public IEnumerable<ChannelMapping> GetDONoneChannels() => AllChannels?.Where(c => c.ModuleType?.ToUpper() == "DONONE");

        /// <summary>
        /// 获取所有AI无源类型的通道
        /// </summary>
        // 测试结果数据
        //private ObservableCollection<ChannelMapping> _testResults;
        //public ObservableCollection<ChannelMapping> TestResults
        //{
        //    get => _testResults;
        //    set => SetProperty(ref _testResults, value);
        //}

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
                    //System.Diagnostics.Debug.WriteLine(
                    //    $"CurrentTestResult changed: {_currentTestResult?.VariableName}, HardPointTestResult: {_currentTestResult?.HardPointTestResult}");
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
            get => _selectedBatch;
            set
            {
                SetProperty(ref _selectedBatch, value);
                OnBatchSelected();
            }
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

        // 测试队列相关属性
        private ObservableCollection<ChannelMapping> _testQueue;

        /// <summary>
        /// 测试队列，存储待测试的通道列表
        /// </summary>
        public ObservableCollection<ChannelMapping> TestQueue
        {
            get { return _testQueue; }
            set { SetProperty(ref _testQueue, value); }
        }

        private int _testQueuePosition;

        /// <summary>
        /// 当前测试队列位置
        /// </summary>
        public int TestQueuePosition
        {
            get { return _testQueuePosition; }
            set { SetProperty(ref _testQueuePosition, value); }
        }

        private string _testQueueStatus;

        /// <summary>
        /// 测试队列状态描述
        /// </summary>
        public string TestQueueStatus
        {
            get { return _testQueueStatus; }
            set { SetProperty(ref _testQueueStatus, value); }
        }

        private ChannelMapping _currentQueueItem;

        /// <summary>
        /// 当前队列中的测试项
        /// </summary>
        public ChannelMapping CurrentQueueItem
        {
            get { return _currentQueueItem; }
            set { SetProperty(ref _currentQueueItem, value); }
        }

        // 命令
        public DelegateCommand ImportConfigCommand { get; private set; }
        public DelegateCommand RestoreConfigCommand { get; private set; }
        public DelegateCommand SelectBatchCommand { get; private set; }
        public DelegateCommand ExportChannelMapCommand { get; private set; }
        public DelegateCommand SkipModuleCommand { get; private set; }
        public DelegateCommand FinishWiringCommand { get; private set; }
        public DelegateCommand StartTestCommand { get; private set; }
        public DelegateCommand<ChannelMapping> RetestCommand { get; private set; }
        public DelegateCommand ExportTestResultsCommand { get; private set; }
        public DelegateCommand ConfirmBatchSelectionCommand { get; private set; }
        public DelegateCommand CancelBatchSelectionCommand { get; private set; }
        public DelegateCommand AllocateChannelsCommand { get; private set; }
        public DelegateCommand ClearChannelAllocationsCommand { get; private set; }

        // 添加原始通道集合属性
        private ObservableCollection<ChannelMapping> _originalAIChannels;
        private ObservableCollection<ChannelMapping> _originalAOChannels;
        private ObservableCollection<ChannelMapping> _originalDIChannels;
        private ObservableCollection<ChannelMapping> _originalDOChannels;

        // 命令
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
        public DelegateCommand<ChannelMapping> SendAITestValueCommand { get; private set; }

        /// <summary>
        /// 确认AI值命令
        /// </summary>
        public DelegateCommand<ChannelMapping> ConfirmAIValueCommand { get; private set; }

        /// <summary>
        /// 发送AI高报命令
        /// </summary>
        public DelegateCommand<ChannelMapping> SendAIHighAlarmCommand { get; private set; }
        /// <summary>
        /// 发送AI高高报命令
        /// </summary>
        public DelegateCommand<ChannelMapping> SendAIHighHighAlarmCommand { get; private set; }

        /// <summary>
        /// 复位AI高报命令
        /// </summary>
        public DelegateCommand<ChannelMapping> ResetAIHighAlarmCommand { get; private set; }

        /// <summary>
        /// 确认AI高报命令
        /// </summary>
        public DelegateCommand<ChannelMapping> ConfirmAIHighAlarmCommand { get; private set; }

        /// <summary>
        /// 发送AI低报命令
        /// </summary>
        public DelegateCommand<ChannelMapping> SendAILowAlarmCommand { get; private set; }

        /// <summary>
        /// 发送AI低低报命令
        /// </summary>
        public DelegateCommand<ChannelMapping> SendAILowLowAlarmCommand { get; private set; }

        /// <summary>
        /// 复位AI低报命令
        /// </summary>
        public DelegateCommand<ChannelMapping> ResetAILowAlarmCommand { get; private set; }

        /// <summary>
        /// 确认AI低报命令
        /// </summary>
        public DelegateCommand<ChannelMapping> ConfirmAILowAlarmCommand { get; private set; }

        /// <summary>
        /// 确认AI报警值设定命令
        /// </summary>
        public DelegateCommand<ChannelMapping> ConfirmAIAlarmValueSetCommand { get; private set; }

        /// <summary>
        /// 发送AI维护功能命令
        /// </summary>
        public DelegateCommand<ChannelMapping> SendAIMaintenanceCommand { get; private set; }

        /// <summary>
        /// 复位AI维护功能命令
        /// </summary>
        public DelegateCommand<ChannelMapping> ResetAIMaintenanceCommand { get; private set; }

        /// <summary>
        /// 确认AI维护功能命令
        /// </summary>
        public DelegateCommand<ChannelMapping> ConfirmAIMaintenanceCommand { get; private set; }

        /// <summary>
        /// 确认AI趋势检查命令
        /// </summary>
        public DelegateCommand<ChannelMapping> ConfirmAITrendCheckCommand { get; private set; }

        /// <summary>
        /// 确认AI报表检查命令
        /// </summary>
        public DelegateCommand<ChannelMapping> ConfirmAIReportCheckCommand { get; private set; }

        /// <summary>
        /// 确认AO趋势检查命令
        /// </summary>
        public DelegateCommand<ChannelMapping> ConfirmAOTrendCheckCommand { get; private set; }

        /// <summary>
        /// 确认AO报表检查命令
        /// </summary>
        public DelegateCommand<ChannelMapping> ConfirmAOReportCheckCommand { get; private set; }

        /// <summary>
        /// 发送DI测试命令
        /// </summary>
        public DelegateCommand<ChannelMapping> SendDITestCommand { get; private set; }

        /// <summary>
        /// 复位DI命令
        /// </summary>
        public DelegateCommand<ChannelMapping> ResetDICommand { get; private set; }

        /// <summary>
        /// 确认DI命令
        /// </summary>
        public DelegateCommand<ChannelMapping> ConfirmDICommand { get; private set; }

        private bool _isDOManualTestOpen;

        public bool IsDOManualTestOpen
        {
            get => _isDOManualTestOpen;
            set => SetProperty(ref _isDOManualTestOpen, value);
        }

        private bool _isAOManualTestOpen;

        public bool IsAOManualTestOpen
        {
            get => _isAOManualTestOpen;
            set => SetProperty(ref _isAOManualTestOpen, value);
        }

        // 监测状态
        private string _diMonitorStatus = "开始监测";

        public string DIMonitorStatus
        {
            get { return _diMonitorStatus; }
            set { SetProperty(ref _diMonitorStatus, value); }
        }

        private string _doMonitorStatus = "开始监测";

        public string DOMonitorStatus
        {
            get { return _doMonitorStatus; }
            set { SetProperty(ref _doMonitorStatus, value); }
        }

        private string _aoMonitorStatus = "开始监测";

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
        //public DelegateCommand<ChannelMapping> SaveAO0Command { get; private set; }
        //public DelegateCommand<ChannelMapping> SaveAO25Command { get; private set; }
        //public DelegateCommand<ChannelMapping> SaveAO50Command { get; private set; }
        //public DelegateCommand<ChannelMapping> SaveAO75Command { get; private set; }
        //public DelegateCommand<ChannelMapping> SaveAO100Command { get; private set; }
        public DelegateCommand<ChannelMapping> ConfirmAOCommand { get; private set; }

        private ChannelMapping _currentChannel;

        public ChannelMapping CurrentChannel
        {
            get => _currentChannel;
            set => SetProperty(ref _currentChannel, value);
        }

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
        /// AI低报设定值
        /// </summary>
        private string _aILowSetValue;

        public string AILowSetValue
        {
            get => _aILowSetValue;
            set => SetProperty(ref _aILowSetValue, value);
        }

        /// <summary>
        /// AI低低报设定值
        /// </summary>
        private string _aILowLowSetValue;

        public string AILowLowSetValue
        {
            get => _aILowLowSetValue;
            set => SetProperty(ref _aILowLowSetValue, value);
        }

        /// <summary>
        /// AI高报设定值
        /// </summary>
        private string _aIHighSetValue;

        public string AIHighSetValue
        {
            get => _aIHighSetValue;
            set => SetProperty(ref _aIHighSetValue, value);
        }

        /// <summary>
        /// AI高高报设定值
        /// </summary>
        private string _aIHighHighSetValue;

        public string AIHighHighSetValue
        {
            get => _aIHighHighSetValue;
            set => SetProperty(ref _aIHighHighSetValue, value);
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

        // 添加字段和属性
        private bool _isWiringCompleteBtnEnabled = true;

        /// <summary>
        /// 表示"完成接线确认"按钮是否可用
        /// </summary>
        public bool IsWiringCompleteBtnEnabled
        {
            get => _isWiringCompleteBtnEnabled;
            set => SetProperty(ref _isWiringCompleteBtnEnabled, value);
        }

        // 添加命令属性
        private DelegateCommand _confirmWiringCompleteCommand;

        /// <summary>
        /// 确认接线完成命令
        /// </summary>
        public DelegateCommand ConfirmWiringCompleteCommand =>
            _confirmWiringCompleteCommand ??=
                new DelegateCommand(FinishWiring, CanExecuteConfirmWiringComplete); // 改为执行 FinishWiring

        // 添加控制通道硬点自动测试按钮的启用属性
        private bool _isStartTestButtonEnabled = false;

        /// <summary>
        /// 表示"通道硬点自动测试"按钮是否可用
        /// </summary>
        public bool IsStartTestButtonEnabled
        {
            get => _isStartTestButtonEnabled;
            set => SetProperty(ref _isStartTestButtonEnabled, value);
        }

        /// <summary>
        /// AI手动测试状态（专用于手动测试窗口，与硬点测试分离）
        /// </summary>
        private string _manualAITestStatus = "未测试";
        public string ManualAITestStatus
        {
            get => _manualAITestStatus;
            set => SetProperty(ref _manualAITestStatus, value);
        }

        /// <summary>
        /// DI手动测试状态（专用于手动测试窗口，与硬点测试分离）
        /// </summary>
        private string _manualDITestStatus = "未测试";
        public string ManualDITestStatus
        {
            get => _manualDITestStatus;
            set => SetProperty(ref _manualDITestStatus, value);
        }

        /// <summary>
        /// DO手动测试状态（专用于手动测试窗口，与硬点测试分离）
        /// </summary>
        private string _manualDOTestStatus = "未测试";
        public string ManualDOTestStatus
        {
            get => _manualDOTestStatus;
            set => SetProperty(ref _manualDOTestStatus, value);
        }

        /// <summary>
        /// AO手动测试状态（专用于手动测试窗口，与硬点测试分离）
        /// </summary>
        private string _manualAOTestStatus = "未测试";
        public string ManualAOTestStatus
        {
            get => _manualAOTestStatus;
            set => SetProperty(ref _manualAOTestStatus, value);
        }

        /// <summary>
        /// AI高报警手动测试状态
        /// </summary>
        private string _manualAIHighAlarmStatus = "未测试";
        public string ManualAIHighAlarmStatus
        {
            get => _manualAIHighAlarmStatus;
            set => SetProperty(ref _manualAIHighAlarmStatus, value);
        }

        /// <summary>
        /// AI低报警手动测试状态
        /// </summary>
        private string _manualAILowAlarmStatus = "未测试";
        public string ManualAILowAlarmStatus
        {
            get => _manualAILowAlarmStatus;
            set => SetProperty(ref _manualAILowAlarmStatus, value);
        }

        /// <summary>
        /// AI维护功能手动测试状态
        /// </summary>
        private string _manualAIMaintenanceStatus = "未测试";
        public string ManualAIMaintenanceStatus
        {
            get => _manualAIMaintenanceStatus;
            set => SetProperty(ref _manualAIMaintenanceStatus, value);
        }

        private bool _isHistoryRecordsOpen;
        /// <summary>
        /// 历史记录查看窗口是否打开
        /// </summary>
        public bool IsHistoryRecordsOpen
        {
            get { return _isHistoryRecordsOpen; }
            set { SetProperty(ref _isHistoryRecordsOpen, value); }
        }

        private ObservableCollection<TestBatchInfo> _testBatches;
        /// <summary>
        /// 历史测试批次列表
        /// </summary>
        public ObservableCollection<TestBatchInfo> TestBatches
        {
            get { return _testBatches; }
            set { SetProperty(ref _testBatches, value); }
        }

        private TestBatchInfo _selectedTestBatch;
        /// <summary>
        /// 当前选择的历史测试批次
        /// </summary>
        public TestBatchInfo SelectedTestBatch
        {
            get { return _selectedTestBatch; }
            set { SetProperty(ref _selectedTestBatch, value); }
        }

        // 历史记录相关命令
        public DelegateCommand RestoreTestRecordsCommand { get; private set; }
        public DelegateCommand DeleteTestBatchCommand { get; private set; }
        public DelegateCommand CloseHistoryRecordsCommand { get; private set; }

        // 添加模块跳过相关属性和命令
        private bool _isSkipModuleOpen;
        /// <summary>
        /// 模块跳过窗口是否打开
        /// </summary>
        public bool IsSkipModuleOpen
        {
            get { return _isSkipModuleOpen; }
            set { SetProperty(ref _isSkipModuleOpen, value); }
        }

        private ObservableCollection<ModuleInfo> _modules;
        /// <summary>
        /// 模块列表
        /// </summary>
        public ObservableCollection<ModuleInfo> Modules
        {
            get { return _modules; }
            set { SetProperty(ref _modules, value); }
        }

        private string _skipReason;
        /// <summary>
        /// 跳过原因
        /// </summary>
        public string SkipReason
        {
            get { return _skipReason; }
            set { SetProperty(ref _skipReason, value); }
        }

        // 初始化模块跳过相关命令
        public DelegateCommand ConfirmSkipModuleCommand { get; private set; }
        public DelegateCommand CancelSkipModuleCommand { get; private set; }

        private string _moduleSearchFilter;
        /// <summary>
        /// 模块搜索过滤条件
        /// </summary>
        public string ModuleSearchFilter
        {
            get { return _moduleSearchFilter; }
            set 
            { 
                if (SetProperty(ref _moduleSearchFilter, value))
                {
                    RaisePropertyChanged(nameof(FilteredModules));
                }
            }
        }

        /// <summary>
        /// 过滤后的模块列表
        /// </summary>
        public ObservableCollection<ModuleInfo> FilteredModules
        {
            get
            {
                if (Modules == null)
                    return new ObservableCollection<ModuleInfo>();

                if (string.IsNullOrWhiteSpace(ModuleSearchFilter))
                    return Modules;

                var filtered = Modules.Where(m => 
                    m.ModuleName.Contains(ModuleSearchFilter, StringComparison.OrdinalIgnoreCase) || 
                    m.ModuleType.Contains(ModuleSearchFilter, StringComparison.OrdinalIgnoreCase));
                
                return new ObservableCollection<ModuleInfo>(filtered);
            }
        }

        private bool _selectAllModules;
        /// <summary>
        /// 是否全选模块
        /// </summary>
        public bool SelectAllModules
        {
            get { return _selectAllModules; }
            set 
            { 
                if (SetProperty(ref _selectAllModules, value))
                {
                    // 更新所有模块的选中状态
                    if (Modules != null)
                    {
                        foreach (var module in Modules)
                        {
                            module.IsSelected = value;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 全选模块命令
        /// </summary>
        public DelegateCommand SelectAllModulesCommand { get; private set; }
        
        /// <summary>
        /// 取消全选模块命令
        /// </summary>
        public DelegateCommand UnselectAllModulesCommand { get; private set; }

        /// <summary>
        /// 全选所有模块
        /// </summary>
        private void ExecuteSelectAllModules()
        {
            if (Modules != null)
            {
                foreach (var module in Modules)
                {
                    module.IsSelected = true;
                }
                SelectAllModules = true;
            }
        }

        /// <summary>
        /// 取消全选所有模块
        /// </summary>
        private void ExecuteUnselectAllModules()
        {
            if (Modules != null)
            {
                foreach (var module in Modules)
                {
                    module.IsSelected = false;
                }
                SelectAllModules = false;
            }
        }

        #endregion

        #region 构造函数和初始化

        /// <summary>
        /// DataEditViewModel构造函数
        /// </summary>
        /// <param name="pointDataService">点位数据服务接口</param>
        /// <param name="channelMappingService">通道映射服务接口</param>
        /// <param name="testTaskManager">测试任务管理器接口</param>
        /// <param name="eventAggregator">事件聚合器</param>
        /// <param name="testPlc">测试PLC通信接口</param>
        /// <param name="targetPlc">目标PLC通信接口</param>
        /// <param name="testResultExportService">测试结果导出服务接口</param>
        /// <param name="testRecordService">测试记录服务接口</param>
        public DataEditViewModel(
            IPointDataService pointDataService,
            IChannelMappingService channelMappingService,
            ITestTaskManager testTaskManager,
            IEventAggregator eventAggregator,
            IPlcCommunication testPlc,
            IPlcCommunication targetPlc,
            ITestResultExportService testResultExportService,
            ITestRecordService testRecordService,
            IChannelStateManager channelStateManager
        )
        {
            _pointDataService = pointDataService;
            _channelMappingService = channelMappingService;
            _testTaskManager = testTaskManager;
            _eventAggregator = eventAggregator;
            _testPlc = testPlc ?? throw new ArgumentNullException(nameof(testPlc));
            _targetPlc = targetPlc ?? throw new ArgumentNullException(nameof(targetPlc));
            _testResultExportService = testResultExportService;
            _testRecordService = testRecordService ?? throw new ArgumentNullException(nameof(testRecordService));
            _channelStateManager = channelStateManager;

            // 初始化数据结构
            Initialize();
        }

        /// <summary>
        /// 初始化ViewModel
        /// </summary>
        /// <remarks>
        /// 初始化所有命令、事件订阅和数据集合，设置默认状态
        /// </remarks>
        private void Initialize()
        {
            // 订阅测试结果更新事件
            // 在 DataEditViewModel.cs 的 Initialize() 方法内
            // ... (已有的 _eventAggregator.GetEvent<TestResultsUpdatedEvent>().Subscribe(OnTestResultsUpdated); 之后) ...
            _eventAggregator.GetEvent<ChannelStatesModifiedEvent>().Subscribe(OnChannelStatesModified); // <<<< 新增此行 >>>>

            // 初始化集合
            AllChannels = new ObservableCollection<ChannelMapping>();
            CurrentChannels = new ObservableCollection<ChannelMapping>();
            //TestResults = new ObservableCollection<ChannelMapping>();
            Batches = new ObservableCollection<BatchInfo>();
            TestQueue = new ObservableCollection<ChannelMapping>();
            Modules = new ObservableCollection<ModuleInfo>();

            // 初始化搜索过滤和选中状态
            ModuleSearchFilter = string.Empty;
            SelectAllModules = false;
            SkipReason = string.Empty;

            // 初始化测试队列相关属性
            TestQueuePosition = 0;
            TestQueueStatus = "队列为空";

            // 初始化其他属性
            SelectedChannelType = "AI通道";
            IsBatchSelectionOpen = false;
            SelectedResultFilter = "全部";
            TotalPointCount = "0";
            TestedPointCount = "0";
            WaitingPointCount = "0";
            SuccessPointCount = "0";
            FailurePointCount = "0";

            // 初始化按钮状态
            IsStartTestButtonEnabled = false;

            // 初始化命令
            ImportConfigCommand = new DelegateCommand(ImportConfig);
            SelectBatchCommand = new DelegateCommand(ExecuteSelectBatch);
            ExportChannelMapCommand = new DelegateCommand(ExportChannelMap);
            SkipModuleCommand = new DelegateCommand(SkipModule);
            FinishWiringCommand = new DelegateCommand(FinishWiring);
            StartTestCommand = new DelegateCommand(StartTest);
            RetestCommand = new DelegateCommand<ChannelMapping>(Retest);
            ConfirmBatchSelectionCommand = new DelegateCommand(ConfirmBatchSelection);
            CancelBatchSelectionCommand = new DelegateCommand(CancelBatchSelection);
            AllocateChannelsCommand = new DelegateCommand(ExecuteAllocateChannels);
            ClearChannelAllocationsCommand = new DelegateCommand(ClearChannelAllocationsAsync);
            ExportTestResultsCommand = new DelegateCommand(ExportTestResults, CanExportTestResults);

            // 初始化手动测试相关命令
            OpenAIManualTestCommand = new DelegateCommand<ChannelMapping>(OpenAIManualTest);
            OpenDIManualTestCommand = new DelegateCommand<ChannelMapping>(OpenDIManualTest);
            OpenDOManualTestCommand = new DelegateCommand<ChannelMapping>(OpenDOManualTest);
            OpenAOManualTestCommand = new DelegateCommand<ChannelMapping>(OpenAOManualTest);
            CloseAIManualTestCommand = new DelegateCommand(ExecuteCloseAIManualTest);
            CloseDIManualTestCommand = new DelegateCommand(ExecuteCloseDIManualTest);
            CloseDOManualTestCommand = new DelegateCommand(ExecuteCloseDOManualTest);
            CloseAOManualTestCommand = new DelegateCommand(ExecuteCloseAOManualTest);


            // AI手动测试命令
            SendAITestValueCommand = new DelegateCommand<ChannelMapping>(ExecuteSendAITestValue);
            ConfirmAIValueCommand = new DelegateCommand<ChannelMapping>(ExecuteConfirmAIValue);
            SendAIHighAlarmCommand = new DelegateCommand<ChannelMapping>(ExecuteSendAIHighAlarm);
            SendAIHighHighAlarmCommand = new DelegateCommand<ChannelMapping>(ExecuteSendAIHighHighAlarm);
            ResetAIHighAlarmCommand = new DelegateCommand<ChannelMapping>(ExecuteResetAIHighAlarm);
            ConfirmAIHighAlarmCommand = new DelegateCommand<ChannelMapping>(ExecuteConfirmAIHighAlarm);
            SendAILowAlarmCommand = new DelegateCommand<ChannelMapping>(ExecuteSendAILowAlarm);
            SendAILowLowAlarmCommand = new DelegateCommand<ChannelMapping>(ExecuteSendAILowLowAlarm);
            ResetAILowAlarmCommand = new DelegateCommand<ChannelMapping>(ExecuteResetAILowAlarm);
            ConfirmAILowAlarmCommand = new DelegateCommand<ChannelMapping>(ExecuteConfirmAILowAlarm);
            ConfirmAIAlarmValueSetCommand = new DelegateCommand<ChannelMapping>(ExecuteConfirmAIAlarmValueSet);
            SendAIMaintenanceCommand = new DelegateCommand<ChannelMapping>(ExecuteSendAIMaintenance);
            ResetAIMaintenanceCommand = new DelegateCommand<ChannelMapping>(ExecuteResetAIMaintenance);
            ConfirmAIMaintenanceCommand = new DelegateCommand<ChannelMapping>(ExecuteConfirmAIMaintenance);

            // 添加新的命令初始化
            ConfirmAITrendCheckCommand = new DelegateCommand<ChannelMapping>(ExecuteConfirmAITrendCheck);
            ConfirmAIReportCheckCommand = new DelegateCommand<ChannelMapping>(ExecuteConfirmAIReportCheck);
            ConfirmAOTrendCheckCommand = new DelegateCommand<ChannelMapping>(ExecuteConfirmAOTrendCheck); // 新增AO趋势检查命令初始化
            ConfirmAOReportCheckCommand = new DelegateCommand<ChannelMapping>(ExecuteConfirmAOReportCheck); // 新增AO报表检查命令初始化


            // DI手动测试命令
            SendDITestCommand = new DelegateCommand<ChannelMapping>(ExecuteSendDITest);
            ResetDICommand = new DelegateCommand<ChannelMapping>(ExecuteResetDI);
            ConfirmDICommand = new DelegateCommand<ChannelMapping>(ExecuteConfirmDI);

            // DO手动测试命令
            StartDOMonitorCommand = new DelegateCommand<ChannelMapping>(ExecuteStartDOMonitor);
            ConfirmDOCommand = new DelegateCommand<ChannelMapping>(ExecuteConfirmDO);

            // AO手动测试命令
            StartAOMonitorCommand = new DelegateCommand<ChannelMapping>(ExecuteStartAOMonitor);
            //SaveAO0Command = new DelegateCommand<ChannelMapping>(ExecuteSaveAO0);
            //SaveAO25Command = new DelegateCommand<ChannelMapping>(ExecuteSaveAO25);
            //SaveAO50Command = new DelegateCommand<ChannelMapping>(ExecuteSaveAO50);
            //SaveAO75Command = new DelegateCommand<ChannelMapping>(ExecuteSaveAO75);
            //SaveAO100Command = new DelegateCommand<ChannelMapping>(ExecuteSaveAO100);
            ConfirmAOCommand = new DelegateCommand<ChannelMapping>(ExecuteConfirmAO);

            // 尝试从通道映射信息中提取批次信息
            InitializeBatchData();

            // 历史记录相关命令
            RestoreConfigCommand = new DelegateCommand(RestoreConfig);
            RestoreTestRecordsCommand = new DelegateCommand(RestoreTestRecords);
            DeleteTestBatchCommand = new DelegateCommand(DeleteTestBatch);
            CloseHistoryRecordsCommand = new DelegateCommand(CloseHistoryRecords);

            // 初始化模块跳过相关命令
            ConfirmSkipModuleCommand = new DelegateCommand(ConfirmSkipModule);
            CancelSkipModuleCommand = new DelegateCommand(CancelSkipModule);

            // 初始化模块全选/取消全选命令
            SelectAllModulesCommand = new DelegateCommand(ExecuteSelectAllModules);
            UnselectAllModulesCommand = new DelegateCommand(ExecuteUnselectAllModules);
        }

                // <<<< 新增事件处理器 >>>>
        private void OnChannelStatesModified(List<Guid> modifiedChannelIds)
        {
            if (modifiedChannelIds == null || !modifiedChannelIds.Any() || AllChannels == null) return;

            // 确保在UI线程执行UI更新和依赖属性的刷新
            Application.Current.Dispatcher.Invoke(() =>
            {
                bool currentChannelsPotentiallyAffected = false;
                foreach (Guid id in modifiedChannelIds)
                {
                    // 检查是否有当前显示的通道受到了影响
                    if (CurrentChannels.Any(cc => cc.Id == id))
                    {
                        currentChannelsPotentiallyAffected = true;
                        break; 
                    }
                }

                if (currentChannelsPotentiallyAffected)
                {
                    // 当 ChannelMapping 对象的内部属性被 ChannelStateManager 修改后，
                    // 如果 ChannelMapping 实现了 INotifyPropertyChanged，并且属性的 setter 调用了 RaisePropertyChanged，
                    // 那么绑定到这些特定属性的UI元素（如DataGrid单元格模板中的TextBlock）会自动更新。
                    // 但是，如果DataGrid的ItemsSource绑定的是ObservableCollection<ChannelMapping>，
                    // 并且列的绑定不是直接到可通知的属性，或者需要重新应用排序/筛选，
                    // 则刷新整个 CurrentChannels 可能是必要的。
                    // UpdateCurrentChannels() 通常会基于 AllChannels 和当前筛选条件重新构建 CurrentChannels。
                    UpdateCurrentChannels();
                    System.Diagnostics.Debug.WriteLine($"UI事件：OnChannelStatesModified - CurrentChannels 已通过 UpdateCurrentChannels() 刷新，因为受影响的通道在其中。");

                    // 或者，如果 ChannelMapping 是 BindableBase 并且希望强制刷新特定项的显示（如果上述方法不够用）：
                    // foreach(var id in modifiedChannelIds)
                    // {
                    //    var itemInCurrent = CurrentChannels.FirstOrDefault(c => c.Id == id);
                    //    itemInCurrent?.RaisePropertyChanged(string.Empty); // 通知所有属性已更改
                    //    var itemInAll = AllChannels.FirstOrDefault(c => c.Id == id);
                    //    itemInAll?.RaisePropertyChanged(string.Empty);
                    // }
                }
                else
                {
                    // 即使当前显示的通道列表未直接包含修改项（例如，由于筛选），
                    // AllChannels 中的对象状态也已更新。如果UI中有其他地方绑定到AllChannels
                    // 或依赖于其中对象的特定状态，也可能需要通知。
                    // 但通常，主要关注的是 CurrentChannels。
                    System.Diagnostics.Debug.WriteLine($"UI事件：OnChannelStatesModified - 受影响的通道不在CurrentChannels中，未主动刷新CurrentChannels。");
                }

                // 更新依赖于通道状态的ViewModel聚合属性和命令状态
                UpdatePointStatistics(); // 统计数据依赖于AllChannels中对象的状态
                RefreshBatchStatus();    // 批次状态也依赖于AllChannels中对象的状态
                ExportTestResultsCommand.RaiseCanExecuteChanged(); // 导出命令的可用性可能改变
                // 根据需要，其他命令的CanExecute状态也可能需要刷新
                // 例如，RetestCommand, StartTestCommand 等如果它们的CanExecute逻辑依赖于特定通道状态
                RetestCommand.RaiseCanExecuteChanged();
                StartTestCommand.RaiseCanExecuteChanged(); 
                // ...等其他可能受影响的命令...
            });
        }
        #endregion

        #region 数据加载和处理

        /// <summary>
        /// 导入配置
        /// </summary>
        /// <remarks>
        /// 打开文件对话框并导入Excel配置数据，然后处理导入的数据
        /// </remarks>
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

                    await _pointDataService.ImportPointConfigurationAsync(data =>
                    {
                        try
                        {
                            tcs.SetResult(data);
                        }
                        catch (Exception ex)
                        {
                            tcs.SetException(ex);
                        }
                    });

                    // 等待异步操作完成并获取结果
                    var importedData = await tcs.Task;

                    // 清空原始通道集合引用
                    OriginalAllChannels = null;

                    // 处理导入的数据
                    if (importedData != null)
                    {
                        await ProcessImportedDataAsync(importedData);
                    }
                }
                else
                {
                    await _messageService.ShowAsync("导入失败", "无法获取点表数据服务", MessageBoxButton.OK);
                }
            }
            catch (Exception ex)
            {
                await _messageService.ShowAsync("导入失败", $"导入配置失败: {ex.Message}", MessageBoxButton.OK);
            }
            finally
            {
                IsLoading = false;
                StatusMessage = string.Empty;
            }
        }

        /// <summary>
        /// 处理导入的数据
        /// </summary>
        /// <param name="importedData">从Excel导入的点位数据集合</param>
        /// <returns>处理完成的任务</returns>
        /// <remarks>
        /// 将Excel数据转换为通道映射对象，应用业务规则，并更新UI
        /// </remarks>
        private async Task ProcessImportedDataAsync(IEnumerable<ExcelPointData> importedData)
        {
            if (importedData == null || !importedData.Any())
            {
                await _messageService.ShowAsync("提示", "没有导入任何数据。", MessageBoxButton.OK);
                return;
            }

            try
            {
                IsLoading = true;
                StatusMessage = "正在处理导入数据并初始化通道状态...";

                // 使用 ChannelMappingService 来创建和初始化 ChannelMapping 对象
                // ChannelMappingService.CreateAndInitializeChannelMappingsAsync 内部会调用 IChannelStateManager.InitializeChannelFromImport
                var initializedChannels = await _channelMappingService.CreateAndInitializeChannelMappingsAsync(importedData);

                if (initializedChannels != null)
                {
                    AllChannels = new ObservableCollection<ChannelMapping>(initializedChannels.OrderBy(c => c.TestId)); 
                    OriginalAllChannels = new ObservableCollection<ChannelMapping>(AllChannels); 

                    // UI 更新
                    UpdateCurrentChannels(); 
                    RaisePropertyChanged(nameof(AllChannels));
                    UpdatePointStatistics();
                    await RefreshBatchesFromChannelsAsync(); 
                    SelectedBatch = null; 
                    IsWiringCompleteBtnEnabled = true; 
                    IsStartTestButtonEnabled = false; 

                    await _messageService.ShowAsync("成功", $"已成功处理和初始化 {AllChannels.Count} 个通道。", MessageBoxButton.OK);
                }
                else
                {
                    AllChannels.Clear(); // 如果服务未返回任何内容，则清空
                    OriginalAllChannels.Clear();
                    UpdateCurrentChannels();
                    RaisePropertyChanged(nameof(AllChannels));
                    UpdatePointStatistics();
                    await RefreshBatchesFromChannelsAsync();
                    SelectedBatch = null;
                    await _messageService.ShowAsync("提示", "数据导入后未生成任何通道信息。", MessageBoxButton.OK);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = string.Empty; // 在显示错误消息前清除状态
                await _messageService.ShowAsync("错误", $"导入数据处理错误: {ex.Message}", MessageBoxButton.OK);
                System.Diagnostics.Debug.WriteLine($"ProcessImportedDataAsync Error: {ex.Message}");
                // 发生错误时也清空数据，避免不一致状态
                AllChannels?.Clear();
                OriginalAllChannels?.Clear();
                UpdateCurrentChannels();
                RaisePropertyChanged(nameof(AllChannels));
                UpdatePointStatistics();
                await RefreshBatchesFromChannelsAsync();
                SelectedBatch = null;
            }
            finally
            {
                IsLoading = false;
                StatusMessage = string.Empty;
            }
        }

        /// <summary>
        /// 恢复配置
        /// </summary>
        /// <remarks>
        /// 从数据库中恢复之前保存的通道配置数据
        /// </remarks>
        private async void RestoreConfig()
        {
            try
            {
                // 显示历史记录窗口
                await LoadTestBatchesAsync();
                IsHistoryRecordsOpen = true;
            }
            catch (Exception ex)
            {
                await _messageService.ShowAsync("错误", $"加载历史测试记录失败: {ex.Message}", MessageBoxButton.OK);
            }
        }

        /// <summary>
        /// 加载测试批次
        /// </summary>
        /// <returns>加载完成的任务</returns>
        /// <remarks>
        /// 从数据库加载所有可用的测试批次信息
        /// </remarks>
        private async Task LoadTestBatchesAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "正在加载历史测试记录...";

                // 获取所有测试批次信息
                var batches = await _testRecordService.GetAllTestBatchesAsync();

                if (batches != null && batches.Any())
                {
                    TestBatches = new ObservableCollection<TestBatchInfo>(batches);
                    SelectedTestBatch = TestBatches.FirstOrDefault();
                }
                else
                {
                    TestBatches = new ObservableCollection<TestBatchInfo>();
                    await _messageService.ShowAsync("提示", "没有找到历史测试记录", MessageBoxButton.OK);
                }
            }
            catch (Exception ex)
            {
                await _messageService.ShowAsync("错误", $"加载历史测试记录时出错: {ex.Message}", MessageBoxButton.OK);
            }
            finally
            {
                IsLoading = false;
                StatusMessage = string.Empty;
            }
        }

        /// <summary>
        /// 初始化批次数据
        /// </summary>
        /// <remarks>
        /// 初始化批次选择界面并获取可用的批次列表
        /// </remarks>
        private async void InitializeBatchData()
        {
            // 初始化批次数据
            Batches = new ObservableCollection<BatchInfo>();

            // 尝试从通道映射信息中提取批次信息
            await UpdateBatchInfoAsync();
        }

        /// <summary>
        /// 更新批次信息
        /// </summary>
        /// <returns>更新完成的任务</returns>
        /// <remarks>
        /// 更新当前选中批次的详细信息，包括测试状态和统计数据
        /// </remarks>
        private async Task UpdateBatchInfoAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "正在更新批次信息...";

                // 检查AllChannels是否有效
                if (AllChannels == null || !AllChannels.Any())
                {
                    Message = "没有可用的通道信息";
                    return;
                }

                // 同步确保批次信息和PLC通道信息是最新的
                foreach (var channel in AllChannels)
                {
                    if (channel != null && !string.IsNullOrEmpty(channel.TestBatch))
                    {
                        var result = AllChannels.FirstOrDefault(r =>
                            r != null &&
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
                var batches = await _channelMappingService.ExtractBatchInfoAsync(AllChannels);

                // 根据测试结果更新批次状态
                var updatedBatches = await _channelMappingService.UpdateBatchStatusAsync(batches, AllChannels);

                // 保存当前选中的批次名称
                string selectedBatchName = SelectedBatch?.BatchName;

                // 更新批次集合
                if (updatedBatches != null && updatedBatches.Any())
                {
                    Batches = new ObservableCollection<BatchInfo>(updatedBatches);

                    // 如果之前有选中的批次，尝试找回并选中
                    if (!string.IsNullOrEmpty(selectedBatchName))
                    {
                        var updatedSelectedBatch =
                            Batches.FirstOrDefault(b => b != null && b.BatchName == selectedBatchName);
                        if (updatedSelectedBatch != null)
                        {
                            // 直接设置字段而不触发OnBatchSelected
                            _selectedBatch = updatedSelectedBatch;
                            RaisePropertyChanged(nameof(SelectedBatch));

                            // 更新接线确认按钮状态
                            IsWiringCompleteBtnEnabled = updatedSelectedBatch.Status == "未开始" ||
                                                         updatedSelectedBatch.Status == "测试中";
                            //IsWiringCompleteBtnEnabled = true;
                        }
                    }
                    // 如果没有选中批次但有可用批次，选择第一个
                    else if (Batches.Count > 0)
                    {
                        SelectedBatch = Batches[0];
                    }
                }
                else
                {
                    Message = "未找到批次信息";
                }

                // 更新点位统计数据
                UpdatePointStatistics();

                // 通知UI更新
                RaisePropertyChanged(nameof(AllChannels));
            }
            catch (Exception ex)
            {
                Message = $"更新批次信息失败: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"更新批次信息失败: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                StatusMessage = string.Empty;
            }
        }

        /// <summary>
        /// 执行批次选择
        /// </summary>
        /// <remarks>
        /// 打开批次选择对话框并加载可用批次
        /// </remarks>
        private async void ExecuteSelectBatch()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "正在获取批次信息...";

                // 使用原始通道数据更新批次信息，确保批次列表完整
                if (OriginalAllChannels != null && OriginalAllChannels.Any())
                {
                    // 使用通道映射服务提取批次信息
                    var batchInfoList = await _channelMappingService.ExtractBatchInfoAsync(OriginalAllChannels);

                    // 检查是否有批次信息
                    if (batchInfoList != null && batchInfoList.Any())
                    {
                        Batches = new ObservableCollection<BatchInfo>(batchInfoList.OrderBy(b=>b.BatchName));

                        // 预先选择第一个批次，提升用户体验
                        if (Batches.Count > 0 && SelectedBatch == null)
                        {
                            //SelectedBatch = Batches[0];
                        }
                    }
                    else
                    {
                        Message = "未找到批次信息，请先分配通道";
                    }
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
                await _messageService.ShowAsync("错误", $"获取批次信息失败: {ex.Message}", MessageBoxButton.OK);
                System.Diagnostics.Debug.WriteLine($"获取批次信息失败: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                StatusMessage = string.Empty;
            }
        }
        /// <summary>
        /// 导出通道映射
        /// </summary>
        private void ExportChannelMap()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "正在导出通道映射表...";

                if (AllChannels == null || !AllChannels.Any())
                {
                    _messageService.ShowAsync("导出失败", "没有可导出的通道映射数据", MessageBoxButton.OK);
                    return;
                }

                // 使用ITestResultExportService导出通道映射表
                _testResultExportService.ExportChannelMapToExcelAsync(AllChannels)
                    .ContinueWith(task =>
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            if (task.Result)
                            {
                                StatusMessage = "通道映射表导出成功";
                            }
                            else
                            {
                                StatusMessage = "通道映射表导出失败";
                            }
                            IsLoading = false;
                        });
                    });
            }
            catch (Exception ex)
            {
                _messageService.ShowAsync("导出失败", $"导出通道映射表时发生错误: {ex.Message}", MessageBoxButton.OK);
                StatusMessage = string.Empty;
                IsLoading = false;
            }
        }

        /// <summary>
        /// 跳过选择的模块，不测试并添加备注项
        /// </summary>
        private void SkipModule()
        {
            // 检查是否有可用通道
            if (AllChannels == null || !AllChannels.Any())
            {
                await _messageService.ShowAsync("提示", "没有可用的通道信息", MessageBoxButton.OK);
                return;
            }

            try
            {
                // 输出调试信息
                System.Diagnostics.Debug.WriteLine($"开始提取模块信息: AllChannels包含 {AllChannels.Count} 个通道");
                int validChannels = AllChannels.Count(c => !string.IsNullOrEmpty(c.ChannelTag));
                System.Diagnostics.Debug.WriteLine($"有效ChannelTag的通道数量: {validChannels}");
                
                // 输出前10个通道的ChannelTag示例
                var sampleChannels = AllChannels.Where(c => !string.IsNullOrEmpty(c.ChannelTag)).Take(10);
                foreach (var channel in sampleChannels)
                {
                    System.Diagnostics.Debug.WriteLine($"通道示例: {channel.ChannelTag}, 模块类型: {channel.ModuleType}");
                }
                
                // 清空搜索过滤和选中状态
                ModuleSearchFilter = string.Empty;
                SelectAllModules = false;
                SkipReason = string.Empty;
                
                // 打开模块跳过窗口
                IsSkipModuleOpen = true;
                
                // 从所有通道中提取模块信息
                RefreshModules();
                
                // 打印模块列表信息
                System.Diagnostics.Debug.WriteLine($"模块提取结果: {(Modules == null ? "Modules为null" : $"找到{Modules.Count}个模块")}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"跳过模块操作异常: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"异常详情: {ex}");
                await _messageService.ShowAsync("错误", $"跳过模块操作失败: {ex.Message}", MessageBoxButton.OK);
            }
        }

        /// <summary>
        /// 刷新模块列表
        /// </summary>
        private void RefreshModules()
        {
            if (AllChannels == null || !AllChannels.Any())
                return;

            try
            {
                // 按模块为单位汇总模块信息，从ChannelTag中提取机架、槽位、模块类型信息
                var moduleGroups = AllChannels
                    .Where(c => !string.IsNullOrEmpty(c.ChannelTag))
                    .Select(c => new
                    {
                        Channel = c,
                        Parts = c.ChannelTag.Split('_')
                    })
                    .Where(x => x.Parts.Length >= 3) // 确保至少包含机架_槽_模块类型
                    .GroupBy(x => new
                    {
                        Rack = x.Parts[0],
                        Slot = x.Parts[1],
                        ModuleType = x.Parts[2]
                    })
                    .Select(g => new ModuleInfo
                    {
                        // 生成模块名称: "机架1_槽2_AI"
                        ModuleName = $"{g.Key.Rack}机架_{g.Key.Slot}槽_{g.Key.ModuleType}",
                        ChannelCount = g.Count(),
                        ModuleType = g.Key.ModuleType,
                        IsSelected = false
                    })
                    .OrderBy(m => m.ModuleName)
                    .ToList();

                Modules = new ObservableCollection<ModuleInfo>(moduleGroups);
                
                // 通知UI更新
                RaisePropertyChanged(nameof(Modules));
                RaisePropertyChanged(nameof(FilteredModules));
                
                // 输出调试信息
                System.Diagnostics.Debug.WriteLine($"已加载 {Modules.Count} 个模块");
                foreach (var module in Modules)
                {
                    System.Diagnostics.Debug.WriteLine($"模块: {module.ModuleName}, 类型: {module.ModuleType}, 通道数: {module.ChannelCount}");
                }
            }
            catch (Exception ex)
            {
                await _messageService.ShowAsync("错误", $"提取模块信息失败: {ex.Message}", MessageBoxButton.OK);
            }
        }

        /// <summary>
        /// 确认跳过选中的模块
        /// </summary>
        private async void ConfirmSkipModule()
        {
            if (Modules == null || !Modules.Any(m => m.IsSelected))
            {
                await _messageService.ShowAsync("提示", "请至少选择一个要跳过的模块。", MessageBoxButton.OK);
                return;
            }

            if (string.IsNullOrWhiteSpace(SkipReason))
            {
                await _messageService.ShowAsync("提示", "请输入跳过原因。", MessageBoxButton.OK);
                return;
            }

            var selectedModules = Modules.Where(m => m.IsSelected).ToList();
            if (!selectedModules.Any()) return;

            string confirmationMessage = $"确定要跳过选中的 {selectedModules.Count} 个模块吗？\n原因: {SkipReason}";
            var result = await _messageService.ShowAsync("确认跳过模块", confirmationMessage, MessageBoxButton.YesNo);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            IsLoading = true;
            StatusMessage = "正在处理跳过模块...";
            List<Guid> modifiedChannelIds = new List<Guid>();

            try
            {
                var channelsToSkip = AllChannels
                    .Where(c => selectedModules.Any(m => m.ModuleName == c.ModuleName && m.StationName == c.StationName))
                    .ToList();

                if (channelsToSkip.Any())
                {
                    DateTime skipTime = DateTime.Now;
                    foreach (var channel in channelsToSkip)
                    {
                        _channelStateManager.MarkAsSkipped(channel, SkipReason, skipTime);
                        modifiedChannelIds.Add(channel.Id); 
                    }

                    // 持久化更改 (如果需要)
                    // await _channelMappingService.UpdateChannelMappingsAsync(channelsToSkip); // 示例
                    await _testRecordService.SaveTestRecordsAsync(channelsToSkip); // 保存跳过的记录

                    await _messageService.ShowAsync("操作成功", $"{channelsToSkip.Count} 个通道已成功标记为跳过。", MessageBoxButton.OK);
                }
                else
                {
                    await _messageService.ShowAsync("提示", "未找到与选定模块关联的可跳过通道。", MessageBoxButton.OK);
                }
            }
            catch (Exception ex)
            {
                await _messageService.ShowAsync("错误", $"跳过模块时发生错误: {ex.Message}", MessageBoxButton.OK);
                System.Diagnostics.Debug.WriteLine($"ConfirmSkipModule Error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                StatusMessage = string.Empty;
                IsSkipModuleOpen = false;
                SkipReason = string.Empty; // 清空原因
                RefreshModules(); // 取消模块选择

                // UI 更新
                if (modifiedChannelIds.Any())
                {
                    // 触发属性变更，让UI知道数据已更新
                    // RaisePropertyChanged(nameof(AllChannels)); // 如果AllChannels本身被替换
                    // 或者如果AllChannels是ObservableCollection并且ChannelMapping实现了INotifyPropertyChanged，则其内部属性更改会自动通知绑定
                    // 对于集合视图的更新，可以这样做：
                    CollectionViewSource.GetDefaultView(AllChannels)?.Refresh();
                    CollectionViewSource.GetDefaultView(CurrentChannels)?.Refresh();
                }
                UpdatePointStatistics();
                RefreshBatchStatus();
                ExportTestResultsCommand.RaiseCanExecuteChanged();
                // OnChannelStatesModified(modifiedChannelIds); // 或者通过事件机制更新
            }
        }

        private void CancelSkipModule()
        {
            IsSkipModuleOpen = false;
            SkipReason = string.Empty;
            }
            //如果成功点位为112则也可以导出
            if (SuccessPointCount == TotalPointCount)
            {
                //成功点位
                ExportTestResultsCommand.RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// 保存单个通道的测试记录
        /// </summary>
        /// <param name="channel">需要保存记录的通道对象</param>
        /// <returns>保存是否成功的布尔值任务</returns>
        /// <remarks>
        /// 将单个通道的测试结果保存到数据库
        /// </remarks>
        private async Task<bool> SaveSingleChannelTestRecordAsync(ChannelMapping channel)
        {
            try
            {
                if (channel == null)
                    return false;

                // 确保通道有测试标识
                if (string.IsNullOrEmpty(channel.TestTag))
                {
                    // 使用当前批次名作为TestTag的一部分，以确保同一测试批次的记录具有相同的TestTag
                    string batchInfo = string.IsNullOrEmpty(channel.TestBatch) ? "UNKNOWN" : channel.TestBatch;
                    channel.TestTag = $"TEST_{batchInfo}_{DateTime.Now:yyyyMMdd}";
                }

                // 记录最终测试时间
                channel.FinalTestTime = DateTime.Now;

                // 调用测试记录服务保存记录
                bool result = await _testRecordService.SaveTestRecordAsync(channel);

                if (result)
                {
                    System.Diagnostics.Debug.WriteLine($"通道 {channel.VariableName} 测试记录已保存");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"通道 {channel.VariableName} 测试记录保存失败");
                }

                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存单个通道测试记录时出错: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region 6、手动测试 - AI通道
        /// <summary>
        /// 打开AI通道手动测试窗口
        /// </summary>
        /// <param name="channel">需要手动测试的AI通道</param>
        /// <remarks>
        /// 该方法打开AI通道的手动测试窗口，执行以下操作：
        /// 1. 设置当前选中的通道
        /// 2. 初始化手动测试状态
        /// 3. 打开AI手动测试窗口
        /// </remarks>
        private async void OpenAIManualTest(ChannelMapping channel)
        {
            try
            {
                if (channel != null && channel.ModuleType?.ToLower() == "ai")
                {
                    CurrentChannel = channel;
                    CurrentTestResult = channel; // 保持与当前通道同步

                    // 调用 ChannelStateManager 来准备手动测试状态
                    _channelStateManager.BeginManualTest(channel); 

                    // UI 更新逻辑 (RaisePropertyChanged, UpdatePointStatistics 等)
                    // 应在状态管理器调用后，并且如果事件聚合器未处理这些，则在此处显式调用
                    RaisePropertyChanged(nameof(CurrentChannel));
                    RaisePropertyChanged(nameof(AllChannels)); // 或更细粒度的通知
                    CollectionViewSource.GetDefaultView(AllChannels)?.Refresh(); // 确保DataGrid等控件看到变更
                    UpdatePointStatistics();
                    RefreshBatchStatus(); // 如果适用
                    ExportTestResultsCommand.RaiseCanExecuteChanged();

                    // AISetValue 的初始化可以保留，因为它服务于UI输入，不直接是ChannelMapping的状态
                    if (channel.HighLimit.HasValue && channel.LowLimit.HasValue && channel.HighLimit.Value > channel.LowLimit.Value)
                    {
                        AISetValue = (Math.Round((new Random().NextDouble() * (channel.HighLimit.Value - channel.LowLimit.Value) + channel.LowLimit.Value), 3)).ToString();
                    }
                    else
                    {
                        AISetValue = channel.LowLimit.HasValue ? channel.LowLimit.Value.ToString() : "0"; // 默认值
                    }

                    IsAIManualTestOpen = true;

                    // 移除原先在此处直接初始化子测试状态的逻辑，例如：
                    // channel.ShowValueStatus = "未测试";
                    // channel.HighAlarmStatus = "未测试"; 等等，这些已由 BeginManualTest 处理。
                    // 也移除原先在此处直接修改 channel.ResultText 的逻辑。

                    // 报警值监控循环 (如果还需要，可以保留，因为它读取PLC值更新UI绑定属性，不修改ChannelMapping状态)
                    while (IsAIManualTestOpen && CurrentChannel != null && CurrentChannel.Id == channel.Id) // 确保 CurrentChannel 仍是这个 channel
                    {
                        try
                        {
                            if (!string.IsNullOrEmpty(CurrentChannel.SLSetPointCommAddress))
                                AILowSetValue = (await _targetPlc.ReadAnalogValueAsync(CurrentChannel.SLSetPointCommAddress.Substring(1))).Data.ToString();
                            if (!string.IsNullOrEmpty(CurrentChannel.SLLSetPointCommAddress))
                                AILowLowSetValue = (await _targetPlc.ReadAnalogValueAsync(CurrentChannel.SLLSetPointCommAddress.Substring(1))).Data.ToString();
                            if (!string.IsNullOrEmpty(CurrentChannel.SHSetPointCommAddress))
                                AIHighSetValue = (await _targetPlc.ReadAnalogValueAsync(CurrentChannel.SHSetPointCommAddress.Substring(1))).Data.ToString();
                            if (!string.IsNullOrEmpty(CurrentChannel.SHHSetPointCommAddress))
                                AIHighHighSetValue = (await _targetPlc.ReadAnalogValueAsync(CurrentChannel.SHHSetPointCommAddress.Substring(1))).Data.ToString();
                            await Task.Delay(500);
                        }
                        catch (Exception e)
                        {
                            // 不再使用 MessageBox.Show，而是通过服务或记录日志
                            System.Diagnostics.Debug.WriteLine($"AI手动测试报警值监控失败: {e.Message}");
                            // _messageService.ShowAsync("错误", "报警值监控失败", MessageBoxButton.OK); // 如果需要通知用户
                            break; // 退出循环以避免连续错误
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"打开AI手动测试窗口失败: {ex.Message}");
                await _messageService.ShowAsync("错误", $"打开AI手动测试窗口失败: {ex.Message}", MessageBoxButton.OK);
            }
        }
        /// <summary>
        /// 关闭AI通道手动测试窗口
        /// </summary>
        /// <remarks>
        /// 该方法关闭AI通道的手动测试窗口，并清空当前选中的通道和测试结果。
        /// </remarks>
        private void ExecuteCloseAIManualTest()
        {
            try
            {
                // 设置AI手动测试窗口打开状态为false
                IsAIManualTestOpen = false;

                // 检查当前通道是否通过了测试
                if (CurrentChannel != null && CurrentChannel.ShowValueStatus == "通过")
                {
                    // 如果手动测试已通过，检查是否所有测试都已完成
                    _testTaskManager.CompleteAllTestsAsync();
                }

                // 刷新批次状态
                RefreshBatchStatus();

                // 更新点位统计
                UpdatePointStatistics();
            }
            catch (Exception ex)
            {
                await _messageService.ShowAsync("错误", $"关闭AI手动测试窗口失败: {ex.Message}", MessageBoxButton.OK);
            }
        }
        /// <summary>
        /// 发送AI测试值
        /// </summary>
        /// <param name="channel">需要测试的AI通道</param>
        /// <remarks>
        /// 该方法将用户输入的测试值发送到测试PLC，执行以下操作：
        /// 1. 验证输入的测试值是否有效
        /// 2. 将测试值转换为百分比值
        /// 3. 将测试值写入测试PLC的相应地址
        /// 4. 更新手动测试状态
        /// </remarks>
        private async void ExecuteSendAITestValue(ChannelMapping channel)
        {
            try
            {
                // 实现发送AI测试值的逻辑
                // 直接执行业务逻辑，不弹出消息框
                await _testPlc.WriteAnalogValueAsync(channel.TestPLCCommunicationAddress.Substring(1), 
                    ChannelRangeConversion.RealValueToPercentage(channel, AISetValue));
            }
            catch (Exception ex)
            {
                await _messageService.ShowAsync("错误", $"发送AI测试值失败: {ex.Message}", MessageBoxButton.OK);
            }
        }
        /// <summary>
        /// 确认AI显示值
        /// </summary>
        /// <param name="channel">需要确认的AI通道</param>
        /// <remarks>
        /// 该方法确认AI通道的显示值是否正确，执行以下操作：
        /// 1. 将通道的显示值状态设置为"通过"
        /// 2. 更新手动测试状态
        /// 3. 检查并更新通道的总体测试状态
        /// </remarks>
        private void ExecuteConfirmAIValue(ChannelMapping channel)
        {
            try
            {
                if (channel != null)
                {
                    _channelStateManager.SetManualSubTestOutcome(channel, ManualTestItem.ShowValue, true, DateTime.Now);

                    RaisePropertyChanged(nameof(CurrentChannel)); // CurrentChannel 应该在手动测试UI中被正确设置
                    RaisePropertyChanged(nameof(AllChannels)); // Or CollectionViewSource.GetDefaultView(AllChannels)?.Refresh();
                    UpdatePointStatistics();
                    RefreshBatchStatus(); // 如果适用
                    ExportTestResultsCommand.RaiseCanExecuteChanged();
                }
            }
            catch (Exception ex)
            {
                _messageService.ShowAsync("错误", $"确认AI显示值失败: {ex.Message}", MessageBoxButton.OK);
                System.Diagnostics.Debug.WriteLine($"ExecuteConfirmAIValue Error: {ex.Message}");
            }
        }
        /// <summary>
        /// 发送AI高报测试值
        /// </summary>
        /// <param name="channel">需要测试高报的AI通道</param>
        /// <remarks>
        /// 该方法发送高报测试值到测试PLC，执行以下操作：
        /// 1. 检查通道是否配置了高报值
        /// 2. 将高报值转换为百分比值
        /// 3. 将高报值写入测试PLC的相应地址
        /// 4. 更新高报测试状态
        /// </remarks>
        private async void ExecuteSendAIHighAlarm(ChannelMapping channel)
        {
            try
            {
                // 实现发送AI高报警测试信号的逻辑
                // 直接执行业务逻辑，不弹出消息框
                await _testPlc.WriteAnalogValueAsync(channel.TestPLCCommunicationAddress.Substring(1), ChannelRangeConversion.RealValueToPercentage(channel, channel.HighLimit) + 5f);
            }
            catch (Exception ex)
            {
                await _messageService.ShowAsync("错误", $"发送AI高报警测试信号失败: {ex.Message}", MessageBoxButton.OK);
            }
        }
        /// <summary>
        /// 发送AI高高报测试值
        /// </summary>
        /// <param name="channel">需要测试高高报的AI通道</param>
        /// <remarks>
        /// 该方法发送高高报测试值到测试PLC，执行以下操作：
        /// 1. 检查通道是否配置了高高报值
        /// 2. 将高高报值转换为百分比值
        /// 3. 将高高报值写入测试PLC的相应地址
        /// 4. 更新高高报测试状态
        /// </remarks>
        private async void ExecuteSendAIHighHighAlarm(ChannelMapping channel)
        {
            try
            {
                // 实现发送AI高报警测试信号的逻辑
                // 直接执行业务逻辑，不弹出消息框
                await _testPlc.WriteAnalogValueAsync(channel.TestPLCCommunicationAddress.Substring(1), ChannelRangeConversion.RealValueToPercentage(channel, channel.HighHighLimit) + 5f);
            }
            catch (Exception ex)
            {
                await _messageService.ShowAsync("错误", $"发送AI高报警测试信号失败: {ex.Message}", MessageBoxButton.OK);
            }
        }
        /// <summary>
        /// 复位AI高报测试
        /// </summary>
        /// <param name="channel">需要复位高报的AI通道</param>
        /// <remarks>
        /// 该方法将AI通道的高报测试值复位到正常范围内，执行以下操作：
        /// 1. 将测试值设置为正常范围内的值（通常是50%量程值）
        /// 2. 将测试值写入测试PLC的相应地址
        /// 3. 更新高报测试状态
        /// </remarks>
        private async void ExecuteResetAIHighAlarm(ChannelMapping channel)
        {
            try
            {
                // 实现重置AI高报警测试信号的逻辑
                // 直接执行业务逻辑，不弹出消息框
                await _testPlc.WriteAnalogValueAsync(channel.TestPLCCommunicationAddress.Substring(1),
                    ChannelRangeConversion.RealValueToPercentage(channel, AISetValue));
            }
            catch (Exception ex)
            {
                await _messageService.ShowAsync("错误", $"重置AI高报警测试信号失败: {ex.Message}", MessageBoxButton.OK);
            }
        }
        /// <summary>
        /// 确认AI高报测试
        /// </summary>
        /// <param name="channel">需要确认高报的AI通道</param>
        /// <remarks>
        /// 该方法确认AI通道的高报功能是否正常，执行以下操作：
        /// 1. 将通道的高报状态设置为"通过"
        /// 2. 更新高报测试状态
        /// 3. 检查并更新通道的总体测试状态
        /// </remarks>
        private void ExecuteConfirmAIHighAlarm(ChannelMapping channel)
        {
            try
            {
                if (channel != null)
                {
                    // 假设高报和高高报通过一个按钮确认，或者需要拆分
                    // 为简化，这里合并处理，实际可能需要根据UI设计调整
                    _channelStateManager.SetManualSubTestOutcome(channel, ManualTestItem.HighAlarm, true, DateTime.Now);
                    // 如果高高报是独立控制和确认的，需要独立的ManualTestItem和调用
                    _channelStateManager.SetManualSubTestOutcome(channel, ManualTestItem.HighHighAlarm, true, DateTime.Now);


                    RaisePropertyChanged(nameof(CurrentChannel));
                    RaisePropertyChanged(nameof(AllChannels));
                    UpdatePointStatistics();
                    RefreshBatchStatus();
                    ExportTestResultsCommand.RaiseCanExecuteChanged();
                }
            }
            catch (Exception ex)
            {
                _messageService.ShowAsync("错误", $"确认AI高报警失败: {ex.Message}", MessageBoxButton.OK);
                System.Diagnostics.Debug.WriteLine($"ExecuteConfirmAIHighAlarm Error: {ex.Message}");
            }
        }
        /// <summary>
        /// 发送AI低报测试值
        /// </summary>
        /// <param name="channel">需要测试低报的AI通道</param>
        /// <remarks>
        /// 该方法发送低报测试值到测试PLC，执行以下操作：
        /// 1. 检查通道是否配置了低报值
        /// 2. 将低报值转换为百分比值
        /// 3. 将低报值写入测试PLC的相应地址
        /// 4. 更新低报测试状态
        /// </remarks>
        private async void ExecuteSendAILowAlarm(ChannelMapping channel)
        {
            try
            {
                // 实现发送AI低报警测试信号的逻辑
                // 直接执行业务逻辑，不弹出消息框
                await _testPlc.WriteAnalogValueAsync(channel.TestPLCCommunicationAddress.Substring(1), ChannelRangeConversion.RealValueToPercentage(channel, channel.LowLimit) - 5f);
            }
            catch (Exception ex)
            {
                await _messageService.ShowAsync("错误", $"发送AI低报警测试信号失败: {ex.Message}", MessageBoxButton.OK);
            }
        }
        /// <summary>
        /// 发送AI低低报测试值
        /// </summary>
        /// <param name="channel">需要测试低低报的AI通道</param>
        /// <remarks>
        /// 该方法发送低低报测试值到测试PLC，执行以下操作：
        /// 1. 检查通道是否配置了低低报值
        /// 2. 将低低报值转换为百分比值
        /// 3. 将低低报值写入测试PLC的相应地址
        /// 4. 更新低低报测试状态
        /// </remarks>
        private async void ExecuteSendAILowLowAlarm(ChannelMapping channel)
        {
            try
            {
                // 实现发送AI低报警测试信号的逻辑
                // 直接执行业务逻辑，不弹出消息框
                await _testPlc.WriteAnalogValueAsync(channel.TestPLCCommunicationAddress.Substring(1), ChannelRangeConversion.RealValueToPercentage(channel, channel.LowLowLimit) - 5f);
            }
            catch (Exception ex)
            {
                await _messageService.ShowAsync("错误", $"发送AI低报警测试信号失败: {ex.Message}", MessageBoxButton.OK);
            }
        }
        /// <summary>
        /// 复位AI低报测试
        /// </summary>
        /// <param name="channel">需要复位低报的AI通道</param>
        /// <remarks>
        /// 该方法将AI通道的低报测试值复位到正常范围内，执行以下操作：
        /// 1. 将测试值设置为正常范围内的值（通常是50%量程值）
        /// 2. 将测试值写入测试PLC的相应地址
        /// 3. 更新低报测试状态
        /// </remarks>
        private async void ExecuteResetAILowAlarm(ChannelMapping channel)
        {
            try
            {
                // 实现重置AI低报警测试信号的逻辑
                // 直接执行业务逻辑，不弹出消息框
                await _testPlc.WriteAnalogValueAsync(channel.TestPLCCommunicationAddress.Substring(1),
                    ChannelRangeConversion.RealValueToPercentage(channel, AISetValue));
            }
            catch (Exception ex)
            {
                await _messageService.ShowAsync("错误", $"重置AI低报警测试信号失败: {ex.Message}", MessageBoxButton.OK);
            }
        }
        /// <summary>
        /// 确认AI低报测试
        /// </summary>
        /// <param name="channel">需要确认低报的AI通道</param>
        /// <remarks>
        /// 该方法确认AI通道的低报功能是否正常，执行以下操作：
        /// 1. 将通道的低报状态设置为"通过"
        /// 2. 更新低报测试状态
        /// 3. 检查并更新通道的总体测试状态
        /// </remarks>
        private void ExecuteConfirmAILowAlarm(ChannelMapping channel)
        {
            try
            {
                if (channel != null)
                {
                    _channelStateManager.SetManualSubTestOutcome(channel, ManualTestItem.LowAlarm, true, DateTime.Now);
                    // 如果低低报是独立控制和确认的，需要独立的ManualTestItem和调用
                    _channelStateManager.SetManualSubTestOutcome(channel, ManualTestItem.LowLowAlarm, true, DateTime.Now);

                    RaisePropertyChanged(nameof(CurrentChannel));
                    RaisePropertyChanged(nameof(AllChannels));
                    UpdatePointStatistics();
                    RefreshBatchStatus();
                    ExportTestResultsCommand.RaiseCanExecuteChanged();
                }
            }
            catch (Exception ex)
            {
                _messageService.ShowAsync("错误", $"确认AI低报警失败: {ex.Message}", MessageBoxButton.OK);
                System.Diagnostics.Debug.WriteLine($"ExecuteConfirmAILowAlarm Error: {ex.Message}");
            }
        }
        /// <summary>
        /// 确认AI报警值设定测试
        /// </summary>
        /// <param name="channel">需要确认报警设定值的AI通道</param>
        /// <remarks>
        /// 该方法确认AI通道的报警值设定功能是否正常，执行以下操作：
        /// 1. 将通道的报警值设定状态设置为"通过"
        /// 2. 更新报警值设定测试状态
        /// 3. 检查并更新通道的总体测试状态
        /// </remarks>
        private void ExecuteConfirmAIAlarmValueSet(ChannelMapping channel)
        {
            try
            {
                if (channel != null)
                {
                    _channelStateManager.SetManualSubTestOutcome(channel, ManualTestItem.AlarmValueSet, true, DateTime.Now);

                    RaisePropertyChanged(nameof(CurrentChannel));
                    RaisePropertyChanged(nameof(AllChannels));
                    UpdatePointStatistics();
                    RefreshBatchStatus();
                    ExportTestResultsCommand.RaiseCanExecuteChanged();
                }
            }
            catch (Exception ex)
            {
                // MessageBox.Show($"确认AI低报警失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error); // 原有代码的笔误，应为AI报警值设定
                _messageService.ShowAsync("错误", $"确认AI报警值设定失败: {ex.Message}", MessageBoxButton.OK);
                System.Diagnostics.Debug.WriteLine($"ExecuteConfirmAIAlarmValueSet Error: {ex.Message}");
            }
        }
        /// <summary>
        /// 发送AI维护功能测试信号
        /// </summary>
        /// <remarks>
        /// 该方法关闭DI通道的手动测试窗口，并清空当前选中的通道和测试结果。
        /// </remarks>
        private void ExecuteCloseDIManualTest()
        {
            try
            {
                // 设置DI手动测试窗口打开状态为false
                IsDIManualTestOpen = false;

                // 检查当前通道是否通过了测试
                if (CurrentChannel != null && CurrentChannel.ShowValueStatus == "通过")
                {
                    // 如果手动测试已通过，检查是否所有测试都已完成
                    _testTaskManager.CompleteAllTestsAsync();
                }

                // 刷新批次状态
                RefreshBatchStatus();

                // 更新点位统计
                UpdatePointStatistics();
            }
            catch (Exception ex)
            {
                await _messageService.ShowAsync("错误", $"关闭DI手动测试窗口失败: {ex.Message}", MessageBoxButton.OK);
            }
        }
        /// <summary>
        /// 发送DI测试信号
        /// </summary>
        /// <param name="channel">需要测试的DI通道</param>
        /// <remarks>
        /// 该方法发送DI测试信号到测试PLC，执行以下操作：
        /// 1. 检查通道是否有效
        /// 2. 将DI信号设置为激活状态
        /// 3. 将DI状态写入测试PLC的相应地址
        /// 4. 更新DI测试状态
        /// </remarks>
        private async void ExecuteSendDITest(ChannelMapping channel)
        {
            try
            {
                // 实现发送DI测试信号的逻辑
                // 直接执行业务逻辑，不弹出消息框
                var result = _targetPlc;
                await _testPlc.WriteDigitalValueAsync(channel.TestPLCCommunicationAddress, true);
            }
            catch (Exception ex)
            {
                await _messageService.ShowAsync("错误", $"发送DI测试信号失败: {ex.Message}", MessageBoxButton.OK);
            }
        }
        /// <summary>
        /// 复位DI测试信号
        /// </summary>
        /// <param name="channel">需要复位的DI通道</param>
        /// <remarks>
        /// 该方法将DI通道的测试信号复位到非激活状态，执行以下操作：
        /// 1. 检查通道是否有效
        /// 2. 将DI信号设置为非激活状态
        /// 3. 将DI状态写入测试PLC的相应地址
        /// 4. 更新DI测试状态
        /// </remarks>
        private async void ExecuteResetDI(ChannelMapping channel)
        {
            try
            {
                // 实现重置DI测试信号的逻辑
                // 直接执行业务逻辑，不弹出消息框
                await _testPlc.WriteDigitalValueAsync(channel.TestPLCCommunicationAddress, false);
            }
            catch (Exception ex)
            {
                await _messageService.ShowAsync("错误", $"重置DI测试信号失败: {ex.Message}", MessageBoxButton.OK);
            }
        }
        /// <summary>
        /// 确认DI测试
        /// </summary>
        /// <param name="channel">需要确认的DI通道</param>
        /// <remarks>
        /// 该方法确认DI通道的显示值是否正确，执行以下操作：
        /// 1. 将通道的显示值状态设置为"通过"
        /// 2. 更新DI测试状态
        /// 3. 检查并更新通道的总体测试状态
        /// </remarks>
        private void ExecuteConfirmDI(ChannelMapping channel)
        {
            try
            {
                if (channel != null)
                {
                    _channelStateManager.SetManualSubTestOutcome(channel, ManualTestItem.ShowValue, true, DateTime.Now);

                    RaisePropertyChanged(nameof(CurrentChannel));
                    RaisePropertyChanged(nameof(AllChannels));
                    CollectionViewSource.GetDefaultView(AllChannels)?.Refresh();
                    UpdatePointStatistics();
                    RefreshBatchStatus();
                    ExportTestResultsCommand.RaiseCanExecuteChanged();
                }
            }
            catch (Exception ex)
            {
                _messageService.ShowAsync("错误", $"确认DI测试失败: {ex.Message}", MessageBoxButton.OK);
                System.Diagnostics.Debug.WriteLine($"ExecuteConfirmDI Error: {ex.Message}");
            }
        }
        #endregion

        #region 8、手动测试-AO
        /// <summary>
        /// 打开AO手动测试窗口
        /// </summary>
        /// <param name="channel">要测试的通道</param>
        /// <summary>
        /// 打开AO通道手动测试窗口
        /// </summary>
        /// <param name="channel">需要手动测试的AO通道</param>
        /// <remarks>
        /// 该方法打开AO通道的手动测试窗口，执行以下操作：
        /// 1. 设置当前选中的通道
        /// 2. 初始化手动测试状态
        /// 3. 打开AO手动测试窗口
        /// </remarks>
        private async void OpenAOManualTest(ChannelMapping channel)
        {
            try
            {
                if (channel != null && (channel.ModuleType?.ToLower() == "ao" || channel.ModuleType?.ToLower() == "aonone"))
                {
                    CurrentChannel = channel;
                    CurrentTestResult = channel;

                    _channelStateManager.BeginManualTest(channel);

                    RaisePropertyChanged(nameof(CurrentChannel));
                    RaisePropertyChanged(nameof(AllChannels));
                    CollectionViewSource.GetDefaultView(AllChannels)?.Refresh();
                    UpdatePointStatistics();
                    RefreshBatchStatus();
                    ExportTestResultsCommand.RaiseCanExecuteChanged();

                    IsAOManualTestOpen = true;
                    AOCurrentValue = string.Empty; // 清空上次的值
                    
                    AOMonitorStatus = "停止监测";
                    while (IsAOManualTestOpen && CurrentChannel != null && CurrentChannel.Id == channel.Id)
                    {
                        try
                        {
                            if (!string.IsNullOrEmpty(CurrentChannel.TestPLCCommunicationAddress))
                            {
                                var readResult = await _testPlc.ReadAnalogValueAsync(CurrentChannel.TestPLCCommunicationAddress.Substring(1));
                                if (readResult.IsSuccess)
                                {
                                    AOCurrentValue = ChannelRangeConversion.PercentageToRealValue(CurrentChannel, readResult.Data).ToString("F3");
                                }
                                else
                                {
                                    AOCurrentValue = "读取失败";
                                }
                            }
                            else
                            {
                                AOCurrentValue = "反馈点地址无效";
                            }
                            await Task.Delay(500);
                        }
                        catch (Exception e)
                        {
                            System.Diagnostics.Debug.WriteLine($"AO手动测试监控失败: {e.Message}");
                            AOCurrentValue = "监控异常";
                            break;
                        }
                    }
                    AOMonitorStatus = "开始监测";
                    AOCurrentValue = string.Empty; 
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"打开AO手动测试窗口失败: {ex.Message}");
                await _messageService.ShowAsync("错误", $"打开AO手动测试窗口失败: {ex.Message}", MessageBoxButton.OK);
            }
        }
        /// <summary>
        /// 关闭AO通道手动测试窗口
        /// </summary>
        /// <remarks>
        /// 该方法关闭AO通道的手动测试窗口，并清空当前选中的通道和测试结果。
        /// </remarks>
        private void ExecuteCloseAOManualTest()
        {
            try
            {
                // 设置AO手动测试窗口打开状态为false
                IsAOManualTestOpen = false;

                // 检查当前通道是否通过了测试
                if (CurrentChannel != null && CurrentChannel.ShowValueStatus == "通过")
                {
                    // 如果手动测试已通过，检查是否所有测试都已完成
                    _testTaskManager.CompleteAllTestsAsync();
                }

                // 刷新批次状态
                RefreshBatchStatus();

                // 更新点位统计
                UpdatePointStatistics();
            }
            catch (Exception ex)
            {
                await _messageService.ShowAsync("错误", $"关闭AO手动测试窗口失败: {ex.Message}", MessageBoxButton.OK);
            }
        }
        /// <summary>
        /// 启动AO监测
        /// </summary>
        /// <param name="channel">需要监测的AO通道</param>
        /// <remarks>
        /// 该方法启动AO通道的监测过程，执行以下操作：
        /// 1. 设置当前监测的AO通道
        /// 2. 循环读取AO通道的模拟值
        /// 3. 更新AO监测状态和当前值
        /// </remarks>
        private async void ExecuteStartAOMonitor(ChannelMapping channel)
        {
            try
            {
                if (channel != null && channel.ShowValueStatus != "通过")
                {
                    // 设置当前监测的AO通道
                    CurrentChannel = channel;
                    CurrentTestResult = channel;
                    // 启动AO监测逻辑，当窗口关闭或者
                    while (channel.ShowValueStatus != "通过" && IsAOManualTestOpen)
                    {
                        float persentValue = (await _testPlc.ReadAnalogValueAsync(channel.TestPLCCommunicationAddress.Substring(1))).Data;
                        AOCurrentValue = ChannelRangeConversion.PercentageToRealValue(channel, persentValue).ToString("F3");
                        await Task.Delay(500);
                    }
                    // 直接执行业务逻辑，不弹出消息框
                }
            }
            catch (Exception ex)
            {
                await _messageService.ShowAsync("错误", $"启动AO监测失败: {ex.Message}", MessageBoxButton.OK);
            }
        }

        //private async void ExecuteSaveAO0(ChannelMapping channel)
        //{
        //    channel.Value0Percent = Convert.ToDouble(AOCurrentValue);
        //}
        //private async void ExecuteSaveAO25(ChannelMapping channel)
        //{
        //    channel.Value25Percent = Convert.ToDouble(AOCurrentValue);
        //}
        //private async void ExecuteSaveAO50(ChannelMapping channel)
        //{
        //    channel.Value50Percent = Convert.ToDouble(AOCurrentValue);
        //}
        //private async void ExecuteSaveAO75(ChannelMapping channel)
        //{
        //    channel.Value75Percent = Convert.ToDouble(AOCurrentValue);
        //}
        //private async void ExecuteSaveAO100(ChannelMapping channel)
        //{
        //    channel.Value100Percent = Convert.ToDouble(AOCurrentValue);
        //}
        /// <summary>
        /// 确认AO测试
        /// </summary>
        /// <param name="channel">需要确认的AO通道</param>
        /// <remarks>
        /// 该方法确认AO通道的显示值是否正确，执行以下操作：
        /// 1. 将通道的显示值状态设置为"通过"
        /// 2. 更新AO测试状态
        /// 3. 检查并更新通道的总体测试状态
        /// </remarks>
        private void ExecuteConfirmAO(ChannelMapping channel)
        {
            try
            {
                if (channel != null)
                {
                    _channelStateManager.SetManualSubTestOutcome(channel, ManualTestItem.ShowValue, true, DateTime.Now);
                    // AO的其他手动子测试项如TrendCheck, ReportCheck有单独的确认命令

                    RaisePropertyChanged(nameof(CurrentChannel));
                    RaisePropertyChanged(nameof(AllChannels));
                    CollectionViewSource.GetDefaultView(AllChannels)?.Refresh();
                    UpdatePointStatistics();
                    RefreshBatchStatus();
                    ExportTestResultsCommand.RaiseCanExecuteChanged();
                }
            }
            catch (Exception ex)
            {
                _messageService.ShowAsync("错误", $"确认AO测试失败: {ex.Message}", MessageBoxButton.OK);
                System.Diagnostics.Debug.WriteLine($"ExecuteConfirmAO Error: {ex.Message}");
            }
        }
        #endregion

        #region 9、手动测试-DO
        /// <summary>
        /// 打开DO手动测试窗口
        /// </summary>
        /// <param name="channel">要测试的通道</param>
        /// <summary>
        /// 打开DO通道手动测试窗口
        /// </summary>
        /// <param name="channel">需要手动测试的DO通道</param>
        /// <remarks>
        /// 该方法打开DO通道的手动测试窗口，执行以下操作：
        /// 1. 设置当前选中的通道
        /// 2. 初始化手动测试状态
        /// 3. 打开DO手动测试窗口
        /// </remarks>
        private async void OpenDOManualTest(ChannelMapping channel)
        {
            try
            {
                if (channel != null && (channel.ModuleType?.ToLower() == "do" || channel.ModuleType?.ToLower() == "donone"))
                {
                    CurrentChannel = channel;
                    CurrentTestResult = channel;

                    _channelStateManager.BeginManualTest(channel);

                    RaisePropertyChanged(nameof(CurrentChannel));
                    RaisePropertyChanged(nameof(AllChannels));
                    CollectionViewSource.GetDefaultView(AllChannels)?.Refresh();
                    UpdatePointStatistics();
                    RefreshBatchStatus();
                    ExportTestResultsCommand.RaiseCanExecuteChanged();
                    
                    IsDOManualTestOpen = true;
                    DOCurrentValue = string.Empty; // 清空上次的值

                    // 监控DO点当前值 (通常是读取测试PLC上连接到此DO输出的DI点)
                    DOMonitorStatus = "停止监测";
                    while (IsDOManualTestOpen && CurrentChannel != null && CurrentChannel.Id == channel.Id)
                    {
                        try
                        {
                            if (!string.IsNullOrEmpty(CurrentChannel.TestPLCCommunicationAddress)) // 使用TestPLCCommunicationAddress作为反馈点
                            {
                                var readResult = await _testPlc.ReadDigitalValueAsync(CurrentChannel.TestPLCCommunicationAddress.Substring(1));
                                DOCurrentValue = readResult.IsSuccess ? (readResult.Data ? "ON" : "OFF") : "读取失败";
                            }
                            else
                            {
                                DOCurrentValue = "反馈点地址无效";
                            }
                            await Task.Delay(500);
                        }
                        catch (Exception e)
                        {
                            System.Diagnostics.Debug.WriteLine($"DO手动测试监控失败: {e.Message}");
                            DOCurrentValue = "监控异常";
                            break;
                        }
                    }
                    DOMonitorStatus = "开始监测";
                    DOCurrentValue = string.Empty; // 窗口关闭或监控停止后清空
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"打开DO手动测试窗口失败: {ex.Message}");
                await _messageService.ShowAsync("错误", $"打开DO手动测试窗口失败: {ex.Message}", MessageBoxButton.OK);
            }
        }
        /// <summary>
        /// 关闭DO通道手动测试窗口
        /// </summary>
        /// <remarks>
        /// 该方法关闭历史记录查看窗口。
        /// </remarks>
        private void CloseHistoryRecords()
        {
            IsHistoryRecordsOpen = false;
        }
        /// <summary>
        /// 恢复选中的历史测试记录
        /// </summary>
        /// <remarks>
        /// 该方法用于恢复用户在历史记录窗口中选择的测试批次记录。
        /// 执行流程：
        /// 1. 验证是否选择了测试批次
        /// 2. 显示确认对话框，提醒用户当前数据将被覆盖
        /// 3. 调用测试记录服务恢复选中批次的测试记录
        /// 4. 更新AllChannels集合和原始通道集合
        /// 5. 更新批次信息和点位统计数据
        /// 6. 关闭历史记录窗口并显示成功消息
        /// </remarks>
        private async void RestoreTestRecords()
        {
            if (SelectedTestBatch == null)
            {
                await _messageService.ShowAsync("提示", "请先选择一个测试批次", MessageBoxButton.OK);
                return;
            }

            bool wasPopupOpen = IsHistoryRecordsOpen;
            try
            {
                IsHistoryRecordsOpen = false; 

                ConfirmDialogView confirmDialog = new ConfirmDialogView(
                    $"确定要恢复测试批次 {SelectedTestBatch.TestTag} 的记录吗？当前数据将被覆盖。",
                    "确认恢复");
                confirmDialog.Owner = Application.Current.MainWindow;
                bool? dialogResult = confirmDialog.ShowDialog();

                if (dialogResult == true)
                {
                    IsLoading = true;
                    StatusMessage = $"正在恢复测试记录 {SelectedTestBatch.TestTag}...";
                    var records = await _testRecordService.RestoreTestRecordsAsync(SelectedTestBatch.TestTag);

                    if (records != null && records.Any())
                    {
                        if (AllChannels is not null) { AllChannels.Clear(); }
                        if (OriginalAllChannels is not null) { OriginalAllChannels.Clear(); }
                        AllChannels = new ObservableCollection<ChannelMapping>(records.OrderBy(c => c.TestId));
                        OriginalAllChannels = new ObservableCollection<ChannelMapping>(AllChannels);
                        await UpdateBatchInfoAsync();
                        UpdatePointStatistics();
                        await _messageService.ShowAsync("恢复成功", $"已成功恢复 {records.Count} 条测试记录", MessageBoxButton.OK);
                    }
                    else
                    {
                        StatusMessage = $"测试记录恢复失败"; 
                        await _messageService.ShowAsync("提示", "未找到要恢复的测试记录，或记录为空。", MessageBoxButton.OK);
                        if (wasPopupOpen) IsHistoryRecordsOpen = true; 
                    }
                }
                else 
                {
                    if (wasPopupOpen) IsHistoryRecordsOpen = true;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"恢复测试记录时出错: {ex.Message}"; 
                System.Diagnostics.Debug.WriteLine($"恢复测试记录时出错: {ex.Message}");
                await _messageService.ShowAsync("错误", $"恢复测试记录时出错: {ex.Message}", MessageBoxButton.OK);
                if (wasPopupOpen) IsHistoryRecordsOpen = true; 
            }
            finally
            {
                IsLoading = false;
                if (!string.IsNullOrEmpty(StatusMessage) && StatusMessage != $"正在恢复测试记录 {SelectedTestBatch?.TestTag}...")
                {
                    // Don't clear a specific status message like "测试记录恢复失败"
                }
                else
                {
                    StatusMessage = string.Empty;
                }
            }
        }

        #endregion

        /// <summary>
        /// 确认接线完成后，启用测试按钮并准备测试环境
        /// </summary>
        /// <remarks>
        /// 此方法已弃用。ConfirmWiringCompleteCommand 现在直接执行 FinishWiring 方法。
        /// </remarks>
        [Obsolete("此方法已弃用。ConfirmWiringCompleteCommand 现在直接执行 FinishWiring 方法。", true)]
        private void ExecuteConfirmWiringComplete()
        {
            // 此方法的功能已由 FinishWiring 方法（通过 TestTaskManager）处理。
            System.Diagnostics.Debug.WriteLine("[DEPRECATED] ExecuteConfirmWiringComplete called. This method should be removed or its command retargeted.");
            // FinishWiring(); // 可以考虑调用，但更好的方式是直接绑定命令
            return; 

            // try
            // {
            //     // 原方法体 ...
            // }
            // catch (Exception ex)
            // {
            //     MessageBox.Show($"接线确认操作失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            // }
        }

        private async void ExecuteAllocateChannels()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "正在分配通道...";

                if (AllChannels == null || !AllChannels.Any())
                {
                    await _messageService.ShowAsync("提示", "没有可用的通道需要分配。", MessageBoxButton.OK);
                    return;
                }

                var allocatedChannels = await _channelMappingService.AllocateChannelsTestAsync(new ObservableCollection<ChannelMapping>(AllChannels)); // 传递副本以防服务修改原始集合

                if (allocatedChannels != null)
                {
                    AllChannels = new ObservableCollection<ChannelMapping>(allocatedChannels);
                    OriginalAllChannels = new ObservableCollection<ChannelMapping>(AllChannels); // 更新原始备份

                    UpdateCurrentChannels();
                    RaisePropertyChanged(nameof(AllChannels));
                    UpdatePointStatistics();
                    await RefreshBatchesFromChannelsAsync(); // 分配后需要刷新批次列表

                    await _messageService.ShowAsync("成功", "通道分配完成。", MessageBoxButton.OK);
                }
                else
                {
                    await _messageService.ShowAsync("失败", "通道分配失败或未返回任何通道。", MessageBoxButton.OK);
                }
            }
            catch (Exception ex)
            {
                await _messageService.ShowAsync("错误", $"分配通道时发生错误: {ex.Message}", MessageBoxButton.OK);
                System.Diagnostics.Debug.WriteLine($"ExecuteAllocateChannels Error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                StatusMessage = string.Empty;
            }
        }

        // 确保有一个方法可以从当前的 AllChannels 更新 Batches 集合
        private async Task RefreshBatchesFromChannelsAsync()
        {
            if (AllChannels == null) 
            {
                Batches = new ObservableCollection<BatchInfo>();
                SelectedBatch = null;
                RaisePropertyChanged(nameof(Batches));
                return;
            }
            var batchInfoList = await _channelMappingService.ExtractBatchInfoAsync(AllChannels);
            Batches = new ObservableCollection<BatchInfo>(batchInfoList.OrderBy(b => b.BatchName));
            
            if (SelectedBatch != null && !Batches.Any(b => b.BatchName == SelectedBatch.BatchName))
            {
                SelectedBatch = null; 
            }
            // else if (SelectedBatch == null && Batches.Any())
            // {
            //     SelectedBatch = Batches.FirstOrDefault(); // 可选：默认选择第一个
            // }
            RaisePropertyChanged(nameof(Batches));
        }

        private async void ClearChannelAllocationsAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "正在清除通道分配信息...";

                if (AllChannels == null || !AllChannels.Any())
                {
                    await _messageService.ShowAsync("提示", "没有通道分配信息需要清除。", MessageBoxButton.OK);
                    return;
                }

                var result = await _messageService.ShowAsync("确认操作", "确定要清除所有通道的分配信息吗？这将重置它们的测试状态。", MessageBoxButton.YesNo);
                if (result != MessageBoxResult.Yes)
                {
                    return;
                }
                
                // 服务层方法应该返回状态被 ChannelStateManager 重置后的通道集合
                var channelsAfterClearing = await _channelMappingService.ClearAllChannelAllocationsAsync(new ObservableCollection<ChannelMapping>(AllChannels)); 

                AllChannels = new ObservableCollection<ChannelMapping>(channelsAfterClearing);
                OriginalAllChannels = new ObservableCollection<ChannelMapping>(AllChannels); 
                
                // UI 更新
                UpdateCurrentChannels(); // 这会基于 SelectedChannelType 过滤新的 AllChannels
                                       // 如果 SelectedChannelType 本身依赖于批次，可能也需要重置
                RaisePropertyChanged(nameof(AllChannels)); 
                UpdatePointStatistics();
                await RefreshBatchesFromChannelsAsync(); // 批次信息会改变，SelectedBatch 可能会被置null
                
                // 重置与批次选择和测试流程相关的UI状态
                SelectedBatch = null; // 清除分配后，当前选定批次通常应失效
                // SelectedChannelType = null; // 如果通道类型选择依赖于批次，也应重置
                IsWiringCompleteBtnEnabled = true; // 清除后应可重新接线
                IsStartTestButtonEnabled = false; // 清除后不能直接开始测试
                // 其他可能依赖于通道分配状态的UI元素也应考虑重置

                await _messageService.ShowAsync("成功", "通道分配信息已清除。", MessageBoxButton.OK);
            }
            catch (Exception ex)
            {
                await _messageService.ShowAsync("错误", $"清除通道分配信息时发生错误: {ex.Message}", MessageBoxButton.OK);
                System.Diagnostics.Debug.WriteLine($"ClearChannelAllocationsAsync Error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                StatusMessage = string.Empty;
            }
        }

        private async void DeleteTestBatch()
        {
            if (SelectedTestBatch == null)
            {
                await _messageService.ShowAsync("提示", "请先选择一个测试批次", MessageBoxButton.OK); 
                return;
            }

            bool wasPopupOpen = IsHistoryRecordsOpen;
            try
            {
                IsHistoryRecordsOpen = false; 

                ConfirmDialogView confirmDialog = new ConfirmDialogView(
                    $"确定要删除测试批次 {SelectedTestBatch.TestTag} 的所有记录吗？此操作不可恢复。",
                    "确认删除");
                confirmDialog.Owner = Application.Current.MainWindow;
                bool? dialogResult = confirmDialog.ShowDialog();

                if (dialogResult == true)
                {
                    IsLoading = true;
                    StatusMessage = $"正在删除测试批次 {SelectedTestBatch.TestTag}...";

                    var success = await _testRecordService.DeleteTestBatchAsync(SelectedTestBatch.TestTag);

                    if (success)
                    {
                        TestBatches.Remove(SelectedTestBatch);
                        SelectedTestBatch = TestBatches.FirstOrDefault();
                        await _messageService.ShowAsync("删除成功", "测试批次已成功删除", MessageBoxButton.OK);
                    }
                    else
                    {
                        await _messageService.ShowAsync("删除失败", "删除测试批次失败", MessageBoxButton.OK);
                    }
                }
            }
            catch (Exception ex)
            {
                await _messageService.ShowAsync("错误", $"删除测试批次时出错: {ex.Message}", MessageBoxButton.OK);
            }
            finally
            {
                if (wasPopupOpen) 
                {
                    IsHistoryRecordsOpen = true;
                }
                IsLoading = false;
                StatusMessage = string.Empty;
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
        public DateTime? FirstTestTime { get; set; }
        public DateTime? LastTestTime { get; set; }

        public BatchInfo(string batchName, int itemCount)
        {
            BatchId = Guid.NewGuid().ToString("N");
            BatchName = batchName;
            ItemCount = itemCount;
            CreationDate = DateTime.Now;
            Status = "未开始";
        }

        // 添加无参构造函数
        public BatchInfo()
        {
            BatchId = Guid.NewGuid().ToString("N");
            CreationDate = DateTime.Now;
            Status = "未开始";
        }
    }
}