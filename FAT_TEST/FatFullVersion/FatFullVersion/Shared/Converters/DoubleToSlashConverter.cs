using System;
using System.Globalization;
using System.Windows.Data;

namespace FatFullVersion.Shared.Converters
{
    /// <summary>
    /// 将double.NaN值转换为"/"的转换器
    /// </summary>
    public class DoubleToSlashConverter : IValueConverter
    {
        /// <summary>
        /// 将数值转换为显示值
        /// </summary>
        /// <param name="value">数值</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">参数</param>
        /// <param name="culture">区域信息</param>
        /// <returns>显示值</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double doubleValue)
            {
                if (double.IsNaN(doubleValue))
                {
                    return "/";
                }
                return doubleValue;
            }
            return value;
        }

        /// <summary>
        /// 不支持反向转换
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 