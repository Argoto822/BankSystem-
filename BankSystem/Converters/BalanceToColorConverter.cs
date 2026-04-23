using System;
using System.Globalization;
using System.Windows.Data;

namespace BankSystem.Converters
{
    public class BalanceToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal balance)
            {
                if (balance > 100000)
                    return "#27AE60";
                if (balance > 50000)
                    return "#3498DB";
                if (balance > 10000)
                    return "#F39C12";
                if (balance > 0)
                    return "#7F8C8D";
                return "#E74C3C";
            }
            return "#2C3E50";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}