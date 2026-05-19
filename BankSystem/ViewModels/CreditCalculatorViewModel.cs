using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using BankSystem.Models;
using BankSystem.Services;

namespace BankSystem.ViewModels
{
    public class CreditCalculatorViewModel : INotifyPropertyChanged
    {
        private decimal _creditAmount;
        private decimal _interestRate;
        private int _termMonths;
        private string _paymentType = "Аннуитетный";
        private ObservableCollection<PaymentSchedule> _paymentSchedule;
        private PaymentSchedule _selectedPayment;
        private decimal _totalPayments;
        private decimal _totalInterest;
        private decimal _monthlyPayment;
        private string _statusMessage;
        private decimal _effectiveRate;
        private int _paidCount;
        private int _totalPaymentsCount;
        private decimal _remainingTotal;

        public event PropertyChangedEventHandler PropertyChanged;

        public decimal CreditAmount
        {
            get => _creditAmount;
            set
            {
                if (_creditAmount != value)
                {
                    _creditAmount = value;
                    OnPropertyChanged();
                    Calculate();
                }
            }
        }

        public decimal InterestRate
        {
            get => _interestRate;
            set
            {
                if (_interestRate != value)
                {
                    _interestRate = value;
                    OnPropertyChanged();
                    Calculate();
                }
            }
        }

        public int TermMonths
        {
            get => _termMonths;
            set
            {
                if (_termMonths != value)
                {
                    _termMonths = value;
                    OnPropertyChanged();
                    Calculate();
                }
            }
        }

        public string PaymentType
        {
            get => _paymentType;
            set
            {
                if (_paymentType != value)
                {
                    _paymentType = value;
                    OnPropertyChanged();
                    Calculate();
                }
            }
        }

        public ObservableCollection<PaymentSchedule> PaymentSchedule
        {
            get => _paymentSchedule;
            set
            {
                _paymentSchedule = value;
                OnPropertyChanged();
                UpdateStatistics();
            }
        }

        public PaymentSchedule SelectedPayment
        {
            get => _selectedPayment;
            set
            {
                _selectedPayment = value;
                OnPropertyChanged();
                if (_selectedPayment != null)
                {
                    StatusMessage = $"Выбран платеж №{_selectedPayment.PaymentNumber} от {_selectedPayment.PaymentDate:dd.MM.yyyy} на сумму {_selectedPayment.PaymentAmount:N2} ₽";
                }
            }
        }

        public decimal TotalPayments
        {
            get => _totalPayments;
            set { _totalPayments = value; OnPropertyChanged(); }
        }

        public decimal TotalInterest
        {
            get => _totalInterest;
            set { _totalInterest = value; OnPropertyChanged(); }
        }

        public decimal MonthlyPayment
        {
            get => _monthlyPayment;
            set { _monthlyPayment = value; OnPropertyChanged(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public decimal EffectiveRate
        {
            get => _effectiveRate;
            set { _effectiveRate = value; OnPropertyChanged(); }
        }

        public int PaidCount
        {
            get => _paidCount;
            set { _paidCount = value; OnPropertyChanged(); }
        }

        public int TotalPaymentsCount
        {
            get => _totalPaymentsCount;
            set { _totalPaymentsCount = value; OnPropertyChanged(); }
        }

        public decimal RemainingTotal
        {
            get => _remainingTotal;
            set { _remainingTotal = value; OnPropertyChanged(); }
        }

        public List<string> PaymentTypes { get; } = new List<string> { "Аннуитетный", "Дифференцированный" };

        public CreditCalculatorViewModel()
        {
            CreditAmount = 100000;
            InterestRate = 15;
            TermMonths = 12;
            Calculate();
        }

        public void Calculate()
        {
            try
            {
                if (CreditAmount <= 0 || InterestRate <= 0 || TermMonths <= 0)
                {
                    StatusMessage = "Пожалуйста, введите корректные значения";
                    PaymentSchedule = new ObservableCollection<PaymentSchedule>();
                    TotalPayments = 0;
                    TotalInterest = 0;
                    MonthlyPayment = 0;
                    EffectiveRate = 0;
                    return;
                }

                List<PaymentSchedule> schedule;
                DateTime startDate = DateTime.Now.AddMonths(1);

                if (PaymentType == "Аннуитетный")
                {
                    MonthlyPayment = CreditCalculator.CalculateAnnuityPayment(CreditAmount, InterestRate, TermMonths);
                    schedule = CreditCalculator.GenerateAnnuitySchedule(0, CreditAmount, InterestRate, TermMonths, startDate);
                }
                else
                {
                    schedule = CreditCalculator.GenerateDifferentiatedSchedule(0, CreditAmount, InterestRate, TermMonths, startDate);
                    MonthlyPayment = schedule.FirstOrDefault()?.PaymentAmount ?? 0;
                }

                PaymentSchedule = new ObservableCollection<PaymentSchedule>(schedule);
                TotalPayments = CreditCalculator.CalculateTotalPayments(schedule);
                TotalInterest = CreditCalculator.CalculateTotalInterest(schedule);
                CalculateEffectiveRate();

                StatusMessage = $"Расчет выполнен. Ежемесячный платеж: {MonthlyPayment:N2} ₽";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка расчета: {ex.Message}";
                PaymentSchedule = new ObservableCollection<PaymentSchedule>();
            }
        }

        private void CalculateEffectiveRate()
        {
            if (CreditAmount > 0 && TotalInterest > 0 && TermMonths > 0)
            {
                EffectiveRate = (TotalInterest / CreditAmount) * 100 / (TermMonths / 12m);
                EffectiveRate = Math.Round(EffectiveRate, 2);
            }
            else
            {
                EffectiveRate = 0;
            }
        }

        private void UpdateStatistics()
        {
            if (PaymentSchedule != null && PaymentSchedule.Any())
            {
                TotalPaymentsCount = PaymentSchedule.Count;
                PaidCount = PaymentSchedule.Count(p => p.IsPaid);
                var lastPayment = PaymentSchedule.LastOrDefault();
                RemainingTotal = lastPayment?.RemainingDebt ?? 0;
            }
            else
            {
                TotalPaymentsCount = 0;
                PaidCount = 0;
                RemainingTotal = 0;
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}