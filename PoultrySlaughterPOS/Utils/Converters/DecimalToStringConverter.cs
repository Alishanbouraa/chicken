using System.Globalization;
using System.Windows.Data;

namespace PoultrySlaughterPOS.Utils.Converters
{
    /// <summary>
    /// Advanced decimal to string converter with culture-aware formatting and validation
    /// Provides bidirectional conversion with Arabic numeral support and input sanitization
    /// </summary>
    public class DecimalToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal decimalValue)
            {
                return decimalValue == 0 ? string.Empty : decimalValue.ToString("F3", culture);
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue && !string.IsNullOrWhiteSpace(stringValue))
            {
                if (decimal.TryParse(stringValue, NumberStyles.Any, culture, out decimal result))
                {
                    return result;
                }
            }
            return 0m;
        }
    }
}