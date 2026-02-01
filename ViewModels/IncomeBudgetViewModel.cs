// ff — IncomeBudgetViewModel.cs (fix DbUpdateException: BudgetPlan duplicate (UserId, Month))
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Wpf_Budgetplanerare.Data;
using Wpf_Budgetplanerare.Models;
using Wpf_Budgetplanerare.ViewModels.Base;

namespace Wpf_Budgetplanerare.ViewModels
{
    public class IncomeBudgetViewModel : ViewModelBase
    {
        private readonly BudgetDbContext _db;
        private readonly int _userId;

        public ObservableCollection<IncomeCategoryRowVM> IncomeCategories { get; } = new();

        public event Action? Saved;

        private DateTime _selectedMonth = new(DateTime.Now.Year, DateTime.Now.Month, 1);
        public DateTime SelectedMonth
        {
            get => _selectedMonth;
            set
            {
                var normalized = new DateTime(value.Year, value.Month, 1);
                if (SetProperty(ref _selectedMonth, normalized))
                {
                    OnPropertyChanged(nameof(CurrentMonthText));
                    _ = ReloadAsync();
                }
            }
        }

        public string CurrentMonthText =>
            SelectedMonth.ToString("MMMM yyyy", CultureInfo.CurrentCulture);

        private bool _isEditMode;
        public bool IsEditMode
        {
            get => _isEditMode;
            private set => SetProperty(ref _isEditMode, value);
        }

        public ICommand ToggleEditCommand { get; }

        public decimal TotalIncome => IncomeCategories.Sum(x => x.Amount);

        private decimal _monthlyBudgetAllocation;
        public decimal MonthlyBudgetAllocation
        {
            get => _monthlyBudgetAllocation;
            set
            {
                if (SetProperty(ref _monthlyBudgetAllocation, value))
                    OnPropertyChanged(nameof(RemainingTotal));
            }
        }

        private decimal _quarterBudgetAllocation;
        public decimal QuarterBudgetAllocation
        {
            get => _quarterBudgetAllocation;
            set
            {
                if (SetProperty(ref _quarterBudgetAllocation, value))
                    OnPropertyChanged(nameof(RemainingTotal));
            }
        }

        private decimal _yearBudgetAllocation;
        public decimal YearBudgetAllocation
        {
            get => _yearBudgetAllocation;
            set
            {
                if (SetProperty(ref _yearBudgetAllocation, value))
                    OnPropertyChanged(nameof(RemainingTotal));
            }
        }

        public decimal RemainingTotal =>
            TotalIncome - (MonthlyBudgetAllocation + QuarterBudgetAllocation + YearBudgetAllocation);

        public IncomeBudgetViewModel(BudgetDbContext db, int userId)
        {
            _db = db;
            _userId = userId;

            ToggleEditCommand = new RelayCommand(async () => await ToggleEditAsync());
            _ = ReloadAsync();
        }

        private static DateTime GetQuarterStart(DateTime month)
        {
            var m = ((month.Month - 1) / 3) * 3 + 1; // 1,4,7,10
            return new DateTime(month.Year, m, 1);
        }

        private static DateTime GetYearStart(DateTime month) => new DateTime(month.Year, 1, 1);

        private async Task ToggleEditAsync()
        {
            if (IsEditMode)
            {
                await SaveAsync();
                IsEditMode = false;
            }
            else
            {
                IsEditMode = true;
            }
        }

        public async Task ReloadAsync()
        {
            IncomeCategories.Clear();

            var cats = await _db.Categories
                .Where(c => c.ItemType == ItemType.Income)
                .OrderBy(c => c.Name)
                .ToListAsync();

            var month = SelectedMonth;

            var monthBudgets = await _db.MonthlyBudgets
                .Where(mb => mb.UserId == _userId && mb.Month == month)
                .ToListAsync();

            foreach (var c in cats)
            {
                var mb = monthBudgets.FirstOrDefault(x => x.CategoryId == c.Id);

                IncomeCategories.Add(new IncomeCategoryRowVM
                {
                    CategoryId = c.Id,
                    Name = c.Name,
                    Amount = mb?.Amount ?? 0m,
                    EndMonth = mb?.EndMonth ?? month
                });
            }

            // Read allocations from correct period starts
            var monthStart = month;
            var quarterStart = GetQuarterStart(month);
            var yearStart = GetYearStart(month);

            var monthPlan = await _db.BudgetPlans.AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == _userId && p.Month == monthStart);

            var quarterPlan = await _db.BudgetPlans.AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == _userId && p.Month == quarterStart);

