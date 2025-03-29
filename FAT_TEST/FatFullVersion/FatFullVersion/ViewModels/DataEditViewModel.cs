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

namespace FatFullVersion.ViewModels
{
    public class DataEditViewModel : BindableBase
    {
        #region 属性和字段

        private readonly IPointDataService _pointDataService;
        private readonly IChannelMappingService _channelMappingService;
        private readonly ITestTaskManager _testTaskManager;
        private readonly IEventAggregator _eventAggregator;

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
            _confirmWiringCompleteCommand ??= new DelegateCommand(ExecuteConfirmWiringComplete, CanExecuteConfirmWiringComplete);

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

        #endregion

        public DataEditViewModel(
            IPointDataService pointDataService, 
            IChannelMappingService channelMappingService,
            ITestTaskManager testTaskManager,
            IEventAggregator eventAggregator)
        {
            _pointDataService = pointDataService;
            _channelMappingService = channelMappingService;
            _testTaskManager = testTaskManager;
            _eventAggregator = eventAggregator;

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

            // 尝试从通道映射信息中提取批次信息
            InitializeBatchData();
        }

        /// <summary>
        /// 根据选择的通道类型更新当前显示的通道集合
        /// </summary>
        private void UpdateCurrentChannels()
        {
            if (string.IsNullOrEmpty(SelectedChannelType) || AllChannels == null)
                return;

            try
            {
                var filteredChannels = SelectedChannelType switch
                {
                    "AI通道" => AllChannels.Where(c => c.ModuleType?.ToLower() == "ai"),
                    "AO通道" => AllChannels.Where(c => c.ModuleType?.ToLower() == "ao"),
                    "DI通道" => AllChannels.Where(c => c.ModuleType?.ToLower() == "di"),
                    "DO通道" => AllChannels.Where(c => c.ModuleType?.ToLower() == "do"),
                    _ => Enumerable.Empty<ChannelMapping>()
                };

                CurrentChannels = new ObservableCollection<ChannelMapping>(filteredChannels);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新当前通道失败: {ex.Message}");
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

                        DataType = point.DataType,
                        RangeLowerLimitValue = (float)point.LowLowLimit,
                        RangeUpperLimitValue = (float)point.HighHighLimit,
                        // AI点位不初始化百分比值，这些值将在测试过程中填充
                        TestResultStatus = 0, // 未测试
                        ResultText = "未测试"
                    };
                    AllChannels.Add(channel);

                    // 创建对应的测试结果
                    //var result = new ChannelMapping
                    //{
                    //    TestId = TestResults.Count + 1,
                    //    VariableName = point.VariableName,
                    //    ModuleType = point.ModuleType,
                    //    DataType = point.DataType,
                    //    ChannelTag = point.ChannelTag,
                    //    RangeLowerLimitValue = (float)point.LowLowLimit,
                    //    RangeUpperLimitValue = (float)point.HighHighLimit,
                    //    // AI点位不初始化百分比值，这些值将在测试过程中填充
                    //    TestResultStatus = 0, // 未测试
                    //    ResultText = "未测试"
                    //};
                    //TestResults.Add(result);
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

                        DataType = point.DataType,
                        RangeLowerLimitValue = (float)point.LowLowLimit,
                        RangeUpperLimitValue = (float)point.HighHighLimit,
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

                    // 创建对应的测试结果
                    //var result = new ChannelMapping
                    //{
                    //    TestId = TestResults.Count + 1,
                    //    VariableName = point.VariableName,
                    //    ModuleType = point.ModuleType,
                    //    DataType = point.DataType,
                    //    ChannelTag = point.ChannelTag,
                    //    RangeLowerLimitValue = (float)point.LowLowLimit,
                    //    RangeUpperLimitValue = (float)point.HighHighLimit,
                    //    // AO点位相关设置
                    //    TestResultStatus = 0, // 未测试
                    //    ResultText = "未测试",
                    //    // AO点位的低低报、低报、高报、高高报设置为N/A
                    //    LowLowAlarmStatus = "N/A",
                    //    LowAlarmStatus = "N/A",
                    //    HighAlarmStatus = "N/A",
                    //    HighHighAlarmStatus = "N/A"
                    //};
                    //TestResults.Add(result);
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

                    // 创建对应的测试结果
                    //var result = new ChannelMapping
                    //{
                    //    TestId = TestResults.Count + 1,
                    //    VariableName = point.VariableName,
                    //    ModuleType = point.ModuleType,
                    //    DataType = point.DataType,
                    //    ChannelTag = point.ChannelTag,
                    //    TestResultStatus = 0, // 未测试
                    //    ResultText = "未测试",
                    //    // 为DI点位设置NaN值
                    //    RangeLowerLimitValue = float.NaN,
                    //    RangeUpperLimitValue = float.NaN, 
                    //    Value0Percent = double.NaN,
                    //    Value25Percent = double.NaN,
                    //    Value50Percent = double.NaN,
                    //    Value75Percent = double.NaN,
                    //    Value100Percent = double.NaN,
                    //    LowLowAlarmStatus = "N/A",
                    //    LowAlarmStatus = "N/A",
                    //    HighAlarmStatus = "N/A",
                    //    HighHighAlarmStatus = "N/A",
                    //    MaintenanceFunction = "N/A"
                    //};
                    //TestResults.Add(result);
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

                    // 创建对应的测试结果
                    //var result = new ChannelMapping
                    //{
                    //    TestId = TestResults.Count + 1,
                    //    VariableName = point.VariableName,
                    //    ModuleType = point.ModuleType,
                    //    DataType = point.DataType,
                    //    ChannelTag = point.ChannelTag,
                    //    TestResultStatus = 0, // 未测试
                    //    ResultText = "未测试",
                    //    // 为DO点位设置NaN值
                    //    RangeLowerLimitValue = float.NaN,
                    //    RangeUpperLimitValue = float.NaN, 
                    //    Value0Percent = double.NaN,
                    //    Value25Percent = double.NaN,
                    //    Value50Percent = double.NaN,
                    //    Value75Percent = double.NaN,
                    //    Value100Percent = double.NaN,
                    //    LowLowAlarmStatus = "N/A",
                    //    LowAlarmStatus = "N/A",
                    //    HighAlarmStatus = "N/A",
                    //    HighHighAlarmStatus = "N/A",
                    //    MaintenanceFunction = "N/A"
                    //};
                    //TestResults.Add(result);
                }
                //当Excel中点位解析完成后并且已经初始化完ChannelMapping后调用自动分配程序分配点位
                var channelsMappingResult = await _channelMappingService.AllocateChannelsTestAsync(
                    AllChannels);
                
