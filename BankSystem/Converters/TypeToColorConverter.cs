using System;
using System.Globalization;
using System.Windows.Data;

namespace BankSystem.Converters
{
    public class TypeToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string type = value?.ToString();
            switch (type)
            {
                case "current":
                    return "#3498DB";
                case "saving":
                    return "#F39C12";
                case "card":
                    return "#27AE60";
                default:
                    return "#2C3E50";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}