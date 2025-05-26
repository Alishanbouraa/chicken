using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PoultrySlaughterPOS.Utils.Converters
{
    /// <summary>
    /// Enhanced boolean to visibility converter with inverse support and null handling
    /// Provides robust UI state management for WPF applications
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public bool Inverse { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolValue = false;

            if (value is bool b)
                boolValue = b;
            else if (value is bool?)
                boolValue = ((bool?)value).GetValueOrDefault();

            if (Inverse)
                boolValue = !boolValue;

            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                bool result = visibility == Visibility.Visible;
                return Inverse ? !result : result;
            }
            return false;
        }
    }
}