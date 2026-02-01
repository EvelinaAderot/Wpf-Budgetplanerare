using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Wpf_Budgetplanerare.Data;
using Wpf_Budgetplanerare.Models;
using Wpf_Budgetplanerare.ViewModels.Base;

namespace Wpf_Budgetplanerare.ViewModels
{
    public class SavingsSummaryViewModel : ViewModelBase
    {
        private readonly BudgetDbContext _db;
        private readonly int _userId;

        private decimal _savingsLastMonth;
        public decimal SavingsLastMonth
        {
            get => _savingsLastMonth;
            private set => SetProperty(ref _savingsLastMonth, value);
        }

        private decimal _savingsLastQuarter;
        public decimal SavingsLastQuarter
        {
            get => _savingsLastQuarter;
            private set => SetProperty(ref _savingsLastQuarter, value);
        }

        private decimal _savingsLastYear;
        public decimal SavingsLastYear
        {
            get => _savingsLastYear;
            private set => SetProperty(ref _savingsLastYear, value);
        }

        public SavingsSummaryViewModel(BudgetDbContext db, int userId)
        {
            _db = db;
            _userId = userId;

            _ = ReloadAsync();
        }

        public async Task ReloadAsync()
        {
            var today = DateTime.Today;

            var thisMonthStart = new DateTime(today.Year, today.Month, 1);
            var lastMonthStart = thisMonthStart.AddMonths(-1);
            var lastMonthEnd = thisMonthStart;

            var thisQuarterStart = GetQuarterStart(thisMonthStart);
            var lastQuarterStart = thisQuarterStart.AddMonths(-3);
            var lastQuarterEnd = thisQuarterStart;

            var thisYearStart = new DateTime(today.Year, 1, 1);
            var lastYearStart = thisYearStart.AddYears(-1);
            var lastYearEnd = thisYearStart;

            SavingsLastMonth = await SumSavingsAsync(lastMonthStart, lastMonthEnd);
            SavingsLastQuarter = await SumSavingsAsync(lastQuarterStart, lastQuarterEnd);
            SavingsLastYear = await SumSavingsAsync(lastYearStart, lastYearEnd);
        }

        private async Task<decimal> SumSavingsAsync(DateTime startInclusive, DateTime endExclusive)
        {
            return await _db.Items
                .AsNoTracking()
                .Where(i => i.UserId == _userId
                            && i.ItemType == ItemType.Savings
                            && i.TransactionDate >= startInclusive
                            && i.TransactionDate < endExclusive)
                .SumAsync(i => (decimal?)i.Amount) ?? 0m;
        }

        private static DateTime GetQuarterStart(DateTime monthStart)
        {
            var m = ((monthStart.Month - 1) / 3) * 3 + 1; 
            return new DateTime(monthStart.Year, m, 1);
        }
    }
}
