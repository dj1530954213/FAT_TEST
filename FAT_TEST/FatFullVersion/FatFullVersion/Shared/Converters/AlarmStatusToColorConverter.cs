using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace FatFullVersion.Shared.Converters
{
    /// <summary>
    /// 报警状态转换为颜色转换器
    /// </summary>
    public class AlarmStatusToColorConverter : IValueConverter
    {
        /// <summary>
        /// 将报警状态转换为对应的颜色
        /// </summary>
        /// <param name="value">状态值</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">转换参数</param>
        /// <param name="culture">文化信息</param>
        /// <returns>对应的颜色</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 输出调试信息，帮助诊断问题
            System.Diagnostics.Debug.WriteLine($"AlarmStatusToColorConverter: value={value}");

            if (value == null)
            {
                return new SolidColorBrush(Color.FromRgb(0x44, 0x72, 0xC4)); // 默认蓝色
            }

            string status = value.ToString();
            
            switch (status)
            {
                case "已通过":
                    return new SolidColorBrush(Colors.Green);
                case "未通过":
                    return new SolidColorBrush(Colors.Red);
                case "测试中":
                case "正在检测":
                    return new SolidColorBrush(Colors.Orange);
                case "未测试":
                    return new SolidColorBrush(Colors.Gray);
                default:
                    return new SolidColorBrush(Color.FromRgb(0x44, 0x72, 0xC4)); // 默认蓝色 #4472C4
            }
        }

        /// <summary>
        /// 反向转换（不支持）
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 