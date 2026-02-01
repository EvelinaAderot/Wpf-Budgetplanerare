using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Wpf_Budgetplanerare.Data;
using Wpf_Budgetplanerare.Models;
using Wpf_Budgetplanerare.ViewModels.Base;

namespace Wpf_Budgetplanerare.ViewModels
{
    public class YearlyBudgetViewModel : ViewModelBase
    {
        private readonly BudgetDbContext _db;
        private readonly int _userId;

        public ObservableCollection<BudgetRowVM> BudgetRows { get; } = new();
        public ICollectionView BudgetRowsView { get; }

        private DateTime _selectedMonth = new(DateTime.Now.Year, DateTime.Now.Month, 1);
        public DateTime SelectedMonth
        {
            get => _selectedMonth;
            set
            {
                var normalized = new DateTime(value.Year, value.Month, 1);
                if (SetProperty(ref _selectedMonth, normalized))
                {
                    OnPropertyChanged(nameof(CurrentPeriodText));
                    _ = ReloadAsync();
                }
            }
        }

        public string CurrentPeriodText => SelectedMonth.Year.ToString(CultureInfo.CurrentCulture);

        private bool _isEditMode;
        public bool IsEditMode
        {
            get => _isEditMode;
            private set
            {
                if (SetProperty(ref _isEditMode, value))
                    BudgetRowsView.Refresh();
            }
        }

        public ICommand ToggleEditCommand { get; }

        private decimal _totalBudget;
        public decimal TotalBudget
        {
            get => _totalBudget;
            private set
            {
                if (SetProperty(ref _totalBudget, value))
                    NotifyRemainingChanged();
            }
        }

        public decimal DistributedTotal => BudgetRows.Sum(x => x.Amount);
        public decimal RemainingToAllocate => TotalBudget - DistributedTotal;
        public bool IsRemainingNegative => RemainingToAllocate < 0;

        public YearlyBudgetViewModel(BudgetDbContext db, int userId)
        {
            _db = db;
            _userId = userId;

            ToggleEditCommand = new RelayCommand(async () => await ToggleEditAsync());

            BudgetRowsView = CollectionViewSource.GetDefaultView(BudgetRows);
            BudgetRowsView.Filter = BudgetRowFilter;

            BudgetRows.CollectionChanged += BudgetRows_CollectionChanged;

            _ = ReloadAsync();
        }

        private static DateTime GetYearStart(DateTime month) => new DateTime(month.Year, 1, 1);
        private static DateTime GetYearEnd(DateTime month) => new DateTime(month.Year, 12, 1);

        private bool BudgetRowFilter(object obj)
        {
            if (IsEditMode) return true;
            return obj is BudgetRowVM row ? row.Amount != 0m : true;
        }

        private void BudgetRows_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
                foreach (var item in e.OldItems.OfType<BudgetRowVM>())
                    item.PropertyChanged -= Row_PropertyChanged;

            if (e.NewItems != null)
                foreach (var item in e.NewItems.OfType<BudgetRowVM>())
                    item.PropertyChanged += Row_PropertyChanged;

            BudgetRowsView.Refresh();
            NotifyRemainingChanged();
        }

        private void Row_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(BudgetRowVM.Amount))
            {
                BudgetRowsView.Refresh();
                NotifyRemainingChanged();
            }
        }

        private void NotifyRemainingChanged()
        {
            OnPropertyChanged(nameof(DistributedTotal));
            OnPropertyChanged(nameof(RemainingToAllocate));
            OnPropertyChanged(nameof(IsRemainingNegative));
        }

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
            foreach (var row in BudgetRows)
                row.PropertyChanged -= Row_PropertyChanged;

            BudgetRows.Clear();

            var periodStart = GetYearStart(SelectedMonth);
            var periodEnd = GetYearEnd(SelectedMonth);

            // Total budget from Income (BudgetPlan) on periodStart
            var plan = await _db.BudgetPlans
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == _userId && p.Month == periodStart);

            TotalBudget = plan?.YearlyBudget ?? 0m;

            var categories = await _db.Categories
                .Where(c => c.ItemType == ItemType.Expense || c.ItemType == ItemType.Savings)
                .OrderBy(c => c.ItemType)
                .ThenBy(c => c.Name)
                .ToListAsync();

            var catIds = categories.Select(c => c.Id).ToList();

            var existing = await _db.MonthlyBudgets
                .Where(mb =>
                    mb.UserId == _userId &&
                    mb.Month == periodStart &&
                    mb.EndMonth == periodEnd &&
                    catIds.Contains(mb.CategoryId))
                .ToListAsync();

            foreach (var c in categories)
            {
                var mb = existing.FirstOrDefault(x => x.CategoryId == c.Id);

                BudgetRows.Add(new BudgetRowVM
                {
                    CategoryId = c.Id,
                    Name = c.Name,
                    Amount = mb?.Amount ?? 0m
                });
            }

            BudgetRowsView.Refresh();
            NotifyRemainingChanged();
        }

        private async Task SaveAsync()
        {
            var periodStart = GetYearStart(SelectedMonth);
            var periodEnd = GetYearEnd(SelectedMonth);

            var catIds = BudgetRows.Select(r => r.CategoryId).ToList();

            var existing = await _db.MonthlyBudgets
                .Where(mb =>
                    mb.UserId == _userId &&
                    mb.Month == periodStart &&
                    mb.EndMonth == periodEnd &&
                    catIds.Contains(mb.CategoryId))
                .ToListAsync();

            _db.MonthlyBudgets.RemoveRange(existing);

            foreach (var row in BudgetRows)
            {
                if (row.Amount == 0m)
                    continue;

                _db.MonthlyBudgets.Add(new MonthlyBudget
                {
                    UserId = _userId,
                    CategoryId = row.CategoryId,
                    Month = periodStart,
                    EndMonth = periodEnd,
                    Amount = row.Amount
                });
            }

            await _db.SaveChangesAsync();

            BudgetRowsView.Refresh();
            NotifyRemainingChanged();
        }

        public class BudgetRowVM : ViewModelBase
        {
            public int CategoryId { get; set; }
            public string Name { get; set; } = string.Empty;

            private decimal _amount;
            public decimal Amount
            {
                get => _amount;
                set => SetProperty(ref _amount, value);
            }
        }
    }
}
