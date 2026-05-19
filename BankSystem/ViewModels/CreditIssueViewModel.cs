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
    public class CreditIssueViewModel : INotifyPropertyChanged
    {
        private int _clientId;
        private string _clientFullName;
        private decimal _amount = 100000;
        private decimal _interestRate = 15;
        private int _termMonths = 12;
        private string _paymentType = "Аннуитетный";
        private string _purpose;
        private decimal _monthlyPayment;
        private decimal _totalPayments;
        private decimal _totalInterest;
        private ObservableCollection<PaymentSchedule> _paymentSchedulePreview;

        public event PropertyChangedEventHandler PropertyChanged;

        public int ClientId
        {
            get => _clientId;
            set { _clientId = value; OnPropertyChanged(); }
        }

        public string ClientFullName
        {
            get => _clientFullName;
            set { _clientFullName = value; OnPropertyChanged(); }
        }

        public decimal Amount
        {
            get => _amount;
            set
            {
                if (value > 0 && value <= 10000000)
                {
                    _amount = value;
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
                if (value > 0 && value <= 50)
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
                if (value > 0 && value <= 360)
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
                _paymentType = value;
                OnPropertyChanged();
                Calculate();
            }
        }

        public string Purpose
        {
            get => _purpose;
            set { _purpose = value; OnPropertyChanged(); }
        }

        public decimal MonthlyPayment
        {
            get => _monthlyPayment;
            set { _monthlyPayment = value; OnPropertyChanged(); }
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

        public ObservableCollection<PaymentSchedule> PaymentSchedulePreview
        {
            get => _paymentSchedulePreview;
            set { _paymentSchedulePreview = value; OnPropertyChanged(); }
        }

        public List<string> PaymentTypes { get; } = new List<string> { "Аннуитетный", "Дифференцированный" };

        public CreditIssueViewModel()
        {
            Calculate();
        }

        private void Calculate()
        {
            try
            {
                if (Amount <= 0 || InterestRate <= 0 || TermMonths <= 0) return;

                List<PaymentSchedule> schedule;
                DateTime startDate = DateTime.Now.AddMonths(1);

                if (PaymentType == "Аннуитетный")
                {
                    MonthlyPayment = CreditCalculator.CalculateAnnuityPayment(Amount, InterestRate, TermMonths);
                    schedule = CreditCalculator.GenerateAnnuitySchedule(0, Amount, InterestRate, TermMonths, startDate);
                }
                else
                {
                    schedule = CreditCalculator.GenerateDifferentiatedSchedule(0, Amount, InterestRate, TermMonths, startDate);
                    MonthlyPayment = schedule.FirstOrDefault()?.PaymentAmount ?? 0;
                }

                TotalPayments = CreditCalculator.CalculateTotalPayments(schedule);
                TotalInterest = CreditCalculator.CalculateTotalInterest(schedule);
                PaymentSchedulePreview = new ObservableCollection<PaymentSchedule>(schedule.Take(6));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка расчета: {ex.Message}");
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}