                //通道分配完成之后同步更新结果表位中的对应数据
                _channelMappingService.SyncChannelAllocation(
                    channelsMappingResult);
                
                //通知前端页面更新数据
                RaisePropertyChanged(nameof(AllChannels));

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
                            SelectedBatch = Batches[0];
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
                    MessageBox.Show($"未找到批次 {SelectedBatch.BatchName} 的通道信息", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 获取当前批次的所有通道ID
                var batchChannelVariableNames = batchChannels.Select(c => c.VariableName).ToHashSet();
                
                // 筛选当前显示的通道集合，只显示当前批次的通道
                AllChannels = new ObservableCollection<ChannelMapping>(
                    OriginalAllChannels.Where(c => batchChannelVariableNames.Contains(c.VariableName)));
                
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

        private void CancelBatchSelection()
        {
            IsBatchSelectionOpen = false;
        }

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
                var result = await _testTaskManager.ConfirmWiringCompleteAsync(batchInfo, false,AllChannels);
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
                // 显示等待测试的画面
                await _testTaskManager.ShowTestProgressDialogAsync();
                
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

                Message = $"测试已启动，批次: {string.Join(", ", affectedBatchNames)}";
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

            RaisePropertyChanged(nameof(AllChannels));
            
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
                
                // 保存当前选中的批次ID
                string selectedBatchId = SelectedBatch?.BatchId;
                
                // 更新批次集合
                if (updatedBatches != null && updatedBatches.Any())
                {
                    Batches = new ObservableCollection<BatchInfo>(updatedBatches);
                    
                    // 如果之前有选中的批次，尝试找回并选中
                    if (!string.IsNullOrEmpty(selectedBatchId))
                    {
                        var updatedSelectedBatch = Batches.FirstOrDefault(b => b != null && b.BatchId == selectedBatchId);
                        if (updatedSelectedBatch != null)
                        {
                            // 直接设置字段而不触发OnBatchSelected
                            _selectedBatch = updatedSelectedBatch;
                            RaisePropertyChanged(nameof(SelectedBatch));
                            
                            // 更新接线确认按钮状态
                            IsWiringCompleteBtnEnabled = updatedSelectedBatch.Status == "未开始" || updatedSelectedBatch.Status == "测试中";
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
            if (AllChannels == null) return;
            
            // 如果有当前选中的批次，只计算该批次的点位
            var channelsToCount = SelectedBatch != null && !string.IsNullOrEmpty(SelectedBatch.BatchName)
                ? AllChannels.Where(c => c.TestBatch == SelectedBatch.BatchName).ToList()
                : AllChannels.ToList();
            
            TotalPointCount = $"全部点位数量:{channelsToCount.Count}";
            TestedPointCount = $"已测试点位数量:{channelsToCount.Count(r => r.TestResultStatus > 0)}";
            WaitingPointCount = $"待测试点位数量:{channelsToCount.Count(r => r.TestResultStatus == 0)}";
            SuccessPointCount = $"成功点位数量:{channelsToCount.Count(r => r.TestResultStatus == 1)}";
            FailurePointCount = $"失败点位数量:{channelsToCount.Count(r => r.TestResultStatus == 2)}";
        }

        // 添加过滤逻辑方法
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

        // 修改批次选择逻辑
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

        // 添加LoadTestResults方法
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
        /// 执行完成接线确认操作
        /// </summary>
        private void ExecuteConfirmWiringComplete()
        {
            try
            {
                // 接线确认逻辑实现
                MessageBox.Show("接线确认完成", "操作成功", MessageBoxButton.OK, MessageBoxImage.Information);
                // 设置批次状态为已接线
                if (SelectedBatch != null)
                {
                    SelectedBatch.Status = "已接线";
                    IsStartTestButtonEnabled = true;
                    IsWiringCompleteBtnEnabled = false;
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
        /// 执行向上移动通道操作
        /// </summary>
        private void ExecuteMoveUp(ChannelMapping channel)
        {
            if (channel == null || TestQueue == null || !TestQueue.Any())
                return;

            try
            {
                int index = TestQueue.IndexOf(channel);
                if (index > 0)
                {
                    TestQueue.Move(index, index - 1);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"移动通道失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 执行向下移动通道操作
        /// </summary>
        private void ExecuteMoveDown(ChannelMapping channel)
        {
            if (channel == null || TestQueue == null || !TestQueue.Any())
                return;

            try
            {
                int index = TestQueue.IndexOf(channel);
                if (index < TestQueue.Count - 1 && index >= 0)
                {
                    TestQueue.Move(index, index + 1);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"移动通道失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 执行分配通道操作
        /// </summary>
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
        /// 打开AI手动测试窗口
        /// </summary>
        private void ExecuteOpenAIManualTest(ChannelMapping channel)
        {
            try
            {
                if (channel != null && channel.ModuleType?.ToLower() == "ai")
                {
                    // 实现打开AI手动测试窗口的逻辑
                    MessageBox.Show($"打开AI通道 {channel.ChannelTag} 的手动测试窗口", "手动测试", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开AI手动测试窗口失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 打开DI手动测试窗口
        /// </summary>
        private void ExecuteOpenDIManualTest(ChannelMapping channel)
        {
            try
            {
                if (channel != null && channel.ModuleType?.ToLower() == "di")
                {
                    // 实现打开DI手动测试窗口的逻辑
                    MessageBox.Show($"打开DI通道 {channel.ChannelTag} 的手动测试窗口", "手动测试", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开DI手动测试窗口失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 打开DO手动测试窗口
        /// </summary>
        private void ExecuteOpenDOManualTest(ChannelMapping channel)
        {
            try
            {
                if (channel != null && channel.ModuleType?.ToLower() == "do")
                {
                    // 实现打开DO手动测试窗口的逻辑
                    MessageBox.Show($"打开DO通道 {channel.ChannelTag} 的手动测试窗口", "手动测试", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开DO手动测试窗口失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 打开AO手动测试窗口
        /// </summary>
        private void ExecuteOpenAOManualTest(ChannelMapping channel)
        {
            try
            {
                if (channel != null && channel.ModuleType?.ToLower() == "ao")
                {
                    // 实现打开AO手动测试窗口的逻辑
                    MessageBox.Show($"打开AO通道 {channel.ChannelTag} 的手动测试窗口", "手动测试", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开AO手动测试窗口失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 关闭AI手动测试窗口
        /// </summary>
        private void ExecuteCloseAIManualTest()
        {
            try
            {
                // 实现关闭AI手动测试窗口的逻辑
                MessageBox.Show("关闭AI手动测试窗口", "手动测试", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"关闭AI手动测试窗口失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 关闭DI手动测试窗口
        /// </summary>
        private void ExecuteCloseDIManualTest()
        {
            try
            {
                // 实现关闭DI手动测试窗口的逻辑
                MessageBox.Show("关闭DI手动测试窗口", "手动测试", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"关闭DI手动测试窗口失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 关闭DO手动测试窗口
        /// </summary>
        private void ExecuteCloseDOManualTest()
        {
            try
            {
                // 实现关闭DO手动测试窗口的逻辑
                MessageBox.Show("关闭DO手动测试窗口", "手动测试", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"关闭DO手动测试窗口失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 关闭AO手动测试窗口
        /// </summary>
        private void ExecuteCloseAOManualTest()
        {
            try
            {
                // 实现关闭AO手动测试窗口的逻辑
                MessageBox.Show("关闭AO手动测试窗口", "手动测试", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"关闭AO手动测试窗口失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 执行发送AI测试值
        /// </summary>
        private void ExecuteSendAITestValue()
        {
            try
            {
                // 实现发送AI测试值的逻辑
                MessageBox.Show("已发送AI测试值", "测试", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"发送AI测试值失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 确认AI测试值
        /// </summary>
        private void ExecuteConfirmAIValue(ChannelMapping channel)
        {
            try
            {
                if (channel != null)
                {
                    // 实现确认AI测试值的逻辑
                    channel.TestResultStatus = 1; // 通过
                    channel.ResultText = "AI测试值确认通过";
                    MessageBox.Show($"已确认AI通道 {channel.ChannelTag} 的测试值", "测试", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"确认AI测试值失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 发送AI高报警测试信号
        /// </summary>
        private void ExecuteSendAIHighAlarm()
        {
            try
            {
                // 实现发送AI高报警测试信号的逻辑
                MessageBox.Show("已发送AI高报警测试信号", "测试", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"发送AI高报警测试信号失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 重置AI高报警测试信号
        /// </summary>
        private void ExecuteResetAIHighAlarm()
        {
            try
            {
                // 实现重置AI高报警测试信号的逻辑
                MessageBox.Show("已重置AI高报警测试信号", "测试", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"重置AI高报警测试信号失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 确认AI高报警测试
        /// </summary>
        private void ExecuteConfirmAIHighAlarm(ChannelMapping channel)
        {
            try
            {
                if (channel != null)
                {
                    // 实现确认AI高报警测试的逻辑
                    channel.HighAlarmStatus = "通过";
                    MessageBox.Show($"已确认AI通道 {channel.ChannelTag} 的高报警测试", "测试", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"确认AI高报警测试失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 发送AI低报警测试信号
        /// </summary>
        private void ExecuteSendAILowAlarm()
        {
            try
            {
                // 实现发送AI低报警测试信号的逻辑
                MessageBox.Show("已发送AI低报警测试信号", "测试", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"发送AI低报警测试信号失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 重置AI低报警测试信号
        /// </summary>
        private void ExecuteResetAILowAlarm()
        {
            try
            {
                // 实现重置AI低报警测试信号的逻辑
                MessageBox.Show("已重置AI低报警测试信号", "测试", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"重置AI低报警测试信号失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 确认AI低报警测试
        /// </summary>
        private void ExecuteConfirmAILowAlarm(ChannelMapping channel)
        {
            try
            {
                if (channel != null)
                {
                    // 实现确认AI低报警测试的逻辑
                    channel.LowAlarmStatus = "通过";
                    MessageBox.Show($"已确认AI通道 {channel.ChannelTag} 的低报警测试", "测试", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"确认AI低报警测试失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 发送AI维护测试信号
        /// </summary>
        private void ExecuteSendAIMaintenance()
        {
            try
            {
                // 实现发送AI维护测试信号的逻辑
                MessageBox.Show("已发送AI维护测试信号", "测试", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"发送AI维护测试信号失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 重置AI维护测试信号
        /// </summary>
        private void ExecuteResetAIMaintenance()
        {
            try
            {
                // 实现重置AI维护测试信号的逻辑
                MessageBox.Show("已重置AI维护测试信号", "测试", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"重置AI维护测试信号失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 确认AI维护测试
        /// </summary>
        private void ExecuteConfirmAIMaintenance(ChannelMapping channel)
        {
            try
            {
                if (channel != null)
                {
                    // 实现确认AI维护测试的逻辑
                    channel.MaintenanceFunction = "通过";
                    MessageBox.Show($"已确认AI通道 {channel.ChannelTag} 的维护测试", "测试", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"确认AI维护测试失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 发送DI测试信号
        /// </summary>
        private void ExecuteSendDITest()
        {
            try
            {
                // 实现发送DI测试信号的逻辑
                MessageBox.Show("已发送DI测试信号", "测试", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"发送DI测试信号失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 重置DI测试信号
        /// </summary>
        private void ExecuteResetDI()
        {
            try
            {
                // 实现重置DI测试信号的逻辑
                MessageBox.Show("已重置DI测试信号", "测试", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"重置DI测试信号失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 确认DI测试
        /// </summary>
        private void ExecuteConfirmDI(ChannelMapping channel)
        {
            try
            {
                if (channel != null)
                {
                    // 实现确认DI测试的逻辑
                    channel.TestResultStatus = 1; // 通过
                    channel.ResultText = "DI测试确认通过";
                    MessageBox.Show($"已确认DI通道 {channel.ChannelTag} 的测试", "测试", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"确认DI测试失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 启动DO监测
        /// </summary>
        private void ExecuteStartDOMonitor(ChannelMapping channel)
        {
            try
            {
                if (channel != null)
                {
                    // 实现启动DO监测的逻辑
                    MessageBox.Show($"已启动DO通道 {channel.ChannelTag} 的监测", "测试", MessageBoxButton.OK, MessageBoxImage.Information);
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
        private void ExecuteConfirmDO(ChannelMapping channel)
        {
            try
            {
                if (channel != null)
                {
                    // 实现确认DO测试的逻辑
                    channel.TestResultStatus = 1; // 通过
                    channel.ResultText = "DO测试确认通过";
                    MessageBox.Show($"已确认DO通道 {channel.ChannelTag} 的测试", "测试", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"确认DO测试失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 启动AO监测
        /// </summary>
        private void ExecuteStartAOMonitor(ChannelMapping channel)
        {
            try
            {
                if (channel != null)
                {
                    // 实现启动AO监测的逻辑
                    MessageBox.Show($"已启动AO通道 {channel.ChannelTag} 的监测", "测试", MessageBoxButton.OK, MessageBoxImage.Information);
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
        private void ExecuteConfirmAO(ChannelMapping channel)
        {
            try
            {
                if (channel != null)
                {
                    // 实现确认AO测试的逻辑
                    channel.TestResultStatus = 1; // 通过
                    channel.ResultText = "AO测试确认通过";
                    MessageBox.Show($"已确认AO通道 {channel.ChannelTag} 的测试", "测试", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"确认AO测试失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 处理测试结果更新事件
        /// </summary>
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
                        
                        foreach (var channel in batchChannels)
                        {
                            // 检查该通道是否已经有测试结果状态
                            if (channel.HardPointTestResult == "通过" || channel.HardPointTestResult == "已通过")
                            {
                                // 设置测试结果状态为通过(1)
                                channel.TestResultStatus = 1;
                                
                                // 更新结果文本
                                if (string.IsNullOrEmpty(channel.ResultText) || 
                                    channel.ResultText == "未测试" || 
                                    channel.ResultText == "等待测试")
                                {
                                    channel.ResultText = $"{channel.ModuleType}硬点测试通过";
                                }
                                else if(!channel.ResultText.Contains($"{channel.ModuleType}硬点测试通过"))
                                {
                                    channel.ResultText += $", {channel.ModuleType}硬点测试通过";
                                }
                            }
                            else if (!string.IsNullOrEmpty(channel.HardPointTestResult) && 
                                     channel.HardPointTestResult != "未测试")
                            {
                                // 设置测试结果状态为失败(2)
                                channel.TestResultStatus = 2;
                                
                                // 更新结果文本
                                if (string.IsNullOrEmpty(channel.ResultText) || 
                                    channel.ResultText == "未测试" || 
                                    channel.ResultText == "等待测试")
                                {
                                    channel.ResultText = $"{channel.ModuleType}硬点测试失败: {channel.HardPointTestResult}";
                                }
                                else if(!channel.ResultText.Contains(channel.HardPointTestResult))
                                {
                                    channel.ResultText += $", {channel.ModuleType}硬点测试失败: {channel.HardPointTestResult}";
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
        /// 刷新批次状态
        /// </summary>
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

                // 更新批次状态
                if (testedChannels == 0)
                {
                    SelectedBatch.Status = "未开始";
                }
                else if (testedChannels < totalChannels)
                {
                    SelectedBatch.Status = "进行中";
                }
                else if (passedChannels == totalChannels)
                {
                    SelectedBatch.Status = "已完成";
                }
                else
                {
                    SelectedBatch.Status = "部分失败";
                }

                // 更新批次的测试时间
                if (testedChannels > 0 && SelectedBatch.FirstTestTime == null)
                {
                    SelectedBatch.FirstTestTime = DateTime.Now;
                }
                SelectedBatch.LastTestTime = DateTime.Now;

                // 更新接线确认按钮状态
                IsWiringCompleteBtnEnabled = SelectedBatch.Status == "未开始" || SelectedBatch.Status == "进行中";
                
                // 通知UI更新
                RaisePropertyChanged(nameof(SelectedBatch));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"刷新批次状态时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 刷新测试队列
        /// </summary>
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