            var yearPlan = await _db.BudgetPlans.AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == _userId && p.Month == yearStart);

            MonthlyBudgetAllocation = monthPlan?.MonthlyBudget ?? 0m;
            QuarterBudgetAllocation = quarterPlan?.QuarterlyBudget ?? 0m;
            YearBudgetAllocation = yearPlan?.YearlyBudget ?? 0m;

            OnPropertyChanged(nameof(TotalIncome));
            OnPropertyChanged(nameof(RemainingTotal));
        }

        private async Task SaveAsync()
        {
            var start = SelectedMonth;
            var categoryIds = IncomeCategories.Select(x => x.CategoryId).ToList();

            // 1) Save INCOME budgets (MonthlyBudgets)
            var existingBudgets = await _db.MonthlyBudgets
                .Where(mb => mb.UserId == _userId
                             && categoryIds.Contains(mb.CategoryId)
                             && mb.Month >= start)
                .ToListAsync();

            _db.MonthlyBudgets.RemoveRange(existingBudgets);

            foreach (var row in IncomeCategories)
            {
                var end = row.EndMonth < start ? start : row.EndMonth;

                for (var m = start; m <= end; m = m.AddMonths(1))
                {
                    _db.MonthlyBudgets.Add(new MonthlyBudget
                    {
                        UserId = _userId,
                        CategoryId = row.CategoryId,
                        Month = m,
                        EndMonth = end,
                        Amount = row.Amount
                    });
                }
            }

            // 2) Save BudgetPlan allocations (SAFE: one plan per unique month)
            var monthStart = start;
            var quarterStart = GetQuarterStart(start);
            var yearStart = GetYearStart(start);

            var distinctPlanMonths = new[] { monthStart, quarterStart, yearStart }
                .Distinct()
                .ToList();

            foreach (var planMonth in distinctPlanMonths)
            {
                // IMPORTANT: check tracked entities first (avoids duplicates before SaveChanges)
                var plan = _db.BudgetPlans.Local.FirstOrDefault(p => p.UserId == _userId && p.Month == planMonth)
                           ?? await _db.BudgetPlans.FirstOrDefaultAsync(p => p.UserId == _userId && p.Month == planMonth);

                if (plan == null)
                {
                    plan = new BudgetPlan
                    {
                        UserId = _userId,
                        Month = planMonth,
                        MonthlyBudget = 0m,
                        QuarterlyBudget = 0m,
                        YearlyBudget = 0m
                    };
                    _db.BudgetPlans.Add(plan);
                }

                if (planMonth == monthStart)
                    plan.MonthlyBudget = MonthlyBudgetAllocation;

                if (planMonth == quarterStart)
                    plan.QuarterlyBudget = QuarterBudgetAllocation;

                if (planMonth == yearStart)
                    plan.YearlyBudget = YearBudgetAllocation;
            }

            // 3) Upsert REAL income Items for THIS selected month (so Balance updates)
            var nextMonthStart = monthStart.AddMonths(1);

            var existingIncomeItems = await _db.Items
                .Where(i => i.UserId == _userId
                            && i.ItemType == ItemType.Income
                            && i.TransactionDate >= monthStart
                            && i.TransactionDate < nextMonthStart
                            && categoryIds.Contains(i.CategoryId))
                .ToListAsync();

            foreach (var row in IncomeCategories)
            {
                var existingItem = existingIncomeItems.FirstOrDefault(i => i.CategoryId == row.CategoryId);

                if (row.Amount == 0m)
                {
                    if (existingItem != null)
                        _db.Items.Remove(existingItem);
                    continue;
                }

                if (existingItem == null)
                {
                    _db.Items.Add(new Item
                    {
                        UserId = _userId,
                        CategoryId = row.CategoryId,
                        ItemType = ItemType.Income,
                        Amount = row.Amount,
                        TransactionDate = monthStart,
                        PostingDate = monthStart,
                        RecurrenceType = default
                    });
                }
                else
                {
                    existingItem.Amount = row.Amount;
                    existingItem.TransactionDate = monthStart;
                    existingItem.PostingDate = monthStart;
                }
            }

            await _db.SaveChangesAsync();

            OnPropertyChanged(nameof(TotalIncome));
            OnPropertyChanged(nameof(RemainingTotal));

            Saved?.Invoke();
        }
    }

    public class IncomeCategoryRowVM : ViewModelBase
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;

        private decimal _amount;
        public decimal Amount
        {
            get => _amount;
            set => SetProperty(ref _amount, value);
        }

        private DateTime _endMonth;
        public DateTime EndMonth
        {
            get => _endMonth;
            set => SetProperty(ref _endMonth, new DateTime(value.Year, value.Month, 1));
        }
    }
}
