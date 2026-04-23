using System;
using System.Globalization;
using System.Windows.Data;

namespace BankSystem.Converters
{
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string status = value?.ToString();
            switch (status)
            {
                case "active":
                    return "#27AE60";
                case "blocked":
                    return "#E74C3C";
                case "closed":
                    return "#95A5A6";
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