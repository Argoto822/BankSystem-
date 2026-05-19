using BankSystem.Models;
using System;
using System.Collections.Generic;

namespace BankSystem.Services
{
    public class CreditCalculator
    {
        /// <summary>
        /// Расчет аннуитетного платежа
        /// </summary>
        public static decimal CalculateAnnuityPayment(decimal amount, decimal annualRate, int months)
        {
            decimal monthlyRate = annualRate / 12 / 100;
            if (monthlyRate == 0) return amount / months;

            double factor = Math.Pow(1 + (double)monthlyRate, months);
            double payment = (double)amount * (double)monthlyRate * factor / (factor - 1);

            return (decimal)payment;
        }

        /// <summary>
        /// Расчет дифференцированного платежа
        /// </summary>
        public static decimal CalculateDifferentiatedPayment(decimal amount, decimal annualRate, int months, int currentMonth)
        {
            decimal monthlyRate = annualRate / 12 / 100;
            decimal principalPart = amount / months;
            decimal remainingPrincipal = amount - principalPart * (currentMonth - 1);
            decimal interestPart = remainingPrincipal * monthlyRate;

            return principalPart + interestPart;
        }

        /// <summary>
        /// Генерация графика платежей (аннуитетный)
        /// </summary>
        public static List<PaymentSchedule> GenerateAnnuitySchedule(
            int creditId,
            decimal amount,
            decimal annualRate,
            int months,
            DateTime startDate)
        {
            var schedule = new List<PaymentSchedule>();
            decimal monthlyPayment = CalculateAnnuityPayment(amount, annualRate, months);
            decimal monthlyRate = annualRate / 12 / 100;
            decimal remainingDebt = amount;

            for (int i = 1; i <= months; i++)
            {
                decimal interestAmount = remainingDebt * monthlyRate;
                decimal principalAmount = monthlyPayment - interestAmount;

                if (i == months) // Последний платеж корректируем
                {
                    principalAmount = remainingDebt;
                    interestAmount = monthlyPayment - principalAmount;
                }

                remainingDebt -= principalAmount;

                schedule.Add(new PaymentSchedule
                {
                    CreditId = creditId,
                    PaymentNumber = i,
                    PaymentDate = startDate.AddMonths(i),
                    PaymentAmount = Math.Round(monthlyPayment, 2),
                    PrincipalAmount = Math.Round(principalAmount, 2),
                    InterestAmount = Math.Round(interestAmount, 2),
                    RemainingDebt = Math.Round(remainingDebt, 2),
                    IsPaid = false
                });
            }

            return schedule;
        }

        /// <summary>
        /// Генерация графика платежей (дифференцированный)
        /// </summary>
        public static List<PaymentSchedule> GenerateDifferentiatedSchedule(
            int creditId,
            decimal amount,
            decimal annualRate,
            int months,
            DateTime startDate)
        {
            var schedule = new List<PaymentSchedule>();
            decimal monthlyRate = annualRate / 12 / 100;
            decimal principalPart = amount / months;
            decimal remainingDebt = amount;

            for (int i = 1; i <= months; i++)
            {
                decimal interestAmount = remainingDebt * monthlyRate;
                decimal paymentAmount = principalPart + interestAmount;

                remainingDebt -= principalPart;

                schedule.Add(new PaymentSchedule
                {
                    CreditId = creditId,
                    PaymentNumber = i,
                    PaymentDate = startDate.AddMonths(i),
                    PaymentAmount = Math.Round(paymentAmount, 2),
                    PrincipalAmount = Math.Round(principalPart, 2),
                    InterestAmount = Math.Round(interestAmount, 2),
                    RemainingDebt = Math.Round(Math.Max(0, remainingDebt), 2),
                    IsPaid = false
                });
            }

            return schedule;
        }

        /// <summary>
        /// Общая сумма выплат
        /// </summary>
        public static decimal CalculateTotalPayments(List<PaymentSchedule> schedule)
        {
            decimal total = 0;
            foreach (var payment in schedule)
            {
                total += payment.PaymentAmount;
            }
            return total;
        }

        /// <summary>
        /// Общая сумма процентов
        /// </summary>
        public static decimal CalculateTotalInterest(List<PaymentSchedule> schedule)
        {
            decimal total = 0;
            foreach (var payment in schedule)
            {
                total += payment.InterestAmount;
            }
            return total;
        }
    }
}