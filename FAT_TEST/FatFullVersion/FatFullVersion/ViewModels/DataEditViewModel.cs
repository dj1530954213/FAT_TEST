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
        public DelegateCommand<TestResult> RetestCommand { get; private set; }
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
        public DelegateCommand<ChannelMapping> OpenManualTestCommand { get; private set; }

        /// <summary>
        /// 关闭AI手动测试窗口命令
        /// </summary>
        public DelegateCommand CloseAIManualTestCommand { get; private set; }

        /// <summary>
        /// 关闭DI手动测试窗口命令
        /// </summary>
        public DelegateCommand CloseDIManualTestCommand { get; private set; }

        /// <summary>
        /// 发送AI测试值命令
        /// </summary>
        public DelegateCommand SendAITestValueCommand { get; private set; }

        /// <summary>
        /// 确认AI值命令
        /// </summary>
        public DelegateCommand ConfirmAIValueCommand { get; private set; }

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
        public DelegateCommand ConfirmAIHighAlarmCommand { get; private set; }

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
        public DelegateCommand ConfirmAILowAlarmCommand { get; private set; }

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
        public DelegateCommand ConfirmAIMaintenanceCommand { get; private set; }

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
        public DelegateCommand ConfirmDICommand { get; private set; }

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
            //MoveUpCommand = new DelegateCommand<ChannelMapping>(MoveUp);
            //MoveDownCommand = new DelegateCommand<ChannelMapping>(MoveDown);
            ConfirmBatchSelectionCommand = new DelegateCommand(ConfirmBatchSelection);
            CancelBatchSelectionCommand = new DelegateCommand(CancelBatchSelection);
            //AllocateChannelsCommand = new DelegateCommand(AllocateChannelsAsync);
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

            // 初始化手动测试命令
            OpenManualTestCommand = new DelegateCommand<ChannelMapping>(ExecuteOpenManualTest);
            CloseAIManualTestCommand = new DelegateCommand(ExecuteCloseAIManualTest);
            CloseDIManualTestCommand = new DelegateCommand(ExecuteCloseDIManualTest);
            SendAITestValueCommand = new DelegateCommand(ExecuteSendAITestValue);
            ConfirmAIValueCommand = new DelegateCommand(ExecuteConfirmAIValue);
            SendAIHighAlarmCommand = new DelegateCommand(ExecuteSendAIHighAlarm);
            ResetAIHighAlarmCommand = new DelegateCommand(ExecuteResetAIHighAlarm);
            ConfirmAIHighAlarmCommand = new DelegateCommand(ExecuteConfirmAIHighAlarm);
            SendAILowAlarmCommand = new DelegateCommand(ExecuteSendAILowAlarm);
            ResetAILowAlarmCommand = new DelegateCommand(ExecuteResetAILowAlarm);
            ConfirmAILowAlarmCommand = new DelegateCommand(ExecuteConfirmAILowAlarm);
            SendAIMaintenanceCommand = new DelegateCommand(ExecuteSendAIMaintenance);
            ResetAIMaintenanceCommand = new DelegateCommand(ExecuteResetAIMaintenance);
            ConfirmAIMaintenanceCommand = new DelegateCommand(ExecuteConfirmAIMaintenance);
            SendDITestCommand = new DelegateCommand(ExecuteSendDITest);
            ResetDICommand = new DelegateCommand(ExecuteResetDI);
            ConfirmDICommand = new DelegateCommand(ExecuteConfirmDI);
        }
        /// <summary>
        /// 选择批次后将选择的批次信息放置在当前的显示区域中
        /// </summary>
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
                        //TestBatch = newBatch.BatchName,
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

                    // 创建对应的测试结果
                    var result = new TestResult
                    {
                        TestId = TestResults.Count + 1,
                        VariableName = point.VariableName,
                        PointType = point.ModuleType,
                        ValueType = point.DataType,
                        TargetPlcChannel = point.ChannelTag,
                        RangeMin = point.LowLowLimit,
                        RangeMax = point.HighHighLimit,
                        Value0Percent = point.LowLowLimit,
                        Value25Percent = point.LowLowLimit + (point.HighHighLimit - point.LowLowLimit) * 0.25,
                        Value50Percent = point.LowLowLimit + (point.HighHighLimit - point.LowLowLimit) * 0.5,
                        Value75Percent = point.LowLowLimit + (point.HighHighLimit - point.LowLowLimit) * 0.75,
                        Value100Percent = point.HighHighLimit,
                        //BatchName = newBatch.BatchName,
                        //BatchId = newBatch.BatchId,
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
                        //TestBatch = newBatch.BatchName,
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

                    // 创建对应的测试结果
                    var result = new TestResult
                    {
                        TestId = TestResults.Count + 1,
                        VariableName = point.VariableName,
                        PointType = point.ModuleType,
                        ValueType = point.DataType,
                        TargetPlcChannel = point.ChannelTag,
                        RangeMin = point.LowLowLimit,
                        RangeMax = point.HighHighLimit,
                        Value0Percent = point.LowLowLimit,
                        Value25Percent = point.LowLowLimit + (point.HighHighLimit - point.LowLowLimit) * 0.25,
                        Value50Percent = point.LowLowLimit + (point.HighHighLimit - point.LowLowLimit) * 0.5,
                        Value75Percent = point.LowLowLimit + (point.HighHighLimit - point.LowLowLimit) * 0.75,
                        Value100Percent = point.HighHighLimit,
                        //BatchName = newBatch.BatchName,
                        //BatchId = newBatch.BatchId,
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
                        //TestBatch = newBatch.BatchName,
                        VariableName = point.VariableName,
                        ModuleType = point.ModuleType
                    };
                    DIChannels.Add(channel);

                    // 创建对应的测试结果
                    var result = new TestResult
                    {
                        TestId = TestResults.Count + 1,
                        VariableName = point.VariableName,
                        PointType = point.ModuleType,
                        ValueType = point.DataType,
                        TargetPlcChannel = point.ChannelTag,
                        //BatchName = newBatch.BatchName,
                        //BatchId = newBatch.BatchId,
                        TestResultStatus = 0, // 未测试
                        ResultText = "未测试",
                        // 为DI和DO点位设置NaN值，使其在UI中显示为"/"
                        RangeMin = double.NaN,
                        RangeMax = double.NaN, 
                        Value0Percent = double.NaN,
                        Value25Percent = double.NaN,
                        Value50Percent = double.NaN,
                        Value75Percent = double.NaN,
                        Value100Percent = double.NaN
                    };
                    TestResults.Add(result);
                }

                // 添加DO通道
                foreach (var point in doPoints)
                {
                    var channel = new ChannelMapping
                    {
                        ChannelTag = point.ChannelTag,
                        //TestBatch = newBatch.BatchName,
                        VariableName = point.VariableName,
                        ModuleType = point.ModuleType
                    };
                    DOChannels.Add(channel);

                    // 创建对应的测试结果
                    var result = new TestResult
                    {
                        TestId = TestResults.Count + 1,
                        VariableName = point.VariableName,
                        PointType = point.ModuleType,
                        ValueType = point.DataType,
                        TargetPlcChannel = point.ChannelTag,
                        //BatchName = newBatch.BatchName,
                        //BatchId = newBatch.BatchId,
                        TestResultStatus = 0, // 未测试
                        ResultText = "未测试",
                        // 为DI和DO点位设置NaN值，使其在UI中显示为"/"
                        RangeMin = double.NaN,
                        RangeMax = double.NaN, 
                        Value0Percent = double.NaN,
                        Value25Percent = double.NaN,
                        Value50Percent = double.NaN,
                        Value75Percent = double.NaN,
                        Value100Percent = double.NaN
                    };
                    TestResults.Add(result);
                }
                //当Excel中点位解析完成后并且已经初始化完ChannelMapping后调用自动分配程序分配点位
                var channelsMappingResult = await _channelMappingService.AllocateChannelsTestAsync(AIChannels, AOChannels, DIChannels, DOChannels);
                //通道分配完成之后同步更新结果表位中的对应数据
                _channelMappingService.AllocateResult(channelsMappingResult.AI, channelsMappingResult.AO, channelsMappingResult.DI,
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
                            _originalDOChannels, 
                            TestResults));
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
                }

                // 根据选择的批次筛选相关的测试结果
                var batchResults = TestResults.Where(r => r.BatchName == SelectedBatch.BatchName).ToList();
                
                // 获取当前批次中的所有通道ID
                var batchChannelVariableNames = batchResults.Select(r => r.VariableName).ToHashSet();
                
                // 筛选和更新当前显示的通道
                AIChannels = new ObservableCollection<ChannelMapping>(
                    _originalAIChannels.Where(c => batchChannelVariableNames.Contains(c.VariableName)));
                
                AOChannels = new ObservableCollection<ChannelMapping>(
                    _originalAOChannels.Where(c => batchChannelVariableNames.Contains(c.VariableName)));
                
                DIChannels = new ObservableCollection<ChannelMapping>(
                    _originalDIChannels.Where(c => batchChannelVariableNames.Contains(c.VariableName)));
                
                DOChannels = new ObservableCollection<ChannelMapping>(
                    _originalDOChannels.Where(c => batchChannelVariableNames.Contains(c.VariableName)));
                
                // 更新当前显示的通道
                UpdateCurrentChannels();
                
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
                if (!string.IsNullOrEmpty(result.BatchName))
                {
                    affectedBatchNames.Add(result.BatchName);
                }
            }

            // 更新批次信息
            await UpdateBatchInfoAsync();

            RaisePropertyChanged(nameof(TestResults));
            
            // 更新点位统计数据
            UpdatePointStatistics();
        }

        private async void Retest(TestResult result)
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

                // 使用通道映射服务提取批次信息
                Batches = new ObservableCollection<BatchInfo>(
                    await _channelMappingService.ExtractBatchInfoAsync(
                        AIChannels, 
                        AOChannels, 
                        DIChannels, 
                        DOChannels, 
                        TestResults));

                // 通知UI更新
                RaisePropertyChanged(nameof(Batches));
                
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
        /// 执行打开手动测试窗口
        /// </summary>
        private void ExecuteOpenManualTest(ChannelMapping channel)
        {
            if (channel == null) return;

            SelectedChannel = channel;
            switch (channel.ModuleType)
            {
                case "AI":
                    IsAIManualTestOpen = true;
                    break;
                case "DI":
                    IsDIManualTestOpen = true;
                    break;
            }
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
        private void ExecuteConfirmAIValue()
        {
            // TODO: 实现确认AI值的逻辑
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
        private void ExecuteConfirmAIHighAlarm()
        {
            // TODO: 实现确认AI高报的逻辑
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
        private void ExecuteConfirmAILowAlarm()
        {
            // TODO: 实现确认AI低报的逻辑
        }

        /// <summary>
        /// 执行发送AI维护功能
        /// </summary>
        private void ExecuteSendAIMaintenance()
        {
            // TODO: 实现发送AI维护功能的逻辑
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
        private void ExecuteConfirmAIMaintenance()
        {
            // TODO: 实现确认AI维护功能的逻辑
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
        private void ExecuteConfirmDI()
        {
            // TODO: 实现确认DI的逻辑
        }

        /// <summary>
        /// 用新的测试数据更新或创建测试结果
        /// </summary>
        private TestResult CreateOrUpdateTestResult(ChannelMapping point, bool isSuccess, string resultText)
        {
            var testResult = new TestResult
            {
                TestId = TestResults.Count + 1,
                VariableName = point.VariableName,
                PointType = point.ModuleType,
                ValueType = GetValueTypeForModule(point.ModuleType),
                TestPlcChannel = point.TestPLCChannelTag,
                TargetPlcChannel = point.ChannelTag,
                TestResultStatus = isSuccess ? 1 : 2,
                ResultText = resultText,
                TestTime = DateTime.Now,
                Status = isSuccess ? "通过" : "失败",
                BatchName = SelectedBatch?.BatchName
            };

            // 为DI和DO点位设置特殊显示值
            if (point.ModuleType?.ToLower() == "di" || point.ModuleType?.ToLower() == "do")
            {
                // 使用字符串"/"表示不适用的数据
                testResult.RangeMin = double.NaN;
                testResult.RangeMax = double.NaN;
                testResult.Value0Percent = double.NaN;
                testResult.Value25Percent = double.NaN;
                testResult.Value50Percent = double.NaN;
                testResult.Value75Percent = double.NaN;
                testResult.Value100Percent = double.NaN;
            }

            // 如果是更新现有结果，则替换原结果
            var existingResult = TestResults.FirstOrDefault(r => r.VariableName == point.VariableName);
            if (existingResult != null)
            {
                int index = TestResults.IndexOf(existingResult);
                TestResults[index] = testResult;
            }
            else
            {
                TestResults.Add(testResult);
            }

            return testResult;
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
