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

        private string _status;
        /// <summary>
        /// 测试状态（通过/失败/取消等）
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

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 