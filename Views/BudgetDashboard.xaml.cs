using System;
using System.Threading.Tasks;
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

        private IncomeBudgetView? _incomeView;
        private MonthlyBudgetView? _monthlyView;
        private QuarterlyBudgetView? _quarterlyView;
        private YearlyBudgetView? _yearlyView;

        // ✅ Savings box in row 1 col 1
        private SavingsSummaryView? _savingsSummaryView;

        public BudgetDashboard()
        {
            InitializeComponent();

            _db = new BudgetDbContext();
            _userRepo = new UserRepository(_db);

            DataContext = new BudgetDashboardViewModel(_db, _userRepo);

            UpdateSection();
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

            // ✅ show savings summary only for Income
            if (MiddleRightContent != null)
            {
                MiddleRightContent.Content = section == "Income"
                    ? CreateSavingsSummaryView()
                    : null;
            }
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

            _monthlyView ??= new MonthlyBudgetView
            {
                DataContext = new MonthlyBudgetViewModel(_db, dashVm.ActiveUser.Id)
            };

            return _monthlyView;
        }

        private FrameworkElement CreateQuarterlyView()
        {
            if (DataContext is not BudgetDashboardViewModel dashVm || dashVm.ActiveUser == null)
                return NoActiveUser();

            _quarterlyView ??= new QuarterlyBudgetView
            {
                DataContext = new QuarterlyBudgetViewModel(_db, dashVm.ActiveUser.Id)
            };

            return _quarterlyView;
        }

        private FrameworkElement CreateYearlyView()
        {
            if (DataContext is not BudgetDashboardViewModel dashVm || dashVm.ActiveUser == null)
                return NoActiveUser();

            _yearlyView ??= new YearlyBudgetView
            {
                DataContext = new YearlyBudgetViewModel(_db, dashVm.ActiveUser.Id)
            };

            return _yearlyView;
        }

        private FrameworkElement CreateIncomeView()
        {
            if (DataContext is not BudgetDashboardViewModel dashVm || dashVm.ActiveUser == null)
                return NoActiveUser();

            if (_incomeView == null)
            {
                var incomeVm = new IncomeBudgetViewModel(_db, dashVm.ActiveUser.Id);

                // ✅ when Income saves: refresh top + savings summary
                incomeVm.Saved += async () =>
                {
                    if (DataContext is BudgetDashboardViewModel dvm)
                        await dvm.ReloadAsync();

                    if (_savingsSummaryView?.DataContext is SavingsSummaryViewModel svm)
                        await svm.ReloadAsync();
                };

                _incomeView = new IncomeBudgetView
                {
                    DataContext = incomeVm
                };
            }

            return _incomeView;
        }

        private FrameworkElement CreateSavingsSummaryView()
        {
            if (DataContext is not BudgetDashboardViewModel dashVm || dashVm.ActiveUser == null)
                return NoActiveUser();

            if (_savingsSummaryView == null)
            {
                _savingsSummaryView = new SavingsSummaryView
                {
                    DataContext = new SavingsSummaryViewModel(_db, dashVm.ActiveUser.Id)
                };
            }

            _ = RefreshSavingsSummaryAsync();
            return _savingsSummaryView;
        }

        private async Task RefreshSavingsSummaryAsync()
        {
            if (_savingsSummaryView?.DataContext is SavingsSummaryViewModel vm)
                await vm.ReloadAsync();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _db.Dispose();
        }
    }
}
