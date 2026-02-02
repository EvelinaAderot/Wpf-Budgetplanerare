// ff — MonthlyBudgetViewModel.cs (FULL FILE, DbContext-per-operation, latest-wins reload, safe save/reload serialization)
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Wpf_Budgetplanerare.Data;
using Wpf_Budgetplanerare.Models;
using Wpf_Budgetplanerare.ViewModels.Base;

namespace Wpf_Budgetplanerare.ViewModels
{
    public class MonthlyBudgetViewModel : ViewModelBase, IDisposable
    {
        private readonly Func<BudgetDbContext> _dbFactory;
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
                    OnPropertyChanged(nameof(CurrentMonthText));
                    _ = ReloadAsyncSafe();
                }
            }
        }

        public string CurrentMonthText =>
            SelectedMonth.ToString("MMMM yyyy", CultureInfo.CurrentCulture);

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

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                    (ToggleEditCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public ICommand ToggleEditCommand { get; }

        private decimal _monthlyBudgetTotal;
        public decimal MonthlyBudgetTotal
        {
            get => _monthlyBudgetTotal;
            private set
            {
                if (SetProperty(ref _monthlyBudgetTotal, value))
                    NotifyRemainingChanged();
            }
        }

        public decimal DistributedTotal => BudgetRows.Sum(x => x.Amount);
        public decimal RemainingToAllocate => MonthlyBudgetTotal - DistributedTotal;
        public bool IsRemainingNegative => RemainingToAllocate < 0;

        // Serialize ALL operations (reload/save) so UI collection isn't modified concurrently
        private readonly SemaphoreSlim _opGate = new(1, 1);

        // Latest-wins reload
        private CancellationTokenSource? _reloadCts;

        private bool _isDisposed;

        public MonthlyBudgetViewModel(Func<BudgetDbContext> dbFactory, int userId)
        {
            _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
            _userId = userId;

            ToggleEditCommand = new RelayCommand(
                async () => await ToggleEditAsync(),
                () => !IsBusy
            );

            BudgetRowsView = CollectionViewSource.GetDefaultView(BudgetRows);
            BudgetRowsView.Filter = BudgetRowFilter;

            BudgetRows.CollectionChanged += BudgetRows_CollectionChanged;

            _ = ReloadAsyncSafe();
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            try { BudgetRows.CollectionChanged -= BudgetRows_CollectionChanged; } catch { }

            try { _reloadCts?.Cancel(); } catch { }
            try { _reloadCts?.Dispose(); } catch { }
            _reloadCts = null;
        }

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
            if (IsBusy) return;

            try
            {
                IsBusy = true;

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
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Monthly edit/save failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        // Latest-wins reload: cancels previous reload, serializes operations
        private async Task ReloadAsyncSafe()
        {
            if (_isDisposed) return;

            try { _reloadCts?.Cancel(); } catch { }
            _reloadCts = new CancellationTokenSource();
            var ct = _reloadCts.Token;

            try
            {
                IsBusy = true;
                await ReloadAsync(ct);
            }
            catch (OperationCanceledException)
            {
                // ignored (newer reload started)
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Monthly reload failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public Task ReloadAsync() => ReloadAsync(CancellationToken.None);

        private async Task ReloadAsync(CancellationToken ct)
        {
            await _opGate.WaitAsync(ct);
            try
            {
                ct.ThrowIfCancellationRequested();

                foreach (var row in BudgetRows)
                    row.PropertyChanged -= Row_PropertyChanged;

                BudgetRows.Clear();

                var month = SelectedMonth;

                await using var db = _dbFactory();

                var plan = await db.BudgetPlans
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.UserId == _userId && p.Month == month, ct);

                MonthlyBudgetTotal = plan?.MonthlyBudget ?? 0m;

                var categories = await db.Categories
                    .AsNoTracking()
                    .Where(c => c.ItemType == ItemType.Expense || c.ItemType == ItemType.Savings)
                    .OrderBy(c => c.ItemType)
                    .ThenBy(c => c.Name)
                    .ToListAsync(ct);

                var catIds = categories.Select(c => c.Id).ToList();

                var existing = await db.MonthlyBudgets
                    .AsNoTracking()
                    .Where(mb => mb.UserId == _userId && mb.Month == month && catIds.Contains(mb.CategoryId))
                    .ToListAsync(ct);

                foreach (var c in categories)
                {
                    ct.ThrowIfCancellationRequested();

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
            finally
            {
                try { _opGate.Release(); } catch { /* ignore */ }
            }
        }

        private async Task SaveAsync()
        {
            await _opGate.WaitAsync();
            try
            {
                var month = SelectedMonth;
                var catIds = BudgetRows.Select(r => r.CategoryId).ToList();

                await using var db = _dbFactory();

                var existing = await db.MonthlyBudgets
                    .Where(mb => mb.UserId == _userId && mb.Month == month && catIds.Contains(mb.CategoryId))
                    .ToListAsync();

                db.MonthlyBudgets.RemoveRange(existing);

                foreach (var row in BudgetRows)
                {
                    if (row.Amount == 0m)
                        continue;

                    db.MonthlyBudgets.Add(new MonthlyBudget
                    {
                        UserId = _userId,
                        CategoryId = row.CategoryId,
                        Month = month,
                        EndMonth = month,
                        Amount = row.Amount
                    });
                }

                await db.SaveChangesAsync();

                BudgetRowsView.Refresh();
                NotifyRemainingChanged();
            }
            finally
            {
                try { _opGate.Release(); } catch { /* ignore */ }
            }
        }
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
