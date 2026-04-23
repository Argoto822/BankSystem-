using System;
using System.Globalization;
using System.Windows.Data;

namespace BankSystem.Converters
{
    public class BoolToStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return boolValue ? "Активен" : "Заблокирован";
            return "Неизвестно";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string strValue)
                return strValue == "Активен";
            return false;
        }
    }
}