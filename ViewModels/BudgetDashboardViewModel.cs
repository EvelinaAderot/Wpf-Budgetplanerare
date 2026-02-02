// ff — BudgetDashboardViewModel.cs
using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Wpf_Budgetplanerare.Data;
using Wpf_Budgetplanerare.Data.Repositories.Interfaces;
using Wpf_Budgetplanerare.Models;
using Wpf_Budgetplanerare.ViewModels.Base;

namespace Wpf_Budgetplanerare.ViewModels
{
    public enum DashboardPeriodKind
    {
        Monthly,
        Quarterly,
        Yearly
    }

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

        // =========================
        // TOP BAR VALUES
        // =========================

        // Balance = (income ever) - (expenses ever)
        private decimal _balance;
        public decimal Balance
        {
            get => _balance;
            private set => SetProperty(ref _balance, value);
        }

        // Period shown in the top bar (Monthly/Quarterly/Yearly)
        private DashboardPeriodKind _activePeriodKind = DashboardPeriodKind.Monthly;
        public DashboardPeriodKind ActivePeriodKind
        {
            get => _activePeriodKind;
            set
            {
                if (SetProperty(ref _activePeriodKind, value))
                {
                    OnPropertyChanged(nameof(PeriodLabel));
                    // Optionally auto-refresh when section changes
                    _ = ReloadTotalsAsync();
                }
            }
        }

        public string PeriodLabel => ActivePeriodKind switch
        {
            DashboardPeriodKind.Monthly => "This month",
            DashboardPeriodKind.Quarterly => "This quarter",
            DashboardPeriodKind.Yearly => "This year",
            _ => "This month"
        };

        // Period totals for the selected kind
        private decimal _periodIncome;
        public decimal PeriodIncome
        {
            get => _periodIncome;
            private set => SetProperty(ref _periodIncome, value);
        }

        private decimal _periodSpent;
        public decimal PeriodSpent
        {
            get => _periodSpent;
            private set => SetProperty(ref _periodSpent, value);
        }

        // Backwards-compat / old UI bindings (still kept)
        private decimal _spentThisMonth;
        public decimal SpentThisMonth
        {
            get => _spentThisMonth;
            private set => SetProperty(ref _spentThisMonth, value);
        }

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

        // ===== CHILD VIEWMODELS (Views bind to these) =====
        public IncomeBudgetViewModel? IncomeVM { get; private set; }
        public MonthlyBudgetViewModel? MonthlyVM { get; private set; }

        private ViewModelBase? _currentViewModel;
        public ViewModelBase? CurrentViewModel
        {
            get => _currentViewModel;
            private set => SetProperty(ref _currentViewModel, value);
        }

        public ICommand ShowIncomeCommand { get; }
        public ICommand ShowMonthlyCommand { get; }

        public BudgetDashboardViewModel(BudgetDbContext db, IUserRepository userRepository)
        {
            _db = db;
            _userRepository = userRepository;

            ShowIncomeCommand = new RelayCommand(() =>
            {
                if (IncomeVM != null) CurrentViewModel = IncomeVM;
            });

            ShowMonthlyCommand = new RelayCommand(() =>
            {
                if (MonthlyVM != null) CurrentViewModel = MonthlyVM;
            });

            _ = ReloadAsync();
        }

        public async Task ReloadAsync()
        {
            ActiveUser = await _userRepository.GetActiveAsync();

            if (ActiveUser == null)
            {
                Balance = 0m;
                PeriodIncome = 0m;
                PeriodSpent = 0m;

                SpentThisMonth = 0m;
                IncomeThisMonth = 0m;
                ExpensesThisMonth = 0m;
                SavingsThisMonth = 0m;

                IncomeVM = null;
                MonthlyVM = null;
                CurrentViewModel = null;

                OnPropertyChanged(nameof(IncomeVM));
                OnPropertyChanged(nameof(MonthlyVM));
                OnPropertyChanged(nameof(TodayWithWeek));
                OnPropertyChanged(nameof(CurrentWeek));
                OnPropertyChanged(nameof(PeriodLabel));
                return;
            }

            // ✅ Create/refresh child VMs with correct user id
            IncomeVM ??= new IncomeBudgetViewModel(_db, ActiveUser.Id);
            MonthlyVM ??= new MonthlyBudgetViewModel(() => new BudgetDbContext(), ActiveUser.Id);

            IncomeVM.Saved -= OnIncomeSaved;   // avoid double subscribe
            IncomeVM.Saved += OnIncomeSaved;

            // Default view
            CurrentViewModel ??= IncomeVM;

            OnPropertyChanged(nameof(IncomeVM));
            OnPropertyChanged(nameof(MonthlyVM));

            // Recalculate all top bar totals
            await ReloadTotalsAsync();

            OnPropertyChanged(nameof(ActiveUserFullName));
            OnPropertyChanged(nameof(TodayWithWeek));
            OnPropertyChanged(nameof(CurrentWeek));
        }

