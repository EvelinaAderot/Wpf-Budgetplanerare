using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Wpf_Budgetplanerare.Data;
using Wpf_Budgetplanerare.Data.Repositories.Interfaces;
using Wpf_Budgetplanerare.Models;
using Wpf_Budgetplanerare.ViewModels.Base;

namespace Wpf_Budgetplanerare.ViewModels
{
    public class BudgetDashboardViewModel : ViewModelBase
    {
        private readonly BudgetDbContext _db;
        private readonly IUserRepository _userRepository;

        private User? _activeUser;
        public User? ActiveUser
        {
            get => _activeUser;
            private set
            {
                if (SetProperty(ref _activeUser, value))
                {
                    OnPropertyChanged(nameof(ActiveUserFullName));
                    OnPropertyChanged(nameof(Balance));
                }
            }
        }

        private decimal _balance;
        public decimal Balance
        {
            get => _balance;
            private set => SetProperty(ref _balance, value);
        }

        private decimal _spentThisMonth;
        public decimal SpentThisMonth
        {
            get => _spentThisMonth;
            private set => SetProperty(ref _spentThisMonth, value);
        }

        // Optional breakdown (keep if you use them elsewhere)
        private decimal _incomeThisMonth;
        public decimal IncomeThisMonth
        {
            get => _incomeThisMonth;
            private set => SetProperty(ref _incomeThisMonth, value);
        }

        private decimal _expensesThisMonth;
        public decimal ExpensesThisMonth
        {
            get => _expensesThisMonth;
            private set => SetProperty(ref _expensesThisMonth, value);
        }

        private decimal _savingsThisMonth;
        public decimal SavingsThisMonth
        {
            get => _savingsThisMonth;
            private set => SetProperty(ref _savingsThisMonth, value);
        }

        public string ActiveUserFullName =>
            ActiveUser == null ? "" : $"{ActiveUser.FirstName} {ActiveUser.LastName}";

        public string TodayWithWeek => $"{DateTime.Now:yyyy-MM-dd} (v.{CurrentWeek})";

        public int CurrentWeek
        {
            get
            {
                var culture = CultureInfo.GetCultureInfo("sv-SE");
                var calendar = culture.Calendar;
                var rule = culture.DateTimeFormat.CalendarWeekRule;
                var firstDay = culture.DateTimeFormat.FirstDayOfWeek;
                return calendar.GetWeekOfYear(DateTime.Now, rule, firstDay);
            }
        }

        public BudgetDashboardViewModel(BudgetDbContext db, IUserRepository userRepository)
        {
            _db = db;
            _userRepository = userRepository;

            _ = ReloadAsync();
        }

        public async Task ReloadAsync()
        {
            ActiveUser = await _userRepository.GetActiveAsync();

            if (ActiveUser == null)
            {
                Balance = 0m;
                SpentThisMonth = 0m;
                IncomeThisMonth = 0m;
                ExpensesThisMonth = 0m;
                SavingsThisMonth = 0m;

                OnPropertyChanged(nameof(TodayWithWeek));
                OnPropertyChanged(nameof(CurrentWeek));
                return;
            }

            var now = DateTime.Now;
            var monthStart = new DateTime(now.Year, now.Month, 1);
            var nextMonthStart = monthStart.AddMonths(1);

            // ===== ALL-TIME totals for real account balance (Items only) =====
            var totalIncome = await _db.Items
                .Where(i => i.UserId == ActiveUser.Id && i.ItemType == ItemType.Income)
                .SumAsync(i => (decimal?)i.Amount) ?? 0m;

            var totalExpenses = await _db.Items
                .Where(i => i.UserId == ActiveUser.Id && i.ItemType == ItemType.Expense)
                .SumAsync(i => (decimal?)i.Amount) ?? 0m;

            var totalSavings = await _db.Items
                .Where(i => i.UserId == ActiveUser.Id && i.ItemType == ItemType.Savings)
                .SumAsync(i => (decimal?)i.Amount) ?? 0m;

            // ✅ Balance is ONLY based on Items (budgets do NOT affect balance)
            Balance = totalIncome - totalExpenses - totalSavings;

            // ===== This month breakdown =====
            IncomeThisMonth = await _db.Items
                .Where(i => i.UserId == ActiveUser.Id
                            && i.TransactionDate >= monthStart
                            && i.TransactionDate < nextMonthStart
                            && i.ItemType == ItemType.Income)
                .SumAsync(i => (decimal?)i.Amount) ?? 0m;

            ExpensesThisMonth = await _db.Items
                .Where(i => i.UserId == ActiveUser.Id
                            && i.TransactionDate >= monthStart
                            && i.TransactionDate < nextMonthStart
                            && i.ItemType == ItemType.Expense)
                .SumAsync(i => (decimal?)i.Amount) ?? 0m;

            SavingsThisMonth = await _db.Items
                .Where(i => i.UserId == ActiveUser.Id
                            && i.TransactionDate >= monthStart
                            && i.TransactionDate < nextMonthStart
                            && i.ItemType == ItemType.Savings)
                .SumAsync(i => (decimal?)i.Amount) ?? 0m;

            // ✅ Spent this month = expenses only
            SpentThisMonth = ExpensesThisMonth;

            OnPropertyChanged(nameof(ActiveUserFullName));
            OnPropertyChanged(nameof(TodayWithWeek));
            OnPropertyChanged(nameof(CurrentWeek));
        }
    }
}
