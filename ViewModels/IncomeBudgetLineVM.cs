using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wpf_Budgetplanerare.ViewModels.Base;

namespace Wpf_Budgetplanerare.ViewModels
{
    public class IncomeBudgetLineVM : ViewModelBase
    {
        public int CategoryId { get; }
        public string Name { get; }

        private decimal _budgetMonthly;
        public decimal BudgetMonthly
        {
            get => _budgetMonthly;
            set => SetProperty(ref _budgetMonthly, value);
        }

        private decimal _fromAmount;
        public decimal FromAmount
        {
            get => _fromAmount;
            set => SetProperty(ref _fromAmount, value);
        }

        private decimal _toAmount;
        public decimal ToAmount
        {
            get => _toAmount;
            set => SetProperty(ref _toAmount, value);
        }

        public IncomeBudgetLineVM(int categoryId, string name, decimal budgetMonthly)
        {
            CategoryId = categoryId;
            Name = name;
            _budgetMonthly = budgetMonthly;
        }
    }
}

