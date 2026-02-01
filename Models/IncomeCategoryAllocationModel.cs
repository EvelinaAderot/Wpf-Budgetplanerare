using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Wpf_Budgetplanerare.Models
{
    public class IncomeCategoryAllocationModel : INotifyPropertyChanged
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;

        private decimal _amount;
        public decimal Amount
        {
            get => _amount;
            set
            {
                if (_amount != value)
                {
                    _amount = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Leftover));
                }
            }
        }

        private decimal _monthlyAllocation;
        public decimal MonthlyAllocation
        {
            get => _monthlyAllocation;
            set
            {
                if (_monthlyAllocation != value)
                {
                    _monthlyAllocation = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Leftover));
                }
            }
        }

        private decimal _quarterlyAllocation;
        public decimal QuarterlyAllocation
        {
            get => _quarterlyAllocation;
            set
            {
                if (_quarterlyAllocation != value)
                {
                    _quarterlyAllocation = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Leftover));
                }
            }
        }

        private decimal _yearlyAllocation;
        public decimal YearlyAllocation
        {
            get => _yearlyAllocation;
            set
            {
                if (_yearlyAllocation != value)
                {
                    _yearlyAllocation = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Leftover));
                }
            }
        }

        public decimal Leftover =>
            Amount - (MonthlyAllocation + QuarterlyAllocation + YearlyAllocation);

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
