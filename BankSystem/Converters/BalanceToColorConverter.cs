using BankSystem.Models;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace BankSystem.Converters
{
    public class BalanceToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Account account)
            {
                if (account.AccountType == "credit")
                    return new SolidColorBrush(Colors.Red);
                else if (account.Balance > 0)
                    return new SolidColorBrush(Colors.Green);
                else if (account.Balance == 0)
                    return new SolidColorBrush(Colors.Gray);
                else
                    return new SolidColorBrush(Colors.Black);
            }

            if (value is decimal balance)
            {
                if (balance < 0)
                    return new SolidColorBrush(Colors.Red);
                else if (balance > 0)
                    return new SolidColorBrush(Colors.Green);
                else
                    return new SolidColorBrush(Colors.Gray);
            }

            return new SolidColorBrush(Colors.Black);
        }


        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}