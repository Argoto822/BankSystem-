using System;

namespace BankSystem.Models
{
    public class Account
    {
        public int Id { get; set; }
        public string AccountNumber { get; set; }
        public string AccountType { get; set; } // current, saving, credit
        public string AccountName { get; set; }
        public int ClientId { get; set; }
        public decimal Balance { get; set; }
        public string Currency { get; set; }
        public string Status { get; set; }
        public string StatusCode { get; set; }
        public DateTime OpeningDate { get; set; }

        // Свойство для отображения типа счета на русском
        public string AccountTypeName
        {
            get
            {
                if (AccountType == "current")
                {
                    return "Текущий счет";
                }
                else if (AccountType == "saving")
                {
                    return "Сберегательный счет";
                }
                else if (AccountType == "credit")
                {
                    return "Кредитный счет";
                }
                else
                {
                    return AccountType;
                }
            }
        }

        // Свойство для отображения в ComboBox
        public string DisplayName
        {
            get
            {
                return AccountNumber + " (" + AccountTypeName + ")";
            }
        }

        // Свойство для отображения баланса с учетом знака
        public string FormattedBalance
        {
            get
            {
                if (AccountType == "credit")
                {
                    return "- " + Math.Abs(Balance).ToString("N2") + " ₽";
                }
                else
                {
                    return Balance.ToString("N2") + " ₽";
                }
            }
        }

        // Свойство для отображения абсолютного значения баланса
        public decimal AbsoluteBalance
        {
            get
            {
                return Math.Abs(Balance);
            }
        }

        // Цвет баланса в зависимости от типа счета
        public string BalanceColor
        {
            get
            {
                if (AccountType == "credit")
                {
                    return "Red";
                }
                else if (Balance > 0)
                {
                    return "Green";
                }
                else if (Balance == 0)
                {
                    return "Gray";
                }
                else
                {
                    return "Black";
                }
            }
        }

        // Статус на русском
        public string StatusName
        {
            get
            {
                if (StatusCode == "active")
                {
                    return "Активен";
                }
                else if (StatusCode == "closed")
                {
                    return "Закрыт";
                }
                else if (StatusCode == "blocked")
                {
                    return "Заблокирован";
                }
                else
                {
                    return Status;
                }
            }
        }

        // Проверка, является ли счет кредитным
        public bool IsCreditAccount
        {
            get
            {
                return AccountType == "credit";
            }
        }

        // Проверка, является ли счет активным
        public bool IsActive
        {
            get
            {
                return StatusCode == "active";
            }
        }

        // Сумма задолженности по кредиту (только для кредитных счетов)
        public decimal CreditDebt
        {
            get
            {
                if (AccountType == "credit" && Balance < 0)
                {
                    return Math.Abs(Balance);
                }
                return 0;
            }
        }
    }
}