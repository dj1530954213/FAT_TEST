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
        private ObservableCollection<ChannelMapping> _testResults;
        public ObservableCollection<ChannelMapping> TestResults
        {
            get => _testResults;
            set => SetProperty(ref _testResults, value);
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
            IEventAggregator eventAggregator,
            IChannelMappingService channelMappingService,
            IPointDataService pointDataService,
            ITestTaskManager testTaskManager)
        {
            _eventAggregator = eventAggregator;
            _channelMappingService = channelMappingService;
            _pointDataService = pointDataService;
            _testTaskManager = testTaskManager;
            
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

            // 初始化集合
            AllChannels = new ObservableCollection<ChannelMapping>();
            CurrentChannels = new ObservableCollection<ChannelMapping>();
            TestResults = new ObservableCollection<ChannelMapping>();
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
                        SHHSetValueNumber = point.SHHSetValueNumber,
                        PlcCommunicationAddress = point.CommunicationAddress
                    };
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
                        SHHSetValueNumber = point.SHHSetValueNumber,
                        PlcCommunicationAddress = point.CommunicationAddress
                    };
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
                        // AO点位相关设置
                        TestResultStatus = 0, // 未测试
                        ResultText = "未测试",
                        // AO点位的低低报、低报、高报、高高报设置为N/A
                        LowLowAlarmStatus = "N/A",
                        LowAlarmStatus = "N/A",
                        HighAlarmStatus = "N/A",
                        HighHighAlarmStatus = "N/A"
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
                        ModuleType = point.ModuleType,
                        PlcCommunicationAddress = point.CommunicationAddress
                    };
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
                        ModuleType = point.ModuleType,
                        PlcCommunicationAddress = point.CommunicationAddress
                    };
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
                var channelsMappingResult = await _channelMappingService.AllocateChannelsTestAsync(
                    AllChannels,
                    TestResults);
                
                //通道分配完成之后同步更新结果表位中的对应数据
                _channelMappingService.SyncChannelAllocation(
                    channelsMappingResult, 
                    TestResults);
                
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
        private async void ExecuteSelectBatch()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "正在获取批次信息...";

                // 使用原始通道数据更新批次信息，确保批次列表完整
                if (OriginalAllChannels != null)
                {
                    // 使用通道映射服务提取批次信息
                    Batches = new ObservableCollection<BatchInfo>(
                        await _channelMappingService.ExtractBatchInfoAsync(
                            _channelMappingService.GetAIChannels(OriginalAllChannels).ToList(),
                            _channelMappingService.GetAOChannels(OriginalAllChannels).ToList(), 
                            _channelMappingService.GetDIChannels(OriginalAllChannels).ToList(), 
                            _channelMappingService.GetDOChannels(OriginalAllChannels).ToList()));
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
                        var result = TestResults.FirstOrDefault(r => 
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
                    await RefreshBatchStatus();
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
                if (TestResults != null)
                {
                    foreach (var item in TestResults)
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
                var batches = await _channelMappingService.ExtractBatchInfoAsync(
                    GetAIChannels().ToList(), 
                    GetAOChannels().ToList(), 
                    GetDIChannels().ToList(), 
                    GetDOChannels().ToList());
                
                // 根据测试结果更新批次状态
                var updatedBatches = await _channelMappingService.UpdateBatchStatusAsync(batches, TestResults);
                
                // 保存当前选中的批次ID
                string selectedBatchId = SelectedBatch?.BatchId;
                
                // 更新批次集合
                Batches = new ObservableCollection<BatchInfo>(updatedBatches);
                
                // 如果之前有选中的批次，尝试找回并选中
                if (!string.IsNullOrEmpty(selectedBatchId))
                {
                    var updatedSelectedBatch = Batches.FirstOrDefault(b => b.BatchId == selectedBatchId);
                    if (updatedSelectedBatch != null)
                    {
                        // 直接设置字段而不触发OnBatchSelected
                        _selectedBatch = updatedSelectedBatch;
                        RaisePropertyChanged(nameof(SelectedBatch));
                        
                        // 更新接线确认按钮状态
                        IsWiringCompleteBtnEnabled = updatedSelectedBatch.Status == "未开始" || updatedSelectedBatch.Status == "进行中";
                    }
                }

                // 更新点位统计数据
                UpdatePointStatistics();

                // 通知UI更新
                RaisePropertyChanged(nameof(TestResults));
            }
            catch (Exception ex)
            {
                Message = $"更新批次信息失败: {ex.Message}";
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
                OriginalAllChannels = null;

                // 清除所有通道分配信息
                AllChannels = new ObservableCollection<ChannelMapping>(
                    await _channelMappingService.ClearAllChannelAllocationsAsync(AllChannels));

                // 更新当前显示的通道集合
                UpdateCurrentChannels();

                // 同步更新测试结果中的通道信息
                foreach (var result in TestResults)
                {
                    result.TestBatch = string.Empty;
                    result.TestPLCChannelTag = string.Empty;
                    result.TestPLCCommunicationAddress = string.Empty;
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
                
                // 使用追加模式更新结果文本
                if (CurrentTestResult.ResultText == "未测试" || CurrentTestResult.ResultText == "等待测试")
                {
                    CurrentTestResult.ResultText = "AI硬点测试通过";
                }
                else
                {
                    CurrentTestResult.ResultText += ", AI硬点测试通过";
                }
                
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
                // 同时设置高报和高高报状态为已通过
                CurrentTestResult.HighAlarmStatus = "已通过";
                CurrentTestResult.HighHighAlarmStatus = "已通过";
                
                // 追加结果文本
                if (CurrentTestResult.ResultText == "未测试" || CurrentTestResult.ResultText == "等待测试")
                {
                    CurrentTestResult.ResultText = "AI高报/高高报测试通过";
                }
                else
                {
                    CurrentTestResult.ResultText += ", AI高报/高高报测试通过";
                }
                
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
                // 同时设置低报和低低报状态为已通过
                CurrentTestResult.LowAlarmStatus = "已通过";
                CurrentTestResult.LowLowAlarmStatus = "已通过";
                
                // 追加结果文本
                if (CurrentTestResult.ResultText == "未测试" || CurrentTestResult.ResultText == "等待测试")
                {
                    CurrentTestResult.ResultText = "AI低报/低低报测试通过";
                }
                else
                {
                    CurrentTestResult.ResultText += ", AI低报/低低报测试通过";
                }
                
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
                
                // 使用追加模式更新结果文本
                if (CurrentTestResult.ResultText == "未测试" || CurrentTestResult.ResultText == "等待测试")
                {
                    CurrentTestResult.ResultText = "AI维护功能测试通过";
                }
                else
                {
                    CurrentTestResult.ResultText += ", AI维护功能测试通过";
                }
                
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
                
                // 使用追加模式更新结果文本
                if (CurrentTestResult.ResultText == "未测试" || CurrentTestResult.ResultText == "等待测试")
                {
                    CurrentTestResult.ResultText = "DI硬点测试通过";
                }
                else
                {
                    CurrentTestResult.ResultText += ", DI硬点测试通过";
                }
                
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
                
                // 使用追加模式更新结果文本
                if (CurrentTestResult.ResultText == "未测试" || CurrentTestResult.ResultText == "等待测试")
                {
                    CurrentTestResult.ResultText = "DO硬点测试通过";
                }
                else
                {
                    CurrentTestResult.ResultText += ", DO硬点测试通过";
                }
                
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
                
                // 使用追加模式更新结果文本
                if (CurrentTestResult.ResultText == "未测试" || CurrentTestResult.ResultText == "等待测试")
                {
                    CurrentTestResult.ResultText = "AO硬点测试通过";
                }
                else
                {
                    CurrentTestResult.ResultText += ", AO硬点测试通过";
                }
                
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

        private async void ExecuteAllocateChannels()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "正在执行通道分配...";

                // 检查是否有通道数据
                if (AllChannels == null || AllChannels.Count == 0)
                {
                    MessageBox.Show("没有通道数据，请先导入点表配置", "分配失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 调用服务进行通道分配
                var allocatedChannels = await _channelMappingService.AllocateChannelsTestAsync(AllChannels, TestResults);

                // 更新通道分配结果
                AllChannels = new ObservableCollection<ChannelMapping>(allocatedChannels);

                // 同步更新测试结果中的通道信息
                _channelMappingService.SyncChannelAllocation(AllChannels, TestResults);

                // 更新通道显示
                UpdateCurrentChannels();

                // 更新批次信息
                await UpdateBatchInfoAsync();

                // 刷新测试队列
                RefreshTestQueue();

                Message = "通道分配完成";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"通道分配失败: {ex.Message}", "操作失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
                StatusMessage = string.Empty;
            }
        }

        /// <summary>
        /// 刷新测试队列，将待测试的点位添加到队列中
        /// </summary>
        private void RefreshTestQueue()
        {
            // 清空当前队列
            TestQueue.Clear();

            // 先添加所有批次中的未测试点位
            var untested = TestResults.Where(r => 
                r.TestResultStatus == 0 && 
                !string.IsNullOrEmpty(r.TestBatch) &&
                !string.IsNullOrEmpty(r.TestPLCChannelTag))
                .OrderBy(r => r.TestBatch)
                .ThenBy(r => r.ModuleType)
                .ThenBy(r => r.VariableName)
                .ToList();

            foreach (var item in untested)
            {
                TestQueue.Add(item);
            }

            // 更新队列状态
            if (TestQueue.Count > 0)
            {
                TestQueuePosition = 1;
                CurrentQueueItem = TestQueue[0];
                TestQueueStatus = $"队列 1/{TestQueue.Count} - {CurrentQueueItem.VariableName}";
            }
            else
            {
                TestQueuePosition = 0;
                CurrentQueueItem = null;
                TestQueueStatus = "队列为空";
            }

            // 通知UI更新
            RaisePropertyChanged(nameof(TestQueue));
            RaisePropertyChanged(nameof(TestQueuePosition));
            RaisePropertyChanged(nameof(CurrentQueueItem));
            RaisePropertyChanged(nameof(TestQueueStatus));
        }

        #endregion

        #region 添加命令执行和判断方法

        private bool CanExecuteConfirmWiringComplete()
        {
            // 只有当选择了批次时才能确认接线
            return SelectedBatch != null;
        }

        private async void ExecuteConfirmWiringComplete()
        {
            if (SelectedBatch == null)
                return;

            // 将ViewModel中的BatchInfo转换为Model中的BatchInfo
            var batchInfo = new Models.BatchInfo
            {
                BatchId = SelectedBatch.BatchId,
                BatchName = SelectedBatch.BatchName,
                CreationDate = SelectedBatch.CreationDate,
                ItemCount = SelectedBatch.ItemCount,
                Status = SelectedBatch.Status
            };

            // 调用接线确认方法，但不显示等待对话框，不开始测试
            var result = await _testTaskManager.ConfirmWiringCompleteAsync(batchInfo, false,AllChannels);
            if (result)
            {
                // 确认接线成功后，禁用接线确认按钮，启用通道硬点自动测试按钮
                IsWiringCompleteBtnEnabled = false;
                IsStartTestButtonEnabled = true;
                
                // 测试完成后，刷新批次状态
                await RefreshBatchStatus();
            }
        }

        // 添加批次状态刷新方法
        private async Task RefreshBatchStatus()
        {
            if (Batches != null && TestResults != null)
            {
                try
                {
                    StatusMessage = "正在刷新批次状态...";
                    
                    // 更新批次状态
                    var updatedBatches = await _channelMappingService.UpdateBatchStatusAsync(Batches, TestResults);
                    
                    // 更新UI上的批次集合
                    Batches = new ObservableCollection<BatchInfo>(updatedBatches);
                    
                    // 找到并更新选中的批次
                    if (SelectedBatch != null)
                    {
                        var updatedSelectedBatch = Batches.FirstOrDefault(b => b.BatchId == SelectedBatch.BatchId);
                        if (updatedSelectedBatch != null)
                        {
                            // 注意这里不使用属性赋值，避免再次触发OnBatchSelected
                            _selectedBatch = updatedSelectedBatch;
                            RaisePropertyChanged(nameof(SelectedBatch));
                            
                            // 更新接线确认按钮状态
                            IsWiringCompleteBtnEnabled = updatedSelectedBatch.Status == "未开始" || updatedSelectedBatch.Status == "进行中";
                        }
                    }
                    
                    // 更新点位统计
                    UpdatePointStatistics();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"刷新批次状态失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    StatusMessage = string.Empty;
                }
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
            if (SelectedBatch == null)
                return;
        
            try
            {
                IsLoading = true;
                StatusMessage = "正在加载测试结果数据...";
            
                // 根据选中的批次加载对应的测试结果
                var results = AllChannels?
                    .Where(c => c.TestBatch == SelectedBatch.BatchName)
                    .ToList();
            
                if (results != null && results.Any())
                {
                    // 保留现有的测试结果，只添加新的结果
                    if (TestResults == null)
                    {
                        TestResults = new ObservableCollection<ChannelMapping>(results);
                    }
                    else
                    {
                        // 获取当前不存在的新结果
                        var existingIds = TestResults.Select(r => r.VariableName).ToHashSet();
                        var newResults = results.Where(r => !existingIds.Contains(r.VariableName)).ToList();
                        
                        // 添加新结果
                        foreach (var newResult in newResults)
                        {
                            TestResults.Add(newResult);
                        }
                        
                        // 更新现有结果的状态
                        foreach (var result in results)
                        {
                            var existingResult = TestResults.FirstOrDefault(r => r.VariableName == result.VariableName);
                            if (existingResult != null)
                            {
                                // 更新必要的属性，但保留测试结果
                                existingResult.TestBatch = result.TestBatch;
                                existingResult.ChannelTag = result.ChannelTag;
                                existingResult.TestPLCChannelTag = result.TestPLCChannelTag;
                            }
                        }
                    }
                }
                else
                {
                    // 如果找不到基于BatchName的结果，尝试使用BatchId
                    results = AllChannels?
                        .Where(c => c.TestBatch == SelectedBatch.BatchId)
                        .ToList();
                        
                    if (results != null && results.Any())
                    {
                        // 使用相同的逻辑处理BatchId查找的结果
                        if (TestResults == null)
                        {
                            TestResults = new ObservableCollection<ChannelMapping>(results);
                        }
                        else
                        {
                            var existingIds = TestResults.Select(r => r.VariableName).ToHashSet();
                            var newResults = results.Where(r => !existingIds.Contains(r.VariableName)).ToList();
                            
                            foreach (var newResult in newResults)
                            {
                                TestResults.Add(newResult);
                            }
                            
                            foreach (var result in results)
                            {
                                var existingResult = TestResults.FirstOrDefault(r => r.VariableName == result.VariableName);
                                if (existingResult != null)
                                {
                                    existingResult.TestBatch = result.TestBatch;
                                    existingResult.ChannelTag = result.ChannelTag;
                                    existingResult.TestPLCChannelTag = result.TestPLCChannelTag;
                                }
                            }
                        }
                    }
                    else if (TestResults == null)
                    {
                        // 只有当TestResults为null时才创建新集合
                        TestResults = new ObservableCollection<ChannelMapping>();
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
