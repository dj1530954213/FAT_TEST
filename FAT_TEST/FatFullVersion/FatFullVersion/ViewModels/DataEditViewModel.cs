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
using Prism.Events;
using FatFullVersion.Events;
using System.Threading.Channels;
using System.Security.Cryptography;
using NPOI.SS.Formula.Functions;
using static NPOI.POIFS.Crypt.CryptoFunctions;
using static Org.BouncyCastle.Asn1.Cmp.Challenge;

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
        private readonly IPlcCommunication _plcCommunication;
        private readonly IMessageService _messageService;
        private readonly ITestResultExportService _testResultExportService;
        private readonly ITestRecordService _testRecordService;

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
        /// 测试PLC通信实例
        /// </summary>
        protected readonly IPlcCommunication TestPlcCommunication;

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
        public IEnumerable<ChannelMapping> GetAIChannels() => AllChannels?.Where(c => c.ModuleType?.ToLower() == "ai");

        /// <summary>
        /// 获取所有AO类型的通道
        /// </summary>
        public IEnumerable<ChannelMapping> GetAOChannels() => AllChannels?.Where(c => c.ModuleType?.ToLower() == "ao");

        /// <summary>
        /// 获取所有DI类型的通道
        /// </summary>
        public IEnumerable<ChannelMapping> GetDIChannels() => AllChannels?.Where(c => c.ModuleType?.ToLower() == "di");

        /// <summary>
        /// 获取所有DO类型的通道
        /// </summary>
        public IEnumerable<ChannelMapping> GetDOChannels() => AllChannels?.Where(c => c.ModuleType?.ToLower() == "do");

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
                    System.Diagnostics.Debug.WriteLine(
                        $"CurrentTestResult changed: {_currentTestResult?.VariableName}, HardPointTestResult: {_currentTestResult?.HardPointTestResult}");
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
                new DelegateCommand(ExecuteConfirmWiringComplete, CanExecuteConfirmWiringComplete);

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

        #endregion

        #region 1、构造函数和初始化数据结构
        /// <summary>
        /// 数据编辑视图模型构造函数
        /// </summary>
        /// <param name="pointDataService">点位数据服务，用于导入和处理点表配置</param>
        /// <param name="channelMappingService">通道映射服务，用于管理通道分配和批次信息</param>
        /// <param name="testTaskManager">测试任务管理器，用于控制测试流程</param>
        /// <param name="eventAggregator">事件聚合器，用于处理事件通信</param>
        /// <param name="testPlc">测试PLC通信接口，用于与测试设备通信</param>
        /// <param name="targetPlc">目标PLC通信接口，用于与被测设备通信</param>
        /// <param name="testResultExportService">测试结果导出服务，用于导出测试数据</param>
        /// <param name="testRecordService">测试记录服务，用于管理测试历史记录</param>
        public DataEditViewModel(
            IPointDataService pointDataService,
            IChannelMappingService channelMappingService,
            ITestTaskManager testTaskManager,
            IEventAggregator eventAggregator,
            IPlcCommunication testPlc,
            IPlcCommunication targetPlc,
            ITestResultExportService testResultExportService,
            ITestRecordService testRecordService
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

            // 初始化数据结构
            Initialize();
        }
        /// <summary>
        /// 初始化数据结构和命令
        /// </summary>
        /// <remarks>
        /// 该方法负责：
        /// 1. 订阅测试结果更新事件
        /// 2. 初始化所有数据集合
        /// 3. 初始化测试队列相关属性
        /// 4. 初始化UI显示属性
        /// 5. 初始化所有命令对象
        /// 6. 初始化批次数据
        /// </remarks>
        private void Initialize()
        {
            // 订阅测试结果更新事件
            _eventAggregator.GetEvent<TestResultsUpdatedEvent>().Subscribe(OnTestResultsUpdated);

            // 初始化集合
            AllChannels = new ObservableCollection<ChannelMapping>();
            CurrentChannels = new ObservableCollection<ChannelMapping>();
            //TestResults = new ObservableCollection<ChannelMapping>();
            Batches = new ObservableCollection<BatchInfo>();
            TestQueue = new ObservableCollection<ChannelMapping>();

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
            SendAIMaintenanceCommand = new DelegateCommand<ChannelMapping>(ExecuteSendAIMaintenance);
            ResetAIMaintenanceCommand = new DelegateCommand<ChannelMapping>(ExecuteResetAIMaintenance);
            ConfirmAIMaintenanceCommand = new DelegateCommand<ChannelMapping>(ExecuteConfirmAIMaintenance);


            // DI手动测试命令
            SendDITestCommand = new DelegateCommand<ChannelMapping>(ExecuteSendDITest);
            ResetDICommand = new DelegateCommand<ChannelMapping>(ExecuteResetDI);
            ConfirmDICommand = new DelegateCommand<ChannelMapping>(ExecuteConfirmDI);

            // DO手动测试命令
            StartDOMonitorCommand = new DelegateCommand<ChannelMapping>(ExecuteStartDOMonitor);
            ConfirmDOCommand = new DelegateCommand<ChannelMapping>(ExecuteConfirmDO);

            // AO手动测试命令
            StartAOMonitorCommand = new DelegateCommand<ChannelMapping>(ExecuteStartAOMonitor);
            ConfirmAOCommand = new DelegateCommand<ChannelMapping>(ExecuteConfirmAO);

            // 尝试从通道映射信息中提取批次信息
            InitializeBatchData();

            // 历史记录相关命令
            RestoreConfigCommand = new DelegateCommand(RestoreConfig);
            RestoreTestRecordsCommand = new DelegateCommand(RestoreTestRecords);
            DeleteTestBatchCommand = new DelegateCommand(DeleteTestBatch);
            CloseHistoryRecordsCommand = new DelegateCommand(CloseHistoryRecords);
        }

        #endregion

        #region 2、数据加载和处理
        /// <summary>
        /// 导入Excel点表配置文件
        /// </summary>
        /// <remarks>
        /// 该方法执行以下操作：
        /// 1. 调用点表数据服务导入Excel配置文件
        /// 2. 清空原始通道集合引用
        /// 3. 处理导入的数据，转换为通道映射对象
        /// 4. 显示导入结果信息
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
                AllChannels.Clear();
                //TestResults.Clear();

                // 添加AI通道
                foreach (var point in aiPoints)
                {
                    var channel = new ChannelMapping
                    {
                        TestId = point.SerialNumber,
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
                        SHHSetValueNumber = point.SHHSetValueNumber,
                        PlcCommunicationAddress = point.CommunicationAddress,
                        MaintenanceEnableSwitchPointCommAddress = point.MaintenanceEnableSwitchPointCommAddress,

                        DataType = point.DataType,
                        RangeLowerLimitValue = (float)point.RangeLowerLimitValue,
                        RangeUpperLimitValue = (float)point.RangeUpperLimitValue,
                        // AI点位不初始化百分比值，这些值将在测试过程中填充
                        TestResultStatus = 0, // 未测试
                        ResultText = "未测试"
                    };

                    #region 电表内参数空校验及处理
                    //低报、低低报
                    if (channel.SLLSetValue == "" && channel.SLSetValue == "")
                    {
                        channel.LowLowAlarmStatus = "通过";
                        channel.LowAlarmStatus = "通过";
                    }
                    //高报、高高报
                    if (channel.SHSetValue == "" && channel.SHHSetValue == "")
                    {
                        channel.HighAlarmStatus = "通过";
                        channel.HighHighAlarmStatus = "通过";
                    }
                    #endregion
                    AllChannels.Add(channel);
                }

                // 添加AO通道
                foreach (var point in aoPoints)
                {
                    var channel = new ChannelMapping
                    {
                        TestId = point.SerialNumber,
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
                        SHHSetValueNumber = point.SHHSetValueNumber,
                        PlcCommunicationAddress = point.CommunicationAddress,
                        MaintenanceEnableSwitchPointCommAddress = point.MaintenanceEnableSwitchPointCommAddress,

                        DataType = point.DataType,
                        RangeLowerLimitValue = (float)point.RangeLowerLimitValue,
                        RangeUpperLimitValue = (float)point.RangeUpperLimitValue,
                        // AO点位相关设置
                        TestResultStatus = 0, // 未测试
                        ResultText = "未测试",
                        // AO点位的低低报、低报、高报、高高报设置为N/A
                        LowLowAlarmStatus = "N/A",
                        LowAlarmStatus = "N/A",
                        HighAlarmStatus = "N/A",
                        HighHighAlarmStatus = "N/A"
                    };
                    AllChannels.Add(channel);
                }

                // 添加DI通道
                foreach (var point in diPoints)
                {
                    var channel = new ChannelMapping
                    {
                        TestId = point.SerialNumber,
                        ChannelTag = point.ChannelTag,
                        VariableName = point.VariableName,
                        ModuleType = point.ModuleType,
                        PlcCommunicationAddress = point.CommunicationAddress,

                        DataType = point.DataType,
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
                    AllChannels.Add(channel);
                }

                // 添加DO通道
                foreach (var point in doPoints)
                {
                    var channel = new ChannelMapping
                    {
                        TestId = point.SerialNumber,
                        ChannelTag = point.ChannelTag,
                        VariableName = point.VariableName,
                        ModuleType = point.ModuleType,
                        PlcCommunicationAddress = point.CommunicationAddress,

                        DataType = point.DataType,
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
                    AllChannels.Add(channel);
                }

                //当Excel中点位解析完成后并且已经初始化完ChannelMapping后调用自动分配程序分配点位
                var channelsMappingResult = await _channelMappingService.AllocateChannelsTestAsync(
                    AllChannels);

                //通道分配完成之后同步更新结果表位中的对应数据
                _channelMappingService.SyncChannelAllocation(
                    channelsMappingResult);

                //通知前端页面更新数据
                RaisePropertyChanged(nameof(AllChannels));

                // 更新当前显示的通道(已禁用此功能。第一次初始化放在点击选择批次且批次已自动分配之后)
                //SelectedBatch = Batches.FirstOrDefault();
                //UpdateCurrentChannels();

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
        /// 恢复历史配置
        /// </summary>
        /// <remarks>
        /// 该方法用于显示历史测试记录窗口，允许用户查看和恢复之前保存的测试配置。
        /// 它会调用LoadTestBatchesAsync方法加载所有历史测试批次，并打开历史记录窗口供用户选择。
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
                MessageBox.Show($"加载历史测试记录失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        /// <summary>
        /// 加载历史测试批次
        /// </summary>
        /// <remarks>
        /// 该方法从测试记录服务中获取所有历史测试批次信息，并将其加载到TestBatches集合中。
        /// 如果找到历史记录，则选择第一个批次作为默认选中项；如果没有找到历史记录，则显示提示信息。
        /// 该方法在加载过程中会显示加载状态和消息。
        /// </remarks>
        /// <returns>表示异步操作的任务</returns>
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
                    MessageBox.Show("没有找到历史测试记录", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载历史测试记录时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
                StatusMessage = string.Empty;
            }
        }
        #endregion

        #region 3、批次管理
        /// <summary>
        /// 初始化批次数据
        /// </summary>
        /// <remarks>
        /// 该方法初始化批次数据集合，并尝试从通道映射信息中提取批次信息。
        /// 它会创建一个新的批次集合，然后调用UpdateBatchInfoAsync方法来更新批次信息。
        /// </remarks>
        private async void InitializeBatchData()
        {
            // 初始化批次数据
            Batches = new ObservableCollection<BatchInfo>();

            // 尝试从通道映射信息中提取批次信息
            await UpdateBatchInfoAsync();
        }
        /// <summary>
        /// 从测试分配服务中的ChannelMapping提取相关信息形成批次相关信息
        /// </summary>
        /// <remarks>
        /// 该方法执行以下操作：
        /// 1. 检查AllChannels是否有效
        /// 2. 同步确保批次信息和PLC通道信息是最新的
        /// 3. 使用通道映射服务提取批次信息
        /// 4. 根据测试结果更新批次状态
        /// 5. 更新批次集合并尝试保持选中状态
        /// 6. 更新点位统计数据
        /// </remarks>
        /// <returns>表示异步操作的任务</returns>
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
        /// 点击批次选择窗口后执行的逻辑
        /// </summary>
        /// <remarks>
        /// 该方法执行以下操作：
        /// 1. 设置加载状态和消息
        /// 2. 如果有原始通道数据，使用它来提取批次信息
        /// 3. 如果没有原始通道数据，则使用当前通道集合更新批次信息
        /// 4. 显示批次选择窗口
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
                        Batches = new ObservableCollection<BatchInfo>(batchInfoList);

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
                MessageBox.Show($"获取批次信息失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"获取批次信息失败: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                StatusMessage = string.Empty;
            }
        }
        /// <summary>
        /// 批次选择窗口确定按钮逻辑
        /// </summary>
        /// <remarks>
        /// 该方法执行以下操作：
        /// 1. 验证是否选择了批次
        /// 2. 关闭批次选择窗口
        /// 3. 首次保存原始通道集合
        /// 4. 同步所有通道的批次信息到测试结果
        /// 5. 获取当前批次的所有通道并更新显示
        /// </remarks>
        private void ConfirmBatchSelection()
        {
            if (SelectedBatch == null)
            {
                MessageBox.Show("请先选择一个批次", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                IsLoading = true;
                StatusMessage = "正在更新批次信息...";

                // 关闭批次选择窗口
                IsBatchSelectionOpen = false;

                // 首次保存原始通道集合
                if (OriginalAllChannels == null)
                {
                    OriginalAllChannels = new ObservableCollection<ChannelMapping>(AllChannels);
                }

                // 同步所有通道的批次信息到测试结果
                foreach (var channel in OriginalAllChannels)
                {
                    if (!string.IsNullOrEmpty(channel.TestBatch))
                    {
                        var result = AllChannels.FirstOrDefault(r =>
                            r.VariableName == channel.VariableName &&
                            r.ChannelTag == channel.ChannelTag);

                        if (result != null)
                        {
                            result.TestBatch = channel.TestBatch;
                            result.TestPLCChannelTag = channel.TestPLCChannelTag;
                            result.TestPLCCommunicationAddress = channel.TestPLCCommunicationAddress;
                        }
                    }
                }

                // 获取当前批次的所有通道
                var batchChannels = OriginalAllChannels.Where(c => c.TestBatch == SelectedBatch.BatchName).ToList();

                if (batchChannels.Count == 0)
                {
                    MessageBox.Show($"未找到批次 {SelectedBatch.BatchName} 的通道信息", "警告", MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // 获取当前批次的所有通道ID
                var batchChannelVariableNames = batchChannels.Select(c => c.VariableName).ToHashSet();
                //同步更新显示区域为当前批次信息
                CurrentChannels =
                    new ObservableCollection<ChannelMapping>(AllChannels.Where(c =>
                        c.TestBatch.Equals(SelectedBatch.BatchName) && c.ModuleType.Equals("AI")));
                //批次选择完成之后同时将当前展示的数据调整为当前批次内的数据
                UpdateCurrentChannels();
                // 筛选当前显示的通道集合，只显示当前批次的通道
                //AllChannels = new ObservableCollection<ChannelMapping>(
                //    OriginalAllChannels.Where(c => batchChannelVariableNames.Contains(c.VariableName)));

                Message = $"已选择批次: {SelectedBatch.BatchName}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"选择批次时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
                StatusMessage = string.Empty;
            }
        }
        /// <summary>
        /// 取消批次选择
        /// </summary>
        /// <remarks>
        /// 该方法关闭批次选择窗口，不进行任何批次选择操作。
        /// </remarks>
        private void CancelBatchSelection()
        {
            IsBatchSelectionOpen = false;
        }
        /// <summary>
        /// 当选择批次发生变化时执行的逻辑
        /// </summary>
        /// <remarks>
        /// 该方法在用户选择不同批次时触发，执行以下操作：
        /// 1. 检查选中的批次是否有效
        /// 2. 根据批次状态设置接线确认按钮和通道硬点自动测试按钮的状态
        /// 3. 加载该批次的测试结果
        /// </remarks>
        private void OnBatchSelected()
        {
            if (SelectedBatch == null)
                return;

            // 如果批次状态为未开始或者进行中，则启用接线确认按钮
            IsWiringCompleteBtnEnabled = SelectedBatch.Status == "未开始" || SelectedBatch.Status == "进行中";
            // 重置通道硬点自动测试按钮状态
            IsStartTestButtonEnabled = false;

            // 加载该批次的测试结果
            LoadTestResults();
        }
        /// <summary>
        /// 刷新批次状态
        /// </summary>
        /// <remarks>
        /// 该方法根据当前测试进度和结果更新批次状态。执行以下操作：
        /// 1. 检查选中的批次是否有效
        /// 2. 获取选定批次的所有通道
        /// 3. 计算测试状态（总通道数、已测试通道数、通过通道数、失败通道数等）
        /// 4. 检查硬点测试状态
        /// 5. 根据测试状态更新批次状态文本
        /// 6. 更新批次的测试时间
        /// 7. 更新接线确认按钮状态
        /// 8. 通知导出测试结果按钮更新状态
        /// </remarks>
        private void RefreshBatchStatus()
        {
            try
            {
                if (SelectedBatch == null || string.IsNullOrEmpty(SelectedBatch.BatchName))
                    return;

                // 获取选定批次的所有通道
                var batchChannels = AllChannels.Where(c => c.TestBatch == SelectedBatch.BatchName).ToList();

                if (batchChannels.Count == 0)
                    return;

                // 计算测试状态
                int totalChannels = batchChannels.Count;
                int testedChannels = batchChannels.Count(c => c.TestResultStatus > 0);
                int passedChannels = batchChannels.Count(c => c.TestResultStatus == 1);
                int failedChannels = batchChannels.Count(c => c.TestResultStatus == 2);

                // 检查硬点测试状态
                int hardPointTestedCount = batchChannels.Count(c =>
                    !string.IsNullOrEmpty(c.HardPointTestResult) &&
                    c.HardPointTestResult != "未测试" &&
                    c.HardPointTestResult != "等待测试" &&
                    c.HardPointTestResult != "测试中");

                int hardPointPassedCount = batchChannels.Count(c =>
                    c.HardPointTestResult == "通过" ||
                    c.HardPointTestResult == "已通过");

                int hardPointFailedCount = hardPointTestedCount - hardPointPassedCount;

                // 接线确认后批次状态逻辑
                if (IsStartTestButtonEnabled)
                {
                    if (testedChannels == 0)
                    {
                        // 还未开始任何测试
                        SelectedBatch.Status = "测试中";
                    }
                    else if (hardPointTestedCount < totalChannels)
                    {
                        // 硬点测试尚未完成所有通道
                        SelectedBatch.Status = "硬点通道测试中";
                    }
                    else if (hardPointFailedCount > 0)
                    {
                        // 硬点测试有失败的通道
                        SelectedBatch.Status = "硬点通道测试失败请重新测试";
                    }
                    else if (hardPointPassedCount == totalChannels && testedChannels < totalChannels)
                    {
                        // 硬点测试全部通过，但手动测试尚未完成
                        SelectedBatch.Status = "硬点测试通过请开始手动测试";
                    }
                    else if (testedChannels == totalChannels)
                    {
                        // 所有测试都已完成
                        if (passedChannels == totalChannels)
                        {
                            SelectedBatch.Status = "测试完成(全部通过)";
                        }
                        else
                        {
                            SelectedBatch.Status = "测试完成(有失败)";
                        }
                    }
                    else
                    {
                        // 其他情况
                        SelectedBatch.Status = "测试中";
                    }
                }
                else
                {
                    // 常规状态更新逻辑（未进行接线确认）
                    if (testedChannels == 0)
                    {
                        SelectedBatch.Status = "未测试";
                    }
                    else if (testedChannels < totalChannels)
                    {
                        SelectedBatch.Status = "测试中";
                    }
                    else if (failedChannels > 0)
                    {
                        SelectedBatch.Status = "测试完成(有失败)";
                    }
                    else
                    {
                        SelectedBatch.Status = "测试完成(全部通过)";
                    }
                }

                // 更新批次的测试时间
                if (testedChannels > 0 && SelectedBatch.FirstTestTime == null)
                {
                    SelectedBatch.FirstTestTime = DateTime.Now;
                }

                SelectedBatch.LastTestTime = DateTime.Now;

                // 更新接线确认按钮状态
                IsWiringCompleteBtnEnabled = SelectedBatch.Status == "未测试" || SelectedBatch.Status == "测试中";

                // 检查是否所有点位都通过了硬点测试，如果是则启用开始测试按钮
                bool allPassed = hardPointTestedCount == hardPointPassedCount && hardPointTestedCount > 0;

                // 通知导出测试结果按钮更新状态
                ExportTestResultsCommand.RaiseCanExecuteChanged();

                // 通知UI更新
                RaisePropertyChanged(nameof(SelectedBatch));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"刷新批次状态时出错: {ex.Message}");
            }
        }
        #endregion

        #region 4、通道管理
        /// <summary>
        /// 根据选择的通道类型更新当前显示的通道集合
        /// </summary>
        /// <remarks>
        /// 该方法根据用户选择的通道类型(AI/AO/DI/DO)和当前选中的批次，
        /// 从AllChannels集合中筛选出符合条件的通道，并更新CurrentChannels集合用于UI显示。
        /// 如果没有选择通道类型或AllChannels为空，则不执行任何操作。
        /// </remarks>
        private void UpdateCurrentChannels()
        {
            if (string.IsNullOrEmpty(SelectedChannelType) || AllChannels == null)
                return;

            try
            {
                var filteredChannels = SelectedChannelType switch
                {
                    "AI通道" => AllChannels.Where(c => c.ModuleType?.ToLower() == "ai" && c.TestBatch.Equals(SelectedBatch.BatchName)),
                    "AO通道" => AllChannels.Where(c => c.ModuleType?.ToLower() == "ao" && c.TestBatch.Equals(SelectedBatch.BatchName)),
                    "DI通道" => AllChannels.Where(c => c.ModuleType?.ToLower() == "di" && c.TestBatch.Equals(SelectedBatch.BatchName)),
                    "DO通道" => AllChannels.Where(c => c.ModuleType?.ToLower() == "do" && c.TestBatch.Equals(SelectedBatch.BatchName)),
                    _ => Enumerable.Empty<ChannelMapping>()
                };

                CurrentChannels = new ObservableCollection<ChannelMapping>(filteredChannels);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新当前通道失败: {ex.Message}");
            }
        }
        /// <summary>
        /// 执行分配通道操作
        /// </summary>
        /// <remarks>
        /// 该方法调用通道映射服务对所有通道进行自动分配。执行以下操作：
        /// 1. 检查AllChannels是否有效
        /// 2. 调用通道映射服务进行通道分配
        /// 3. 更新通道映射结果并同步到服务
        /// 4. 更新当前显示的通道集合
        /// 5. 保存原始通道集合以便后续使用
        /// </remarks>
        private async void ExecuteAllocateChannels()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "正在分配通道...";

                if (AllChannels == null || !AllChannels.Any())
                {
                    Message = "没有可用的通道需要分配";
                    return;
                }

                // 调用通道映射服务进行通道分配
                var result = await _channelMappingService.AllocateChannelsTestAsync(AllChannels);

                if (result != null)
                {
                    // 更新通道映射结果
                    AllChannels = new ObservableCollection<ChannelMapping>(result);

                    // 同步更新通道分配结果
                    _channelMappingService.SyncChannelAllocation(result);

                    // 更新当前显示的通道集合
                    UpdateCurrentChannels();

                    // 通知UI更新
                    RaisePropertyChanged(nameof(AllChannels));

                    // 保存原始通道集合以便后续使用
                    OriginalAllChannels = new ObservableCollection<ChannelMapping>(AllChannels);

                    Message = "通道分配完成";
                }
                else
                {
                    Message = "通道分配失败";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"分配通道失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
                StatusMessage = string.Empty;
            }
        }
        /// <summary>
        /// 清除所有通道分配信息
        /// </summary>
        /// <remarks>
        /// 该方法清除所有通道的分配信息，包括批次关联和测试PLC通道标识。执行以下操作：
        /// 1. 清空原始通道集合引用
        /// 2. 调用通道映射服务清除所有通道分配信息
        /// 3. 更新当前显示的通道集合
        /// 4. 同步更新测试结果中的通道信息
        /// </remarks>
        private async void ClearChannelAllocationsAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "正在清除通道分配信息...";

                // 清空原始通道集合引用
                OriginalAllChannels = null;

                // 清除所有通道分配信息
                AllChannels = new ObservableCollection<ChannelMapping>(
                    await _channelMappingService.ClearAllChannelAllocationsAsync(AllChannels));

                // 更新当前显示的通道集合
                UpdateCurrentChannels();

                // 同步更新测试结果中的通道信息
                foreach (var result in AllChannels)
                {
                    result.TestBatch = string.Empty;
                    result.TestPLCChannelTag = string.Empty;
                    result.TestPLCCommunicationAddress = string.Empty;
                }

                // 通知UI更新
                RaisePropertyChanged(nameof(AllChannels));

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
        /// 加载批次测试结果
        /// </summary>
        /// <remarks>
        /// 该方法加载当前选中批次的所有通道测试结果。执行以下操作：
        /// 1. 检查是否选择了批次
        /// 2. 从通道映射服务中获取该批次的所有通道
        /// 3. 更新现有的通道数据，只添加新的结果
        /// 4. 刷新测试队列
        /// 5. 更新点位统计
        /// </remarks>
        private async void LoadTestResults()
        {
            IsLoading = true;
            StatusMessage = "正在加载测试结果...";

            try
            {
                // 如果选择了批次，加载该批次的所有通道测试结果
                if (SelectedBatch == null || string.IsNullOrEmpty(SelectedBatch.BatchName))
                {
                    return;
                }

                // 从通道映射服务中获取该批次的所有通道
                var results = await _channelMappingService.GetChannelMappingsByBatchNameAsync(SelectedBatch.BatchName);

                if (results != null && results.Any())
                {
                    // 更新现有的通道数据，只添加新的结果
                    var existingIds = AllChannels.Select(r => r.VariableName).ToHashSet();
                    var newResults = results.Where(r => !existingIds.Contains(r.VariableName)).ToList();

                    // 添加新结果
                    foreach (var newResult in newResults)
                    {
                        AllChannels.Add(newResult);
                    }

                    // 更新现有结果的状态
                    foreach (var result in results)
                    {
                        var existingResult = AllChannels.FirstOrDefault(r => r.VariableName == result.VariableName);
                        if (existingResult != null)
                        {
                            // 更新必要的属性，但保留测试结果
                            existingResult.TestBatch = result.TestBatch;
                            existingResult.ChannelTag = result.ChannelTag;
                            existingResult.TestPLCChannelTag = result.TestPLCChannelTag;
                        }
                    }
                }
                else
                {
                    // 如果找不到基于BatchName的结果，尝试使用BatchId
                    results = AllChannels?
                        .Where(c => c.TestBatch == SelectedBatch.BatchId)
                        .ToList();

                    if (results == null || !results.Any())
                    {
                        // 如果仍然没有找到，检查是否需要创建空集合
                        if (AllChannels == null || AllChannels.Count == 0)
                        {
                            // 创建空集合
                            if (AllChannels == null)
                            {
                                AllChannels = new ObservableCollection<ChannelMapping>();
                            }
                        }
                    }
                }

                // 刷新测试队列
                RefreshTestQueue();

                // 更新点位统计
                UpdatePointStatistics();
            }
            catch (Exception ex)
            {
                StatusMessage = $"加载测试结果时出错: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
        /// <summary>
        /// 应用结果过滤器
        /// </summary>
        /// <remarks>
        /// 该方法根据选择的过滤条件过滤测试结果。支持的过滤条件包括：
        /// - 全部：显示所有结果
        /// - 通过：只显示通过的测试结果
        /// - 失败：只显示失败的测试结果
        /// - 未测试：只显示未测试的结果
        /// 应用过滤后会更新统计数据。
        /// </remarks>
        private void ApplyResultFilter()
        {
            if (string.IsNullOrEmpty(SelectedResultFilter) || AllChannels == null)
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
        /// <summary>
        /// 计算并更新点位统计数据
        /// </summary>
        /// <remarks>
        /// 该方法计算并更新各类点位的统计数据，包括：
        /// - 全部点位数量
        /// - 已测试点位数量
        /// - 待测试点位数量
        /// - 成功点位数量
        /// - 失败点位数量
        /// 这些统计数据用于在UI上显示当前测试进度和状态。
        /// </remarks>
        private void UpdatePointStatistics()
        {
            if (AllChannels == null) return;

            // 如果有当前选中的批次，只计算该批次的点位
            //var channelsToCount = SelectedBatch != null && !string.IsNullOrEmpty(SelectedBatch.BatchName)
            //    ? AllChannels.Where(c => c.TestBatch == SelectedBatch.BatchName).ToList()
            //    : AllChannels.ToList();
            //TotalPointCount = $"全部点位数量:{channelsToCount.Count}";
            //TestedPointCount = $"已测试点位数量:{channelsToCount.Count(r => r.TestResultStatus > 0)}";
            //WaitingPointCount = $"待测试点位数量:{channelsToCount.Count(r => r.TestResultStatus == 0)}";
            //SuccessPointCount = $"成功点位数量:{channelsToCount.Count(r => r.TestResultStatus == 1)}";
            //FailurePointCount = $"失败点位数量:{channelsToCount.Count(r => r.TestResultStatus == 2)}";

            //点位统计只计算总得通道中的数据
            TotalPointCount = $"全部点位数量:{AllChannels.Count}";
            TestedPointCount = $"已测试点位数量:{AllChannels.Count(r => r.TestResultStatus > 0)}";
            WaitingPointCount = $"待测试点位数量:{AllChannels.Count(r => r.TestResultStatus == 0)}";
            SuccessPointCount = $"成功点位数量:{AllChannels.Count(r => r.TestResultStatus == 1)}";
            FailurePointCount = $"失败点位数量:{AllChannels.Count(r => r.TestResultStatus == 2)}";
        }
        #endregion

        #region 5、测试执行和控制
        /// <summary>
        /// 完成接线确认
        /// </summary>
        /// <remarks>
        /// 该方法在用户确认接线完成时调用，执行以下操作：
        /// 1. 检查是否选择了测试批次
        /// 2. 将ViewModel中的BatchInfo转换为Model中的BatchInfo
        /// 3. 调用TestTaskManager的接线确认方法
        /// 4. 如果确认成功，禁用接线确认按钮，启用通道硬点自动测试按钮
        /// 5. 刷新批次状态
        /// </remarks>
        private async void FinishWiring()
        {
            if (SelectedBatch == null)
            {
                MessageBox.Show("请先选择一个测试批次", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            IsLoading = true;
            StatusMessage = "正在确认接线完成...";

            try
            {
                // 将ViewModel中的BatchInfo转换为Model中的BatchInfo
                var batchInfo = new Models.BatchInfo
                {
                    BatchId = SelectedBatch.BatchId,
                    BatchName = SelectedBatch.BatchName,
                    CreationDate = SelectedBatch.CreationDate,
                    ItemCount = SelectedBatch.ItemCount,
                    Status = SelectedBatch.Status
                };

                // 调用TestTaskManager的接线确认方法，但不显示等待对话框，不开始测试
                var result = await _testTaskManager.ConfirmWiringCompleteAsync(batchInfo, false, AllChannels);
                if (result)
                {
                    // 确认接线成功后，禁用接线确认按钮，启用通道硬点自动测试按钮
                    IsWiringCompleteBtnEnabled = false;
                    IsStartTestButtonEnabled = true;
                    Message = "接线确认完成，可以开始测试";

                    // 刷新批次状态
                    RefreshBatchStatus();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"接线确认失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
                StatusMessage = string.Empty;
            }
        }
        /// <summary>
        /// 执行完成接线确认操作
        /// </summary>
        private void ExecuteConfirmWiringComplete()
        {
            try
            {
                // 接线确认逻辑实现
                MessageBox.Show("接线确认完成", "操作成功", MessageBoxButton.OK, MessageBoxImage.Information);
                // 设置批次状态为测试中
                if (SelectedBatch != null)
                {
                    SelectedBatch.Status = "测试中";
                    IsStartTestButtonEnabled = true;
                    IsWiringCompleteBtnEnabled = false;

                    // 遍历当前批次的通道，将硬点测试结果状态初始化为"等待测试"
                    if (!string.IsNullOrEmpty(SelectedBatch.BatchName))
                    {
                        var batchChannels = AllChannels.Where(c => c.TestBatch == SelectedBatch.BatchName).ToList();
                        foreach (var channel in batchChannels)
                        {
                            if (string.IsNullOrEmpty(channel.HardPointTestResult) ||
                                channel.HardPointTestResult == "未测试")
                            {
                                channel.HardPointTestResult = "等待测试";
                            }

                            // 确保测试结果文本不为空
                            if (string.IsNullOrEmpty(channel.ResultText) ||
                                channel.ResultText == "未测试")
                            {
                                channel.ResultText = "等待测试";
                            }
                        }

                        // 通知UI更新
                        RaisePropertyChanged(nameof(AllChannels));

                        // 刷新批次状态
                        RefreshBatchStatus();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"接线确认操作失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        /// <summary>
        /// 检查是否可以执行接线确认操作
        /// </summary>
        private bool CanExecuteConfirmWiringComplete()
        {
            // 检查是否选择了批次且状态允许接线确认
            return SelectedBatch != null &&
                   (SelectedBatch.Status == "未开始" || SelectedBatch.Status == "进行中");
        }
        /// <summary>
        /// 开始测试
        /// </summary>
        /// <remarks>
        /// 该方法启动通道硬点自动测试过程，执行以下操作：
        /// 1. 检查是否可以开始测试
        /// 2. 更新当前批次所有通道的硬点测试状态为"测试中"
        /// 3. 显示等待测试的画面
        /// 4. 设置批次状态为硬点通道测试中
        /// 5. 开始所有测试任务
        /// 6. 更新批次状态和结果中的通道硬点测试结果
        /// </remarks>
        private async void StartTest()
        {
            // 检查是否可以开始测试
            if (SelectedBatch == null)
            {
                MessageBox.Show("请先选择一个测试批次", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 开始测试的实现
            Message = "开始测试";
            IsLoading = true;
            StatusMessage = "正在准备测试...";

            try
            {
                // 先更新当前批次所有通道的硬点测试状态为"测试中"
                if (!string.IsNullOrEmpty(SelectedBatch.BatchName) && AllChannels != null)
                {
                    var batchChannels = AllChannels.Where(c => c.TestBatch == SelectedBatch.BatchName).ToList();
                    foreach (var channel in batchChannels)
                    {
                        // 设置开始测试时间为当前时间，并将最终测试时间设为null
                        channel.StartTime = DateTime.Now;
                        channel.TestTime = DateTime.Now;
                        channel.FinalTestTime = null;

                        // 只更新未测试或等待测试的通道
                        if (string.IsNullOrEmpty(channel.HardPointTestResult) ||
                            channel.HardPointTestResult == "未测试" ||
                            channel.HardPointTestResult == "等待测试")
                        {
                            channel.HardPointTestResult = "测试中";
                        }

                        // 更新结果文本
                        if (string.IsNullOrEmpty(channel.ResultText) ||
                            channel.ResultText == "未测试" ||
                            channel.ResultText == "等待测试")
                        {
                            channel.ResultText = "硬点通道测试中";
                        }
                        else if (!channel.ResultText.Contains("硬点通道测试"))
                        {
                            // 附加硬点通道测试状态
                            channel.ResultText += ", 硬点通道测试中";
                        }
                        else
                        {
                            // 更新已有的硬点测试状态
                            if (channel.ResultText.Contains("硬点通道测试失败"))
                            {
                                channel.ResultText = channel.ResultText.Replace("硬点通道测试失败", "硬点通道测试中");
                            }
                            else if (channel.ResultText.Contains("硬点通道测试成功"))
                            {
                                channel.ResultText = channel.ResultText.Replace("硬点通道测试成功", "硬点通道测试中");
                            }
                        }
                    }
                    // 显示等待测试的画面
                    await _testTaskManager.ShowTestProgressDialogAsync(false, null);
                    // 设置批次状态为硬点通道测试中
                    SelectedBatch.Status = "硬点通道测试中";

                    // 通知UI更新
                    RaisePropertyChanged(nameof(AllChannels));
                    RaisePropertyChanged(nameof(SelectedBatch));
                }

                // 如果未确认接线，则不应继续
                //if (!_testTaskManager.IsWiringCompleted)
                //{
                //    MessageBox.Show("请先确认接线已完成", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                //    return;
                //}

                // 在开始测试前清空所有现有任务，确保批次间互不干扰
                //await _testTaskManager.ClearAllTasksAsync();

                // 开始所有测试任务
                var result = await _testTaskManager.StartAllTasksAsync();
                if (!result)
                {
                    MessageBox.Show("开始测试失败，请检查设备连接", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 收集当前批次名称
                HashSet<string> affectedBatchNames = new HashSet<string>();
                if (AllChannels != null)
                {
                    foreach (var item in AllChannels)
                    {
                        if (!string.IsNullOrEmpty(item.TestBatch))
                        {
                            affectedBatchNames.Add(item.TestBatch);
                        }
                    }
                }

                Message = $"硬点通道测试已启动，批次: {string.Join(", ", affectedBatchNames)}";

                // 更新批次状态
                RefreshBatchStatus();
                //更新结果中的通道硬点测试结果
                var forUpdateResult = AllChannels.Where(c => c.TestBatch == SelectedBatch.BatchName).ToList();
                foreach (var batchChannels in forUpdateResult)
                {
                    if (batchChannels.HardPointTestResult == "通过")
                    {
                        batchChannels.ResultText = "硬点通道测试通过";
                    }
                    else
                    {
                        batchChannels.ResultText = batchChannels.HardPointTestResult;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"开始测试失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
                StatusMessage = string.Empty;
            }
        }
        /// <summary>
        /// 重新测试指定通道
        /// </summary>
        /// <param name="result">需要重新测试的通道</param>
        /// <remarks>
        /// 该方法对指定的通道进行重新测试，执行以下操作：
        /// 1. 调用测试任务管理器重新测试该通道
        /// 2. 如果重测成功，更新批次信息、UI和点位统计数据
        /// 3. 更新测试结果状态
        /// </remarks>
        private async void Retest(ChannelMapping result)
        {
            if (result == null) return;

            try
            {
                // 设置加载状态
                IsLoading = true;
                StatusMessage = $"正在复测通道 {result.VariableName}...";

                // 调用测试任务管理器复测该通道
                bool retestSuccess = await _testTaskManager.RetestChannelAsync(result);

                if (retestSuccess)
                {
                    // 更新批次信息
                    await UpdateBatchInfoAsync();

                    // 通知UI更新
                    RaisePropertyChanged(nameof(AllChannels));
                    UpdateCurrentChannels();

                    // 更新点位统计数据
                    UpdatePointStatistics();

                    // 更新测试结果状态
                    UpdateTestResultStatus(result);

                    // 提示成功
                    StatusMessage = $"通道 {result.VariableName} 复测完成";
                }
                else
                {
                    StatusMessage = $"通道 {result.VariableName} 复测失败";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"复测失败: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"复测失败: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
        /// <summary>
        /// 刷新测试队列
        /// </summary>
        /// <remarks>
        /// 该方法更新当前测试队列的状态和内容，执行以下操作：
        /// 1. 检查是否选择了批次
        /// 2. 获取选定批次的所有未测试通道
        /// 3. 更新测试队列集合
        /// 4. 更新队列状态和位置信息
        /// </remarks>
        private void RefreshTestQueue()
        {
            try
            {
                if (SelectedBatch == null || string.IsNullOrEmpty(SelectedBatch.BatchName))
                {
                    TestQueue.Clear();
                    TestQueueStatus = "队列为空";
                    TestQueuePosition = 0;
                    return;
                }

                // 获取选定批次的所有通道
                var batchChannels = AllChannels
                    .Where(c => c.TestBatch == SelectedBatch.BatchName && c.TestResultStatus == 0)
                    .ToList();

                // 更新测试队列
                TestQueue = new ObservableCollection<ChannelMapping>(batchChannels);

                // 更新队列状态
                if (TestQueue.Count == 0)
                {
                    TestQueueStatus = "队列为空";
                    TestQueuePosition = 0;
                }
                else
                {
                    TestQueueStatus = $"队列中有 {TestQueue.Count} 个通道待测试";
                    TestQueuePosition = 1; // 从第一个开始
                }

                // 通知UI更新
                RaisePropertyChanged(nameof(TestQueue));
                RaisePropertyChanged(nameof(TestQueueStatus));
                RaisePropertyChanged(nameof(TestQueuePosition));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"刷新测试队列时出错: {ex.Message}");
            }
        }
        /// <summary>
        /// 处理测试结果更新事件
        /// </summary>
        /// <remarks>
        /// 该方法在测试结果更新时触发，执行以下操作：
        /// 1. 确保在UI线程上执行更新操作
        /// 2. 检查当前选中的批次，同步更新该批次的通道数据
        /// 3. 更新通道的硬点测试结果和结果文本
        /// 4. 通知UI更新
        /// 5. 更新点位统计和批次状态
        /// </remarks>
        private void OnTestResultsUpdated()
        {
            // 确保在UI线程上执行更新操作
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    Console.WriteLine($"收到测试结果更新事件，时间：{DateTime.Now}");

                    // 如果当前选中了批次，同步更新这个批次的通道数据
                    if (SelectedBatch != null && !string.IsNullOrEmpty(SelectedBatch.BatchName))
                    {
                        // 获取当前批次名称
                        string currentBatchName = SelectedBatch.BatchName;

                        // 找出当前批次的所有通道
                        var batchChannels = AllChannels.Where(c => c.TestBatch == currentBatchName).ToList();

                        bool hasChanges = false;
                        foreach (var channel in batchChannels)
                        {
                            // 确保HardPointTestResult有默认值
                            if (string.IsNullOrEmpty(channel.HardPointTestResult))
                            {
                                channel.HardPointTestResult = "等待测试";
                                hasChanges = true;
                            }

                            // 确保ResultText有默认值
                            if (string.IsNullOrEmpty(channel.ResultText))
                            {
                                channel.ResultText = "等待测试";
                                hasChanges = true;
                            }
                            //硬点通道测试失败显示为红色
                            if (channel.HardPointTestResult == "失败")
                            {
                                channel.TestResultStatus = 2;
                            }

                            // 检查该通道是否已经有测试结果状态
                            if (channel.HardPointTestResult == "通过" || channel.HardPointTestResult == "已通过")
                            {
                                // 设置测试结果状态为通过(1)，但不覆盖已有的测试结果状态
                                // 如果是手动测试失败，则保留失败状态
                                if (channel.TestResultStatus != 2)
                                {
                                    channel.TestResultStatus = 1;
                                    hasChanges = true;
                                }

                                // 更新结果文本，使用附加方式而不是替换
                                if (channel.ResultText == "未测试" ||
                                    channel.ResultText == "等待测试" ||
                                    channel.ResultText == "测试中")
                                {
                                    channel.ResultText = $"硬点通道测试成功";
                                    hasChanges = true;
                                }
                                else if (!channel.ResultText.Contains("硬点通道测试成功"))
                                {
                                    // 检查结果文本是否已包含硬点测试信息
                                    // 如果已有其他测试结果，则附加硬点测试结果
                                    if (channel.ResultText.Contains("硬点通道测试"))
                                    {
                                        // 已包含硬点测试信息，更新为成功
                                        channel.ResultText = channel.ResultText.Replace("硬点通道测试中", "硬点通道测试成功");
                                        channel.ResultText = channel.ResultText.Replace("硬点通道测试失败", "硬点通道测试成功");
                                    }
                                    else
                                    {
                                        // 添加硬点测试成功信息
                                        channel.ResultText += ", 硬点通道测试成功";
                                    }

                                    hasChanges = true;
                                }
                            }
                            else if (!string.IsNullOrEmpty(channel.HardPointTestResult) &&
                                     channel.HardPointTestResult != "未测试" &&
                                     channel.HardPointTestResult != "等待测试" &&
                                     channel.HardPointTestResult != "测试中")
                            {
                                // 设置测试结果状态为失败(2)
                                channel.TestResultStatus = 2;
                                hasChanges = true;

                                // 更新结果文本
                                if (channel.ResultText == "未测试" ||
                                    channel.ResultText == "等待测试" ||
                                    channel.ResultText == "测试中")
                                {
                                    channel.ResultText = $"硬点通道测试失败: {channel.HardPointTestResult}";
                                    hasChanges = true;
                                }
                                else if (!channel.ResultText.Contains("硬点通道测试失败"))
                                {
                                    // 检查结果文本是否已包含硬点测试信息
                                    if (channel.ResultText.Contains("硬点通道测试"))
                                    {
                                        // 已包含硬点测试信息，更新为失败
                                        channel.ResultText = channel.ResultText.Replace("硬点通道测试中",
                                            $"硬点通道测试失败: {channel.HardPointTestResult}");
                                        channel.ResultText = channel.ResultText.Replace("硬点通道测试成功",
                                            $"硬点通道测试失败: {channel.HardPointTestResult}");
                                    }
                                    else
                                    {
                                        // 添加硬点测试失败信息
                                        channel.ResultText += $", 硬点通道测试失败: {channel.HardPointTestResult}";
                                    }

                                    hasChanges = true;
                                }
                            }
                        }

                        // 通知UI更新
                        RaisePropertyChanged(nameof(AllChannels));

                        // 更新点位统计
                        UpdatePointStatistics();

                        // 更新批次状态
                        RefreshBatchStatus();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"测试结果更新出错: {ex.Message}");
                }
            });
        }
        /// <summary>
        /// 更新测试结果的总体状态
        /// </summary>
        /// <param name="channel">需要更新状态的通道</param>
        /// <remarks>
        /// 该方法更新指定通道的测试结果状态，执行以下操作：
        /// 1. 检查通道类型并根据不同类型进行相应处理
        /// 2. 对于AI通道，检查所有子测试（高报、低报、维护功能等）是否都通过
        /// 3. 对于DI/DO/AO通道，只检查ShowValueStatus是否为通过
        /// 4. 如果所有测试都通过，设置总体测试状态为通过
        /// 5. 刷新批次状态并检查是否所有测试已完成
        /// 6. 保存通道测试记录到数据库
        /// </remarks>
        private async void UpdateTestResultStatus(ChannelMapping channel)
        {
            if (channel == null) return;

            // 检查AI通道的所有子测试是否都通过
            bool allPassed = true;

            // 检查必要的子测试状态
            if (channel.ModuleType != null && channel.ModuleType.Contains("AI"))
            {
                // 如果有任何一个子测试未通过，则标记为未通过
                if (channel.HighAlarmStatus != "通过" ||
                    channel.LowAlarmStatus != "通过" ||
                    channel.MaintenanceFunction != "通过" ||
                    channel.ShowValueStatus != "通过")
                {
                    allPassed = false;
                }

                // 如果所有子测试都通过，设置总体测试状态为通过
                if (allPassed)
                {
                    channel.HardPointTestResult = "通过";
                    channel.TestResultStatus = 1; // 通过
                    channel.Status = "通过"; // 更新总体状态

                    // 同时更新手动测试总状态
                    ManualAITestStatus = "通过";

                    // 刷新批次状态
                    RefreshBatchStatus();

                    // 如果所有手动测试已完成，更新批次状态为已完成
                    CheckAndCompleteAllTests();

                    // 通知导出测试结果按钮更新状态
                    ExportTestResultsCommand.RaiseCanExecuteChanged();

                    // 保存通道测试记录到数据库
                    await SaveSingleChannelTestRecordAsync(channel);
                }

                // 更新UI
                RaisePropertyChanged(nameof(CurrentTestResult));
                RaisePropertyChanged(nameof(AllChannels));
            }
            else if (channel.ModuleType != null &&
                     (channel.ModuleType.Contains("DI") ||
                      channel.ModuleType.Contains("DO") ||
                      channel.ModuleType.Contains("AO")))
            {
                // 对于DI/DO/AO通道，只需检查ShowValueStatus
                if (channel.ShowValueStatus == "通过")
                {
                    channel.HardPointTestResult = "通过";
                    channel.TestResultStatus = 1; // 通过
                    channel.Status = "通过"; // 更新总体状态

                    // 刷新批次状态
                    RefreshBatchStatus();

                    // 如果所有手动测试已完成，更新批次状态为已完成
                    CheckAndCompleteAllTests();

                    // 保存通道测试记录到数据库
                    await SaveSingleChannelTestRecordAsync(channel);
                }

                // 更新UI
                RaisePropertyChanged(nameof(CurrentTestResult));
                RaisePropertyChanged(nameof(AllChannels));
            }
        }
        /// <summary>
        /// 检查通道的所有子测试是否完成，如果所有子测试都通过，设置总体测试结果为通过
        /// </summary>
        /// <param name="channel">要检查的通道</param>
        /// <remarks>
        /// 该方法检查指定通道的所有子测试是否完成，执行以下操作：
        /// 1. 根据通道类型检查不同的子测试状态
        /// 2. 对于AI通道，检查高报、高高报、低报、低低报、维护功能和显示值核对状态
        /// 3. 对于DI/DO/AO通道，只检查显示值核对是否通过
        /// 4. 如果所有子测试都通过，设置总体测试状态为通过
        /// 5. 设置最终测试时间为当前时间
        /// 6. 刷新批次状态并检查是否所有测试已完成
        /// 7. 更新UI和点位统计数据
        /// </remarks>
        private void CheckAllSubTestsCompleted(ChannelMapping channel)
        {
            if (channel == null) return;

            // 检查AI通道的所有子测试是否都通过
            bool allPassed = true;

            // 检查必要的子测试状态
            if (channel.ModuleType != null && channel.ModuleType.Contains("AI"))
            {
                // 如果有任何一个子测试未通过，则标记为未通过
                if (channel.HighAlarmStatus != "通过" ||
                    channel.HighHighAlarmStatus != "通过" ||
                    channel.LowAlarmStatus != "通过" ||
                    channel.LowLowAlarmStatus != "通过" ||
                    channel.MaintenanceFunction != "通过" ||
                    channel.ShowValueStatus != "通过")
                {
                    allPassed = false;
                }

                // 如果所有子测试都通过，设置总体测试状态为通过
                if (allPassed)
                {
                    channel.HardPointTestResult = "通过";
                    channel.TestResultStatus = 1; // 通过
                    channel.Status = "通过"; // 更新总体状态
                    channel.ResultText = "测试已通过"; // 更新结果文本

                    // 设置最终测试时间为当前时间
                    channel.FinalTestTime = DateTime.Now;

                    // 刷新批次状态
                    RefreshBatchStatus();

                    // 如果所有手动测试已完成，更新批次状态为已完成
                    CheckAndCompleteAllTests();
                }

                // 更新UI
                RaisePropertyChanged(nameof(CurrentTestResult));
                RaisePropertyChanged(nameof(AllChannels));
            }
            else if (channel.ModuleType != null &&
                    (channel.ModuleType.Contains("DI") ||
                     channel.ModuleType.Contains("DO") ||
                     channel.ModuleType.Contains("AO")))
            {
                // 对于DI/DO/AO通道，只需要检查显示值核对是否通过
                if (channel.ShowValueStatus == "通过")
                {
                    // 如果显示值核对通过，设置总体测试状态为通过
                    channel.HardPointTestResult = "通过";
                    channel.TestResultStatus = 1; // 通过
                    channel.Status = "通过"; // 更新总体状态
                    channel.ResultText = "测试已通过"; // 更新结果文本

                    // 设置最终测试时间为当前时间
                    channel.FinalTestTime = DateTime.Now;

                    // 刷新批次状态
                    RefreshBatchStatus();

                    // 如果所有手动测试已完成，更新批次状态为已完成
                    CheckAndCompleteAllTests();
                }

                // 更新UI
                RaisePropertyChanged(nameof(CurrentTestResult));
                RaisePropertyChanged(nameof(AllChannels));
            }

            // 更新点位统计数据
            UpdatePointStatistics();
        }
        /// <summary>
        /// 检查并完成所有测试
        /// </summary>
        /// <remarks>
        /// 该方法检查是否所有通道都已完成测试，执行以下操作：
        /// 1. 检查各类通道类型（AI/DI/DO/AO）的测试状态
        /// 2. 对于AI通道，检查高报、低报、维护功能和显示值核对状态
        /// 3. 对于DI/DO/AO通道，检查显示值核对状态
        /// 4. 如果所有测试都已完成，调用测试任务管理器完成所有测试
        /// 5. 通知导出测试结果按钮更新状态
        /// </remarks>
        private void CheckAndCompleteAllTests()
        {
            // 检查是否所有通道都已完成测试
            bool allCompleted = true;

            // 检查必要的通道类型
            var aiChannels = AllChannels?.Where(c => c.ModuleType?.ToLower() == "ai");
            var diChannels = AllChannels?.Where(c => c.ModuleType?.ToLower() == "di");
            var doChannels = AllChannels?.Where(c => c.ModuleType?.ToLower() == "do");
            var aoChannels = AllChannels?.Where(c => c.ModuleType?.ToLower() == "ao");

            // 检查AI通道
            if (aiChannels != null && aiChannels.Any())
            {
                foreach (var channel in aiChannels)
                {
                    if (channel.HighAlarmStatus != "通过" ||
                        channel.LowAlarmStatus != "通过" ||
                        channel.MaintenanceFunction != "通过" ||
                        channel.ShowValueStatus != "通过")
                    {
                        allCompleted = false;
                        break;
                    }
                }
            }

            // 检查DI通道
            if (allCompleted && diChannels != null && diChannels.Any())
            {
                foreach (var channel in diChannels)
                {
                    if (channel.ShowValueStatus != "通过")
                    {
                        allCompleted = false;
                        break;
                    }
                }
            }

            // 检查DO通道
            if (allCompleted && doChannels != null && doChannels.Any())
            {
                foreach (var channel in doChannels)
                {
                    if (channel.ShowValueStatus != "通过")
                    {
                        allCompleted = false;
                        break;
                    }
                }
            }

            // 检查AO通道
            if (allCompleted && aoChannels != null && aoChannels.Any())
            {
                foreach (var channel in aoChannels)
                {
                    if (channel.ShowValueStatus != "通过")
                    {
                        allCompleted = false;
                        break;
                    }
                }
            }

            // 如果所有测试都已完成，调用完成所有测试的方法
            if (allCompleted)
            {
                // 调用测试任务管理器完成所有测试
                _testTaskManager.CompleteAllTestsAsync();

                // 通知导出测试结果按钮更新状态
                ExportTestResultsCommand.RaiseCanExecuteChanged();
            }
        }
        /// <summary>
        /// 保存单个通道的测试记录
        /// </summary>
        /// <param name="channel">通道记录</param>
        /// <returns>保存操作是否成功</returns>
        /// <remarks>
        /// 该方法将指定通道的测试记录保存到数据库，执行以下操作：
        /// 1. 检查通道是否有效
        /// 2. 确保通道有测试标识，如果没有则生成一个
        /// 3. 记录最终测试时间
        /// 4. 调用测试记录服务保存记录
        /// 5. 返回保存操作的结果
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
        private void OpenAIManualTest(ChannelMapping channel)
        {
            try
            {
                if (channel != null && channel.ModuleType?.ToLower() == "ai")
                {
                    // 更新当前选中的通道
                    CurrentChannel = channel;
                    // 将当前测试结果设置为当前通道
                    CurrentTestResult = channel;

                    // 初始化各项测试状态
                    if (channel.ShowValueStatus != "通过")
                        channel.ShowValueStatus = "未测试";
                    if (channel.HighAlarmStatus != "通过")
                        channel.HighAlarmStatus = "未测试";
                    if (channel.HighHighAlarmStatus != "通过")
                        channel.HighHighAlarmStatus = "未测试";
                    if (channel.LowAlarmStatus != "通过")
                        channel.LowAlarmStatus = "未测试";
                    if (channel.LowLowAlarmStatus != "通过")
                        channel.LowLowAlarmStatus = "未测试";
                    if (channel.MaintenanceFunction != "通过")
                        channel.MaintenanceFunction = "未测试";

                    // 更新ResultText为手动测试中
                    if (channel.ShowValueStatus == "未测试"
                       || channel.HighAlarmStatus == "未测试"
                       || channel.HighHighAlarmStatus == "未测试"
                       || channel.LowAlarmStatus == "未测试"
                       || channel.LowLowAlarmStatus == "未测试"
                       || channel.MaintenanceFunction == "未测试")
                    {
                        channel.ResultText = "手动测试中";
                    }
                    //打开窗口时生成一个在低报和高报内的随机数
                    AISetValue = ((float)(new Random().NextDouble() * (channel.HighLimit - channel.LowLimit) + channel.LowLimit)).ToString();
                    // 设置AI手动测试窗口打开状态为true
                    IsAIManualTestOpen = true;

                    // 立即更新UI和点位统计
                    RaisePropertyChanged(nameof(AllChannels));
                    UpdatePointStatistics();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开AI手动测试窗口失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show($"关闭AI手动测试窗口失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
                await _testPlc.WriteAnalogValueAsync(channel.TestPLCCommunicationAddress.Substring(1), Convert.ToSingle(AISetValue));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"发送AI测试值失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        /// <summary>
        /// 确认AI显示值
        /// </summary>
        /// <param name="channel">需要确认的AI通道</param>
        /// <remarks>
        /// 该方法确认AI通道的显示值是否正确，执行以下操作：
        /// 1. 将通道的显示值状态设置为“通过”
        /// 2. 更新手动测试状态
        /// 3. 检查并更新通道的总体测试状态
        /// </remarks>
        private void ExecuteConfirmAIValue(ChannelMapping channel)
        {
            try
            {
                if (channel != null)
                {
                    // 更新通道的显示值核对状态为通过
                    channel.ShowValueStatus = "通过";

                    // 更新UI
                    RaisePropertyChanged(nameof(CurrentChannel));

                    // 检查所有子测试是否完成
                    CheckAllSubTestsCompleted(channel);

                    // 更新点位统计
                    UpdatePointStatistics();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"确认AI测试值失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
                await _testPlc.WriteAnalogValueAsync(channel.TestPLCCommunicationAddress.Substring(1), Convert.ToSingle(channel.HighLimit) + 0.01f);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"发送AI高报警测试信号失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
                await _testPlc.WriteAnalogValueAsync(channel.TestPLCCommunicationAddress.Substring(1), Convert.ToSingle(channel.HighHighLimit) + 0.01f);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"发送AI高报警测试信号失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
                await _testPlc.WriteAnalogValueAsync(channel.TestPLCCommunicationAddress.Substring(1), Convert.ToSingle(AISetValue));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"重置AI高报警测试信号失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        /// <summary>
        /// 确认AI高报测试
        /// </summary>
        /// <param name="channel">需要确认高报的AI通道</param>
        /// <remarks>
        /// 该方法确认AI通道的高报功能是否正常，执行以下操作：
        /// 1. 将通道的高报状态设置为“通过”
        /// 2. 更新高报测试状态
        /// 3. 检查并更新通道的总体测试状态
        /// </remarks>
        private void ExecuteConfirmAIHighAlarm(ChannelMapping channel)
        {
            try
            {
                if (channel != null)
                {
                    // 更新通道的高报警状态为通过
                    channel.HighAlarmStatus = "通过";
                    channel.HighHighAlarmStatus = "通过";

                    // 更新UI
                    RaisePropertyChanged(nameof(CurrentChannel));

                    // 检查所有子测试是否完成
                    CheckAllSubTestsCompleted(channel);

                    // 更新点位统计
                    UpdatePointStatistics();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"确认AI高报警失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
                await _testPlc.WriteAnalogValueAsync(channel.TestPLCCommunicationAddress.Substring(1), Convert.ToSingle(channel.LowLimit) - 0.01f);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"发送AI低报警测试信号失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
                await _testPlc.WriteAnalogValueAsync(channel.TestPLCCommunicationAddress.Substring(1), Convert.ToSingle(channel.LowLowLimit) - 0.01f);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"发送AI低报警测试信号失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
                await _testPlc.WriteAnalogValueAsync(channel.TestPLCCommunicationAddress.Substring(1), Convert.ToSingle(AISetValue));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"重置AI低报警测试信号失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        /// <summary>
        /// 确认AI低报测试
        /// </summary>
        /// <param name="channel">需要确认低报的AI通道</param>
        /// <remarks>
        /// 该方法确认AI通道的低报功能是否正常，执行以下操作：
        /// 1. 将通道的低报状态设置为“通过”
        /// 2. 更新低报测试状态
        /// 3. 检查并更新通道的总体测试状态
        /// </remarks>
        private void ExecuteConfirmAILowAlarm(ChannelMapping channel)
        {
            try
            {
                if (channel != null)
                {
                    // 更新通道的低报警状态为通过
                    channel.LowAlarmStatus = "通过";
                    channel.LowLowAlarmStatus = "通过";

                    // 更新UI
                    RaisePropertyChanged(nameof(CurrentChannel));

                    // 检查所有子测试是否完成
                    CheckAllSubTestsCompleted(channel);

                    // 更新点位统计
                    UpdatePointStatistics();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"确认AI低报警失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        /// <summary>
        /// 发送AI维护功能测试信号
        /// </summary>
        /// <param name="channel">需要测试维护功能的AI通道</param>
        /// <remarks>
        /// 该方法发送维护功能测试信号到测试PLC，执行以下操作：
        /// 1. 检查通道是否配置了维护功能开关点地址
        /// 2. 将维护功能开关设置为激活状态
        /// 3. 将维护功能状态写入测试PLC的相应地址
        /// 4. 更新维护功能测试状态
        /// </remarks>
        private async void ExecuteSendAIMaintenance(ChannelMapping channel)
        {
            try
            {
                // 实现发送AI维护测试信号的逻辑
                // 直接执行业务逻辑，不弹出消息框
                await _testPlc.WriteDigitalValueAsync(channel.MaintenanceEnableSwitchPointCommAddress, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"发送AI维护测试信号失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        /// <summary>
        /// 复位AI维护功能测试
        /// </summary>
        /// <param name="channel">需要复位维护功能的AI通道</param>
        /// <remarks>
        /// 该方法将AI通道的维护功能复位到正常状态，执行以下操作：
        /// 1. 检查通道是否配置了维护功能开关点地址
        /// 2. 将维护功能开关设置为非激活状态
        /// 3. 将维护功能状态写入测试PLC的相应地址
        /// 4. 更新维护功能测试状态
        /// </remarks>
        private async void ExecuteResetAIMaintenance(ChannelMapping channel)
        {
            try
            {
                // 实现重置AI维护测试信号的逻辑
                // 直接执行业务逻辑，不弹出消息框
                await _testPlc.WriteDigitalValueAsync(channel.MaintenanceEnableSwitchPointCommAddress, false);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"重置AI维护测试信号失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        /// <summary>
        /// 确认AI维护功能测试
        /// </summary>
        /// <param name="channel">需要确认维护功能的AI通道</param>
        /// <remarks>
        /// 该方法确认AI通道的维护功能是否正常，执行以下操作：
        /// 1. 将通道的维护功能状态设置为“通过”
        /// 2. 更新维护功能测试状态
        /// 3. 检查并更新通道的总体测试状态
        /// </remarks>
        private void ExecuteConfirmAIMaintenance(ChannelMapping channel)
        {
            try
            {
                if (channel != null)
                {
                    // 更新通道的维护功能状态为通过
                    channel.MaintenanceFunction = "通过";

                    // 更新UI
                    RaisePropertyChanged(nameof(CurrentChannel));

                    // 检查所有子测试是否完成
                    CheckAllSubTestsCompleted(channel);

                    // 更新点位统计
                    UpdatePointStatistics();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"确认AI维护功能失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region 7、手动测试 - DI通道
        /// <summary>
        /// 打开DI通道手动测试窗口
        /// </summary>
        /// <param name="channel">需要手动测试的DI通道</param>
        /// <remarks>
        /// 该方法打开DI通道的手动测试窗口，执行以下操作：
        /// 1. 设置当前选中的通道
        /// 2. 初始化手动测试状态
        /// 3. 打开DI手动测试窗口
        /// </remarks>
        private void OpenDIManualTest(ChannelMapping channel)
        {
            try
            {
                if (channel != null && channel.ModuleType?.ToLower() == "di")
                {
                    // 更新当前选中的通道
                    CurrentChannel = channel;
                    // 将当前测试结果设置为当前通道
                    CurrentTestResult = channel;

                    // 初始化测试状态
                    if (channel.ShowValueStatus != "通过")
                        channel.ShowValueStatus = "未测试";

                    // 更新ResultText为手动测试中
                    if (channel.ShowValueStatus == "未测试")
                    {
                        channel.ResultText = "手动测试中";
                    }

                    // 设置DI手动测试窗口打开状态为true
                    IsDIManualTestOpen = true;

                    // 立即更新UI和点位统计
                    RaisePropertyChanged(nameof(AllChannels));
                    UpdatePointStatistics();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开DI手动测试窗口失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        /// <summary>
        /// 关闭DI通道手动测试窗口
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
                MessageBox.Show($"关闭DI手动测试窗口失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
                await _testPlc.WriteDigitalValueAsync(channel.TestPLCCommunicationAddress, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"发送DI测试信号失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
                await _testPlc.WriteDigitalValueAsync(channel.TestPLCCommunicationAddress, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"重置DI测试信号失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        /// <summary>
        /// 确认DI测试
        /// </summary>
        /// <param name="channel">需要确认的DI通道</param>
        /// <remarks>
        /// 该方法确认DI通道的显示值是否正确，执行以下操作：
        /// 1. 将通道的显示值状态设置为“通过”
        /// 2. 更新DI测试状态
        /// 3. 检查并更新通道的总体测试状态
        /// </remarks>
        private void ExecuteConfirmDI(ChannelMapping channel)
        {
            try
            {
                if (channel != null)
                {
                    // 更新通道的显示值核对状态为通过
                    channel.ShowValueStatus = "通过";

                    // 更新UI
                    RaisePropertyChanged(nameof(CurrentChannel));

                    // 检查所有子测试是否完成
                    CheckAllSubTestsCompleted(channel);

                    // 更新点位统计
                    UpdatePointStatistics();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"确认DI测试失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
        private void OpenAOManualTest(ChannelMapping channel)
        {
            try
            {
                if (channel != null && channel.ModuleType?.ToLower() == "ao")
                {
                    // 更新当前选中的通道
                    CurrentChannel = channel;
                    // 将当前测试结果设置为当前通道
                    CurrentTestResult = channel;

                    // 初始化测试状态
                    if (channel.ShowValueStatus != "通过")
                        channel.ShowValueStatus = "未测试";

                    // 更新ResultText为手动测试中
                    if (channel.ShowValueStatus == "未测试")
                    {
                        channel.ResultText = "手动测试中";
                    }

                    // 设置AO手动测试窗口打开状态为true
                    IsAOManualTestOpen = true;

                    // 立即更新UI和点位统计
                    RaisePropertyChanged(nameof(AllChannels));
                    UpdatePointStatistics();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开AO手动测试窗口失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show($"关闭AO手动测试窗口失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    // 启动AO监测逻辑
                    while (channel.ShowValueStatus != "通过")
                    {
                        AOCurrentValue = (await _testPlc.ReadAnalogValueAsync(channel.TestPLCCommunicationAddress.Substring(1))).Data.ToString();
                        await Task.Delay(500);
                    }
                    // 直接执行业务逻辑，不弹出消息框
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"启动AO监测失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        /// <summary>
        /// 确认AO测试
        /// </summary>
        /// <param name="channel">需要确认的AO通道</param>
        /// <remarks>
        /// 该方法确认AO通道的显示值是否正确，执行以下操作：
        /// 1. 将通道的显示值状态设置为“通过”
        /// 2. 更新AO测试状态
        /// 3. 检查并更新通道的总体测试状态
        /// </remarks>
        private void ExecuteConfirmAO(ChannelMapping channel)
        {
            try
            {
                if (channel != null)
                {
                    // 更新通道的显示值核对状态为通过
                    channel.ShowValueStatus = "通过";

                    // 更新UI
                    RaisePropertyChanged(nameof(CurrentChannel));

                    // 检查所有子测试是否完成
                    CheckAllSubTestsCompleted(channel);

                    // 更新点位统计
                    UpdatePointStatistics();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"确认AO测试失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
        private void OpenDOManualTest(ChannelMapping channel)
        {
            try
            {
                if (channel != null && channel.ModuleType?.ToLower() == "do")
                {
                    // 更新当前选中的通道
                    CurrentChannel = channel;
                    // 将当前测试结果设置为当前通道
                    CurrentTestResult = channel;

                    // 初始化测试状态
                    if (channel.ShowValueStatus != "通过")
                        channel.ShowValueStatus = "未测试";

                    // 更新ResultText为手动测试中
                    if (channel.ShowValueStatus == "未测试")
                    {
                        channel.ResultText = "手动测试中";
                    }

                    // 设置DO手动测试窗口打开状态为true
                    IsDOManualTestOpen = true;

                    // 立即更新UI和点位统计
                    RaisePropertyChanged(nameof(AllChannels));
                    UpdatePointStatistics();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开DO手动测试窗口失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        /// <summary>
        /// 关闭DO通道手动测试窗口
        /// </summary>
        /// <remarks>
        /// 该方法关闭DO通道的手动测试窗口，并清空当前选中的通道和测试结果。
        /// </remarks>
        private void ExecuteCloseDOManualTest()
        {
            try
            {
                // 设置DO手动测试窗口打开状态为false
                IsDOManualTestOpen = false;

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
                MessageBox.Show($"关闭DO手动测试窗口失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        /// <summary>
        /// 启动DO监测
        /// </summary>
        /// <param name="channel">需要监测的DO通道</param>
        /// <remarks>
        /// 该方法启动DO通道的监测过程，执行以下操作：
        /// 1. 设置当前监测的DO通道
        /// 2. 循环读取DO通道的状态值
        /// 3. 更新DO监测状态和当前值
        /// </remarks>
        private async void ExecuteStartDOMonitor(ChannelMapping channel)
        {
            try
            {
                //当前传入的数据不为空且未点击确认通过
                if (channel != null && channel.ShowValueStatus != "通过")
                {
                    // 设置当前监测的DO通道
                    CurrentChannel = channel;
                    CurrentTestResult = channel;
                    // 启动DO监测逻辑
                    while (channel.ShowValueStatus != "通过")
                    {
                        DOCurrentValue = (await _testPlc.ReadDigitalValueAsync(channel.TestPLCCommunicationAddress)).Data.ToString();
                        await Task.Delay(500);
                    }
                    // 直接执行业务逻辑，不弹出消息框
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"启动DO监测失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        /// <summary>
        /// 确认DO测试
        /// </summary>
        /// <param name="channel">需要确认的DO通道</param>
        /// <remarks>
        /// 该方法确认DO通道的显示值是否正确，执行以下操作：
        /// 1. 将通道的显示值状态设置为“通过”
        /// 2. 更新DO测试状态
        /// 3. 检查并更新通道的总体测试状态
        /// </remarks>
        private void ExecuteConfirmDO(ChannelMapping channel)
        {
            try
            {
                if (channel != null)
                {
                    // 更新通道的显示值核对状态为通过
                    channel.ShowValueStatus = "通过";

                    // 更新UI
                    RaisePropertyChanged(nameof(CurrentChannel));

                    // 检查所有子测试是否完成
                    CheckAllSubTestsCompleted(channel);

                    // 更新点位统计
                    UpdatePointStatistics();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"确认DO测试失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region 10、测试结果导出
        /// <summary>
        /// 导出测试结果
        /// </summary>
        /// <remarks>
        /// 该方法将所有通道的测试结果导出到Excel文件，执行以下操作：
        /// 1. 调用测试结果导出服务将所有通道数据导出到Excel
        /// 2. 显示导出成功或失败的消息
        /// </remarks>
        private async void ExportTestResults()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "正在导出测试结果，包含所有通道测试数据...";

                // 调用导出服务
                bool result = await _testResultExportService.ExportToExcelAsync(AllChannels);

                if (result)
                {
                    Message = "测试结果导出成功，已导出全部测试数据字段";
                    StatusMessage = "导出完成";
                }
                else
                {
                    Message = "测试结果导出失败";
                    StatusMessage = "导出失败";
                }
            }
            catch (Exception ex)
            {
                Message = $"导出测试结果时出错: {ex.Message}";
                StatusMessage = "导出出错";
            }
            finally
            {
                IsLoading = false;
            }
        }
        /// <summary>
        /// 判断是否可以导出测试结果
        /// </summary>
        /// <remarks>
        /// 该方法判断当前是否可以导出测试结果，满足以下条件时返回true：
        /// 1. 当前有选中的批次
        /// 2. 批次中至少有一个通道已经测试
        /// </remarks>
        /// <returns>如果可以导出测试结果返回true，否则返回false</returns>
        private bool CanExportTestResults()
        {
            // 检查是否所有点位都已测试通过
            return AllChannels != null && AllChannels.Any() &&
                   _testResultExportService.AreAllTestsPassed(AllChannels);
        }


        #endregion

        #region 11、历史记录管理
        /// <summary>
        /// 删除测试批次
        /// </summary>
        /// <remarks>
        /// 该方法删除选中的历史测试批次，执行以下操作：
        /// 1. 验证是否选择了测试批次
        /// 2. 显示确认对话框，询问用户是否确定删除
        /// 3. 调用测试记录服务删除选中的批次
        /// 4. 重新加载历史测试批次列表
        /// </remarks>
        private async void DeleteTestBatch()
        {
            if (SelectedTestBatch == null)
            {
                MessageBox.Show("请先选择一个测试批次", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                // 显示确认对话框
                var result = MessageBox.Show(
                    $"确定要删除测试批次 {SelectedTestBatch.TestTag} 的所有记录吗？此操作不可恢复。",
                    "确认删除",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    IsLoading = true;
                    StatusMessage = $"正在删除测试批次 {SelectedTestBatch.TestTag}...";

                    // 删除测试批次
                    var success = await _testRecordService.DeleteTestBatchAsync(SelectedTestBatch.TestTag);

                    if (success)
                    {
                        // 从列表中移除
                        TestBatches.Remove(SelectedTestBatch);
                        SelectedTestBatch = TestBatches.FirstOrDefault();

                        MessageBox.Show("测试批次已成功删除", "删除成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("删除测试批次失败", "删除失败", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"删除测试批次时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
                StatusMessage = string.Empty;
            }
        }
        /// <summary>
        /// 关闭历史记录窗口
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
                MessageBox.Show("请先选择一个测试批次", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                IsLoading = true;
                StatusMessage = $"正在恢复测试记录 {SelectedTestBatch.TestTag}...";

                // 显示确认对话框
                var result = MessageBox.Show(
                    $"确定要恢复测试批次 {SelectedTestBatch.TestTag} 的记录吗？当前数据将被覆盖。",
                    "确认恢复",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // 恢复测试记录
                    var records = await _testRecordService.RestoreTestRecordsAsync(SelectedTestBatch.TestTag);

                    if (records != null && records.Any())
                    {
                        // 更新AllChannels集合
                        AllChannels = new ObservableCollection<ChannelMapping>(records);

                        // 保存原始通道集合
                        OriginalAllChannels = new ObservableCollection<ChannelMapping>(records);

                        // 更新批次信息
                        await UpdateBatchInfoAsync();

                        // 更新点位统计数据
                        UpdatePointStatistics();

                        // 关闭窗口
                        IsHistoryRecordsOpen = false;

                        MessageBox.Show($"已成功恢复 {records.Count} 条测试记录", "恢复成功", MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                    else
                    {
                        StatusMessage = $"测试记录恢复失败";
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"复测失败: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"复测失败: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion
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