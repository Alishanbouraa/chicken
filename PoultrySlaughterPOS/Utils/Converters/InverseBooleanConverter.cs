using System.Globalization;
using System.Windows.Data;

namespace PoultrySlaughterPOS.Utils.Converters
{
    /// <summary>
    /// Inverse boolean converter for advanced UI state management
    /// Provides clean inversion logic for binding scenarios
    /// </summary>
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return !boolValue;
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return !boolValue;
            return false;
        }
    }
}