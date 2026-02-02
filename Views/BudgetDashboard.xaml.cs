using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Wpf_Budgetplanerare.Data;
using Wpf_Budgetplanerare.Data.Repositories.Implementations;
using Wpf_Budgetplanerare.ViewModels;

namespace Wpf_Budgetplanerare.Views
{
    public partial class BudgetDashboard : Window
    {
        private readonly string[] _sections = { "Monthly", "Quarterly", "Yearly", "Income" };
        private int _currentIndex = 0;

        private readonly BudgetDbContext _db;
        private readonly UserRepository _userRepo;

        private BudgetDbContext? _monthlyDb;
        private BudgetDbContext? _quarterlyDb;
        private BudgetDbContext? _yearlyDb;
        private BudgetDbContext? _incomeDb;
        private BudgetDbContext? _savingsDb;
        private BudgetDbContext? _expenseDb;

        private BudgetDbContext? _transactionsDb;

        private IncomeBudgetView? _incomeView;
        private MonthlyBudgetView? _monthlyView;
        private QuarterlyBudgetView? _quarterlyView;
        private YearlyBudgetView? _yearlyView;

        private SavingsSummaryView? _savingsSummaryView;
        private BudgetProgressView? _budgetProgressView;

        private ExpenseEntryView? _expenseEntryView;
        private ExpenseEntryViewModel? _expenseVm;

        private MonthlyBudgetViewModel? _monthlyVm;
        private QuarterlyBudgetViewModel? _quarterlyVm;
        private YearlyBudgetViewModel? _yearlyVm;

        private BudgetProgressViewModel? _progressVm;

        public BudgetDashboard()
        {
            InitializeComponent();

            _db = new BudgetDbContext();
            _userRepo = new UserRepository(_db);

            DataContext = new BudgetDashboardViewModel(_db, _userRepo);

            Loaded += BudgetDashboard_Loaded;
            UpdateSection();
        }

