using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace BankSystem.Validation
{
    public static class ValidationHelper
    {
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            try
            {
                var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                return regex.IsMatch(email);
            }
            catch { return false; }
        }

        public static bool IsValidPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return false;
            var regex = new Regex(@"^\+?[0-9\s\-\(\)]{10,20}$");
            return regex.IsMatch(phone);
        }

        public static bool IsValidPassport(string passport)
        {
            if (string.IsNullOrWhiteSpace(passport)) return false;
            var regex = new Regex(@"^\d{4}\s?\d{6}$");
            return regex.IsMatch(passport);
        }

        public static bool IsValidInn(string inn)
        {
            if (string.IsNullOrWhiteSpace(inn)) return false;
            var regex = new Regex(@"^\d{10}$|^\d{12}$");
            return regex.IsMatch(inn);
        }

        public static bool IsValidAmount(string amount, out decimal value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(amount)) return false;
            if (!decimal.TryParse(amount, out value)) return false;
            return value > 0;
        }

        // ИСПРАВЛЕНО: using declaration заменен на традиционный блок using
        public static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        public static bool IsStrongPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 6) return false;
            var hasUpper = Regex.IsMatch(password, @"[A-Z]");
            var hasLower = Regex.IsMatch(password, @"[a-z]");
            var hasDigit = Regex.IsMatch(password, @"\d");
            return hasUpper && hasLower && hasDigit;
        }
    }
}