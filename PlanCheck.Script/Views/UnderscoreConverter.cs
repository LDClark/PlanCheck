using System;
using System.Globalization;
using System.Windows.Data;

namespace PlanCheck
{
    // This converter duplicates all underscores
    // because WPF doesn't display single underscores
    public class UnderscoreConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s)
                return s.Replace("_", "__");
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}