        private void BudgetDashboard_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is BudgetDashboardViewModel dashVm)
            {
                dashVm.PropertyChanged += DashVm_PropertyChanged;

                EnsureExpenseEntryView();
                EnsureTransactionsTableView();
                UpdateSection();
            }
        }

        private void DashVm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(BudgetDashboardViewModel.ActiveUser))
            {
                ResetCachedViewsAndContexts();

                EnsureExpenseEntryView();
                EnsureTransactionsTableView();
                UpdateSection();
            }
        }

        private void ResetCachedViewsAndContexts()
        {
            try { _progressVm?.Dispose(); } catch { /* ignore */ }
            _progressVm = null;

            _incomeView = null;
            _monthlyView = null;
            _quarterlyView = null;
            _yearlyView = null;

            _savingsSummaryView = null;
            _budgetProgressView = null;

            _expenseEntryView = null;
            _expenseVm = null;

            try { _monthlyVm?.Dispose(); } catch { /* ignore */ }
            try { _quarterlyVm?.Dispose(); } catch { /* ignore */ }
            try { _yearlyVm?.Dispose(); } catch { /* ignore */ }
            _monthlyVm = null;
            _quarterlyVm = null;
            _yearlyVm = null;

            DisposeAndNull(ref _monthlyDb);
            DisposeAndNull(ref _quarterlyDb);
            DisposeAndNull(ref _yearlyDb);
            DisposeAndNull(ref _incomeDb);
            DisposeAndNull(ref _savingsDb);
            DisposeAndNull(ref _expenseDb);

            // NEW
            DisposeAndNull(ref _transactionsDb);
        }

        private static void DisposeAndNull(ref BudgetDbContext? ctx)
        {
            try { ctx?.Dispose(); }
            catch { /* ignore dispose issues */ }
            ctx = null;
        }

        private void EnsureTransactionsTableView()
        {
            if (TransactionsTable == null)
                return;

            if (DataContext is not BudgetDashboardViewModel dashVm || dashVm.ActiveUser == null)
                return;

            // Reset context so we always read newest values
            DisposeAndNull(ref _transactionsDb);
            _transactionsDb = new BudgetDbContext();

            TransactionsTable.Init(_transactionsDb, dashVm.ActiveUser.Id);
        }

        private void EnsureExpenseEntryView()
        {
            if (RightBottomContent == null)
                return;

            if (DataContext is not BudgetDashboardViewModel dashVm || dashVm.ActiveUser == null)
            {
                RightBottomContent.Content = new TextBlock
                {
                    Text = "No active user.",
                    FontSize = 14,
                    Margin = new Thickness(10)
                };
                return;
            }

            _expenseEntryView ??= new ExpenseEntryView();
            _expenseDb ??= new BudgetDbContext();

            if (_expenseVm == null || _expenseEntryView.DataContext is not ExpenseEntryViewModel)
            {
                _expenseVm = new ExpenseEntryViewModel(_expenseDb, dashVm.ActiveUser.Id);
                _expenseEntryView.DataContext = _expenseVm;
            }

            RightBottomContent.Content = _expenseEntryView;
        }

        private void Prev_Click(object sender, RoutedEventArgs e)
        {
            _currentIndex = (_currentIndex - 1 + _sections.Length) % _sections.Length;
            UpdateSection();
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            _currentIndex = (_currentIndex + 1) % _sections.Length;
            UpdateSection();
        }

        private void UpdateSection()
        {
            var section = _sections[_currentIndex];

            if (SectionTitle != null)
                SectionTitle.Text = section;

            // Tell the dashboard VM which period is active (month/quarter/year)
            if (DataContext is BudgetDashboardViewModel dashVm)
            {
                dashVm.ActivePeriodKind = section switch
                {
                    "Monthly" => DashboardPeriodKind.Monthly,
                    "Quarterly" => DashboardPeriodKind.Quarterly,
                    "Yearly" => DashboardPeriodKind.Yearly,
                    _ => DashboardPeriodKind.Monthly
                };

                _ = dashVm.ReloadTotalsAsync();
            }

            if (LeftContent == null)
                return;

            LeftContent.Content = section switch
            {
                "Monthly" => CreateMonthlyView(),
                "Quarterly" => CreateQuarterlyView(),
                "Yearly" => CreateYearlyView(),
                "Income" => CreateIncomeView(),
                _ => new TextBlock { Text = "Unknown section", FontSize = 14, Margin = new Thickness(10) }
            };

            UpdateMiddleRightContent(section);
            EnsureExpenseEntryView();
            EnsureTransactionsTableView();
        }

        private void UpdateMiddleRightContent(string section)
        {
            if (MiddleRightContent == null)
                return;

            MiddleRightContent.Content = section switch
            {
                "Income" => CreateSavingsSummaryView(),
                "Monthly" => CreateBudgetProgressView(BudgetPeriodKind.Monthly),
                "Quarterly" => CreateBudgetProgressView(BudgetPeriodKind.Quarterly),
                "Yearly" => CreateBudgetProgressView(BudgetPeriodKind.Yearly),
                _ => null
            };
        }

        private FrameworkElement NoActiveUser()
        {
            return new TextBlock
            {
                Text = "No active user.",
                FontSize = 14,
                Margin = new Thickness(10)
            };
        }

        private FrameworkElement CreateMonthlyView()
        {
            if (DataContext is not BudgetDashboardViewModel dashVm || dashVm.ActiveUser == null)
                return NoActiveUser();

            if (_monthlyView == null)
            {
                _monthlyDb ??= new BudgetDbContext();

                _monthlyVm = new MonthlyBudgetViewModel(() => new BudgetDbContext(), dashVm.ActiveUser.Id);
                _monthlyView = new MonthlyBudgetView { DataContext = _monthlyVm };
            }

            return _monthlyView;
        }

        private FrameworkElement CreateQuarterlyView()
        {
            if (DataContext is not BudgetDashboardViewModel dashVm || dashVm.ActiveUser == null)
                return NoActiveUser();

            if (_quarterlyView == null)
            {
                _quarterlyDb ??= new BudgetDbContext();

                _quarterlyVm = new QuarterlyBudgetViewModel(() => new BudgetDbContext(), dashVm.ActiveUser.Id);
                _quarterlyView = new QuarterlyBudgetView { DataContext = _quarterlyVm };
            }

            return _quarterlyView;
        }

        private FrameworkElement CreateYearlyView()
        {
            if (DataContext is not BudgetDashboardViewModel dashVm || dashVm.ActiveUser == null)
                return NoActiveUser();

            if (_yearlyView == null)
            {
                _yearlyDb ??= new BudgetDbContext();

                _yearlyVm = new YearlyBudgetViewModel(() => new BudgetDbContext(), dashVm.ActiveUser.Id);
                _yearlyView = new YearlyBudgetView { DataContext = _yearlyVm };
            }

            return _yearlyView;
        }

        private FrameworkElement CreateIncomeView()
        {
            if (DataContext is not BudgetDashboardViewModel dashVm || dashVm.ActiveUser == null)
                return NoActiveUser();

            if (_incomeView == null)
            {
                _incomeDb ??= new BudgetDbContext();
                var incomeVm = new IncomeBudgetViewModel(_incomeDb, dashVm.ActiveUser.Id);

                incomeVm.Saved += async () =>
                {
                    if (DataContext is BudgetDashboardViewModel dvm)
                        await dvm.ReloadAsync();
                };

                _incomeView = new IncomeBudgetView { DataContext = incomeVm };
            }

            return _incomeView;
        }

        private FrameworkElement CreateSavingsSummaryView()
        {
            if (DataContext is not BudgetDashboardViewModel dashVm || dashVm.ActiveUser == null)
                return NoActiveUser();

            if (_savingsSummaryView == null)
            {
                _savingsDb ??= new BudgetDbContext();
                _savingsSummaryView = new SavingsSummaryView
                {
                    DataContext = new SavingsSummaryViewModel(_savingsDb, dashVm.ActiveUser.Id)
                };
            }

            return _savingsSummaryView;
        }

        private FrameworkElement CreateBudgetProgressView(BudgetPeriodKind kind)
        {
            if (DataContext is not BudgetDashboardViewModel dashVm || dashVm.ActiveUser == null)
                return NoActiveUser();

            _budgetProgressView ??= new BudgetProgressView();

            // Ensure the left VM exists BEFORE wiring progress to it
            switch (kind)
            {
                case BudgetPeriodKind.Monthly:
                    CreateMonthlyView();
                    break;
                case BudgetPeriodKind.Quarterly:
                    CreateQuarterlyView();
                    break;
                case BudgetPeriodKind.Yearly:
                    CreateYearlyView();
                    break;
            }

            INotifyPropertyChanged? periodSource = kind switch
            {
                BudgetPeriodKind.Monthly => _monthlyVm,
                BudgetPeriodKind.Quarterly => _quarterlyVm,
                BudgetPeriodKind.Yearly => _yearlyVm,
                _ => null
            };

            try { _progressVm?.Dispose(); } catch { /* ignore */ }
            _progressVm = null;

            _progressVm = new BudgetProgressViewModel(
                dbFactory: () => new BudgetDbContext(),
                userId: dashVm.ActiveUser.Id,
                periodKind: kind,
                periodSourceVm: periodSource,
                periodSourcePropertyName: "SelectedMonth"
            );

            _budgetProgressView.DataContext = _progressVm;
            return _budgetProgressView;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            if (DataContext is BudgetDashboardViewModel dashVm)
                dashVm.PropertyChanged -= DashVm_PropertyChanged;

            Loaded -= BudgetDashboard_Loaded;

            try { _progressVm?.Dispose(); } catch { /* ignore */ }
            _progressVm = null;

            try { _monthlyVm?.Dispose(); } catch { /* ignore */ }
            try { _quarterlyVm?.Dispose(); } catch { /* ignore */ }
            try { _yearlyVm?.Dispose(); } catch { /* ignore */ }
            _monthlyVm = null;
            _quarterlyVm = null;
            _yearlyVm = null;

            DisposeAndNull(ref _monthlyDb);
            DisposeAndNull(ref _quarterlyDb);
            DisposeAndNull(ref _yearlyDb);
            DisposeAndNull(ref _incomeDb);
            DisposeAndNull(ref _savingsDb);
            DisposeAndNull(ref _expenseDb);
            DisposeAndNull(ref _transactionsDb);

            _db.Dispose();
        }
    }
}