        // Called from code-behind when you switch section (Monthly/Quarterly/Yearly)
        public Task ReloadTotalsAsync() => RecalculateDashboardTotalsAsync();

        private async void OnIncomeSaved()
        {
            if (MonthlyVM != null)
                await MonthlyVM.ReloadAsync();

            await RecalculateDashboardTotalsAsync();
        }

        private async Task RecalculateDashboardTotalsAsync()
        {
            if (ActiveUser == null) return;

            // =========================
            // BALANCE: income ever - expenses ever
            // (savings should NOT be subtracted according to your new rule)
            // =========================
            var totalIncome = await _db.Items
                .AsNoTracking()
                .Where(i => i.UserId == ActiveUser.Id && i.ItemType == ItemType.Income)
                .SumAsync(i => (decimal?)i.Amount) ?? 0m;

            var totalExpenses = await _db.Items
                .AsNoTracking()
                .Where(i => i.UserId == ActiveUser.Id && i.ItemType == ItemType.Expense)
                .SumAsync(i => (decimal?)i.Amount) ?? 0m;

            Balance = totalIncome - totalExpenses;

            // =========================
            // PERIOD RANGE (month/quarter/year)
            // =========================
            var today = DateTime.Today;
            DateTime from;
            DateTime toExclusive;

            switch (ActivePeriodKind)
            {
                case DashboardPeriodKind.Monthly:
                    from = new DateTime(today.Year, today.Month, 1);
                    toExclusive = from.AddMonths(1);
                    break;

                case DashboardPeriodKind.Quarterly:
                    var qStartMonth = ((today.Month - 1) / 3) * 3 + 1;
                    from = new DateTime(today.Year, qStartMonth, 1);
                    toExclusive = from.AddMonths(3);
                    break;

                case DashboardPeriodKind.Yearly:
                    from = new DateTime(today.Year, 1, 1);
                    toExclusive = from.AddYears(1);
                    break;

                default:
                    from = new DateTime(today.Year, today.Month, 1);
                    toExclusive = from.AddMonths(1);
                    break;
            }

            // =========================
            // PERIOD TOTALS
            // =========================
            PeriodIncome = await _db.Items
                .AsNoTracking()
                .Where(i => i.UserId == ActiveUser.Id
                            && i.ItemType == ItemType.Income
                            && i.TransactionDate >= from
                            && i.TransactionDate < toExclusive)
                .SumAsync(i => (decimal?)i.Amount) ?? 0m;

            PeriodSpent = await _db.Items
                .AsNoTracking()
                .Where(i => i.UserId == ActiveUser.Id
                            && i.ItemType == ItemType.Expense
                            && i.TransactionDate >= from
                            && i.TransactionDate < toExclusive)
                .SumAsync(i => (decimal?)i.Amount) ?? 0m;

            // =========================
            // Keep old monthly values alive (in case something still binds to them)
            // Always compute these from the monthly window.
            // =========================
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var nextMonthStart = monthStart.AddMonths(1);

            IncomeThisMonth = await _db.Items
                .AsNoTracking()
                .Where(i => i.UserId == ActiveUser.Id
                            && i.TransactionDate >= monthStart
                            && i.TransactionDate < nextMonthStart
                            && i.ItemType == ItemType.Income)
                .SumAsync(i => (decimal?)i.Amount) ?? 0m;

            ExpensesThisMonth = await _db.Items
                .AsNoTracking()
                .Where(i => i.UserId == ActiveUser.Id
                            && i.TransactionDate >= monthStart
                            && i.TransactionDate < nextMonthStart
                            && i.ItemType == ItemType.Expense)
                .SumAsync(i => (decimal?)i.Amount) ?? 0m;

            SavingsThisMonth = await _db.Items
                .AsNoTracking()
                .Where(i => i.UserId == ActiveUser.Id
                            && i.TransactionDate >= monthStart
                            && i.TransactionDate < nextMonthStart
                            && i.ItemType == ItemType.Savings)
                .SumAsync(i => (decimal?)i.Amount) ?? 0m;

            SpentThisMonth = ExpensesThisMonth;
        }
    }
}
