using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace FatFullVersion.Shared.Converters
{
    // 测试结果状态到背景色的转换器
    public class TestResultStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int status)
            {
                return status switch
                {
                    0 => new SolidColorBrush(Colors.White), // 未测试
                    1 => new SolidColorBrush(Colors.LightGreen), // 通过
                    2 => new SolidColorBrush(Colors.LightPink), // 失败
                    _ => new SolidColorBrush(Colors.White)
                };
            }
            return new SolidColorBrush(Colors.White);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}