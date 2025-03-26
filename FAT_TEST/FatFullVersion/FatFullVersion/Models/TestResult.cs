using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace FatFullVersion.Models
{
    /// <summary>
    /// 测试结果类，用于存储测试的结果数据
    /// </summary>
    public class TestResult : INotifyPropertyChanged
    {
        #region 基本信息字段

        private int _testId;
        /// <summary>
        /// 测试序号
        /// </summary>
        public int TestId
        {
            get { return _testId; }
            set
            {
                if (_testId != value)
                {
                    _testId = value;
                    OnPropertyChanged(nameof(TestId));
                }
            }
        }

        private string _variableName;
        /// <summary>
        /// 变量名称
        /// </summary>
        public string VariableName
        {
            get { return _variableName; }
            set
            {
                if (_variableName != value)
                {
                    _variableName = value;
                    OnPropertyChanged(nameof(VariableName));
                }
            }
        }

        private string _pointType;
        /// <summary>
        /// 点位类型
        /// </summary>
        public string PointType
        {
            get { return _pointType; }
            set
            {
                if (_pointType != value)
                {
                    _pointType = value;
                    OnPropertyChanged(nameof(PointType));
                }
            }
        }

        private string _valueType;
        /// <summary>
        /// 数据类型
        /// </summary>
        public string ValueType
        {
            get { return _valueType; }
            set
            {
                if (_valueType != value)
                {
                    _valueType = value;
                    OnPropertyChanged(nameof(ValueType));
                }
            }
        }

        private string _testPlcChannel;
        /// <summary>
        /// 测试PLC通道标识
        /// </summary>
        public string TestPlcChannel
        {
            get { return _testPlcChannel; }
            set
            {
                if (_testPlcChannel != value)
                {
                    _testPlcChannel = value;
                    OnPropertyChanged(nameof(TestPlcChannel));
                }
            }
        }

        private string _targetPlcChannel;
        /// <summary>
        /// 被测PLC通道
        /// </summary>
        public string TargetPlcChannel
        {
            get { return _targetPlcChannel; }
            set
            {
                if (_targetPlcChannel != value)
                {
                    _targetPlcChannel = value;
                    OnPropertyChanged(nameof(TargetPlcChannel));
                }
            }
        }

        #endregion

        #region 测试状态字段

        private int _testResultStatus;
        /// <summary>
        /// 测试状态(0:未测试, 1:通过, 2:失败)
        /// </summary>
        public int TestResultStatus
        {
            get { return _testResultStatus; }
            set
            {
                if (_testResultStatus != value)
                {
                    _testResultStatus = value;
                    OnPropertyChanged(nameof(TestResultStatus));
                }
            }
        }

        private string _resultText;
        /// <summary>
        /// 测试结果信息
        /// </summary>
        public string ResultText
        {
            get { return _resultText; }
            set
            {
                if (_resultText != value)
                {
                    _resultText = value;
                    OnPropertyChanged(nameof(ResultText));
                }
            }
        }

        private DateTime? _testTime;
        /// <summary>
        /// 测试时间
        /// </summary>
        public DateTime? TestTime
        {
            get { return _testTime; }
            set
            {
                if (_testTime != value)
                {
                    _testTime = value;
                    OnPropertyChanged(nameof(TestTime));
                }
            }
        }

        private string _status;
        /// <summary>
        /// 当前测试状态（通过/失败/取消等）
        /// </summary>
        public string Status
        {
            get { return _status; }
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged(nameof(Status));
                }
            }
        }

        #endregion

        #region 测试时间与持续时间字段

        private DateTime _startTime;
        /// <summary>
        /// 测试开始时间
        /// </summary>
        public DateTime StartTime
        {
            get { return _startTime; }
            set
            {
                if (_startTime != value)
                {
                    _startTime = value;
                    OnPropertyChanged(nameof(StartTime));
                }
            }
        }

        private DateTime _endTime;
        /// <summary>
        /// 测试结束时间
        /// </summary>
        public DateTime EndTime
        {
            get { return _endTime; }
            set
            {
                if (_endTime != value)
                {
                    _endTime = value;
                    OnPropertyChanged(nameof(EndTime));
                    // 当结束时间设置时，自动计算测试持续时间
                    OnPropertyChanged(nameof(TestDuration));
                }
            }
        }

        /// <summary>
        /// 测试持续时间（秒）
        /// </summary>
        public double TestDuration
        {
            get
            {
                if (_endTime > DateTime.MinValue && _startTime > DateTime.MinValue)
                {
                    return (_endTime - _startTime).TotalSeconds;
                }
                return 0;
            }
        }

        #endregion

        #region 量程与范围字段

        private double _rangeMin;
        /// <summary>
        /// 最小量程
        /// </summary>
        public double RangeMin
        {
            get { return _rangeMin; }
            set
            {
                if (_rangeMin != value)
                {
                    _rangeMin = value;
                    OnPropertyChanged(nameof(RangeMin));
                }
            }
        }

        private double _rangeMax;
        /// <summary>
        /// 最大量程
        /// </summary>
        public double RangeMax
        {
            get { return _rangeMax; }
            set
            {
                if (_rangeMax != value)
                {
                    _rangeMax = value;
                    OnPropertyChanged(nameof(RangeMax));
                }
            }
        }

        #endregion

        #region 测试值字段

        private double _expectedValue;
        /// <summary>
        /// 期望值
        /// </summary>
        public double ExpectedValue
        {
            get { return _expectedValue; }
            set
            {
                if (_expectedValue != value)
                {
                    _expectedValue = value;
                    OnPropertyChanged(nameof(ExpectedValue));
                }
            }
        }

        private double _actualValue;
        /// <summary>
        /// 实际值
        /// </summary>
        public double ActualValue
        {
            get { return _actualValue; }
            set
            {
                if (_actualValue != value)
                {
                    _actualValue = value;
                    OnPropertyChanged(nameof(ActualValue));
                    // 当实际值设置时，自动计算偏差值
                    OnPropertyChanged(nameof(Deviation));
                    OnPropertyChanged(nameof(DeviationPercent));
                }
            }
        }

        /// <summary>
        /// 偏差值
        /// </summary>
        public double Deviation
        {
            get { return ActualValue - ExpectedValue; }
        }

        /// <summary>
        /// 偏差百分比
        /// </summary>
        public double DeviationPercent
        {
            get
            {
                if (ExpectedValue != 0)
                {
                    return (Deviation / ExpectedValue) * 100;
                }
                return 0;
            }
        }

        #endregion

        #region 百分比测试点位值

        private double _value0Percent;
        /// <summary>
        /// 0%对比值
        /// </summary>
        public double Value0Percent
        {
            get { return _value0Percent; }
            set
            {
                if (_value0Percent != value)
                {
                    _value0Percent = value;
                    OnPropertyChanged(nameof(Value0Percent));
                }
            }
        }

        private double _value25Percent;
        /// <summary>
        /// 25%对比值
        /// </summary>
        public double Value25Percent
        {
            get { return _value25Percent; }
            set
            {
                if (_value25Percent != value)
                {
                    _value25Percent = value;
                    OnPropertyChanged(nameof(Value25Percent));
                }
            }
        }

        private double _value50Percent;
        /// <summary>
        /// 50%对比值
        /// </summary>
        public double Value50Percent
        {
            get { return _value50Percent; }
            set
            {
                if (_value50Percent != value)
                {
                    _value50Percent = value;
                    OnPropertyChanged(nameof(Value50Percent));
                }
            }
        }

        private double _value75Percent;
        /// <summary>
        /// 75%对比值
        /// </summary>
        public double Value75Percent
        {
            get { return _value75Percent; }
            set
            {
                if (_value75Percent != value)
                {
                    _value75Percent = value;
                    OnPropertyChanged(nameof(Value75Percent));
                }
            }
        }

        private double _value100Percent;
        /// <summary>
        /// 100%对比值
        /// </summary>
        public double Value100Percent
        {
            get { return _value100Percent; }
            set
            {
                if (_value100Percent != value)
                {
                    _value100Percent = value;
                    OnPropertyChanged(nameof(Value100Percent));
                }
            }
        }

        #endregion

        #region 报警状态字段

        private string _lowLowAlarmStatus;
        /// <summary>
        /// 低低报状态
        /// </summary>
        public string LowLowAlarmStatus
        {
            get { return _lowLowAlarmStatus; }
            set
            {
                if (_lowLowAlarmStatus != value)
                {
                    _lowLowAlarmStatus = value;
                    OnPropertyChanged(nameof(LowLowAlarmStatus));
                }
            }
        }

        private string _lowAlarmStatus;
        /// <summary>
        /// 低报状态
        /// </summary>
        public string LowAlarmStatus
        {
            get { return _lowAlarmStatus; }
            set
            {
                if (_lowAlarmStatus != value)
                {
                    _lowAlarmStatus = value;
                    OnPropertyChanged(nameof(LowAlarmStatus));
                }
            }
        }

        private string _highAlarmStatus;
        /// <summary>
        /// 高报状态
        /// </summary>
        public string HighAlarmStatus
        {
            get { return _highAlarmStatus; }
            set
            {
                if (_highAlarmStatus != value)
                {
                    _highAlarmStatus = value;
                    OnPropertyChanged(nameof(HighAlarmStatus));
                }
            }
        }

        private string _highHighAlarmStatus;
        /// <summary>
        /// 高高报状态
        /// </summary>
        public string HighHighAlarmStatus
        {
            get { return _highHighAlarmStatus; }
            set
            {
                if (_highHighAlarmStatus != value)
                {
                    _highHighAlarmStatus = value;
                    OnPropertyChanged(nameof(HighHighAlarmStatus));
                }
            }
        }

        private string _maintenanceFunction;
        /// <summary>
        /// 维护功能结果
        /// </summary>
        public string MaintenanceFunction
        {
            get { return _maintenanceFunction; }
            set
            {
                if (_maintenanceFunction != value)
                {
                    _maintenanceFunction = value;
                    OnPropertyChanged(nameof(MaintenanceFunction));
                }
            }
        }

        #endregion

        #region 其他字段

        private string _errorMessage;
        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage
        {
            get { return _errorMessage; }
            set
            {
                if (_errorMessage != value)
                {
                    _errorMessage = value;
                    OnPropertyChanged(nameof(ErrorMessage));
                }
            }
        }

        private string _batchName;
        /// <summary>
        /// 测试批次名称
        /// </summary>
        public string BatchName
        {
            get { return _batchName; }
            set
            {
                if (_batchName != value)
                {
                    _batchName = value;
                    OnPropertyChanged(nameof(BatchName));
                }
            }
        }

        #endregion

        #region INotifyPropertyChanged 实现

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
} 