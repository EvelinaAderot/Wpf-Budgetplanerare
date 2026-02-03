using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.EntityFrameworkCore;
using Wpf_Budgetplanerare.Data;
using Wpf_Budgetplanerare.Models;
using Wpf_Budgetplanerare.ViewModels.Base;

namespace Wpf_Budgetplanerare.ViewModels
{
    public enum BudgetPeriodKind
    {
        Monthly,
        Quarterly,
        Yearly
    }

    public class BudgetProgressViewModel : ViewModelBase, IDisposable
    {
        private readonly Func<BudgetDbContext> _dbFactory;
        private readonly int _userId;
        private readonly BudgetPeriodKind _periodKind;

        private readonly INotifyPropertyChanged? _periodSourceVm;
        private readonly string? _periodSourcePropertyName;

        private readonly ObservableCollection<BudgetProgressRow> _rows = new();
        private int _index;

        private readonly SemaphoreSlim _opGate = new(1, 1);
        private CancellationTokenSource? _reloadCts;

        private readonly DispatcherTimer _rotateTimer;
        private bool _autoRotateEnabled = true;
        private int _autoRotateSeconds = 5;
        private bool _isDisposed;

        public ICommand PrevCommand { get; }
        public ICommand NextCommand { get; }

        public BudgetProgressViewModel(
            Func<BudgetDbContext> dbFactory,
            int userId,
            BudgetPeriodKind periodKind,
            INotifyPropertyChanged? periodSourceVm = null,
            string? periodSourcePropertyName = "SelectedMonth")
        {
            _dbFactory = dbFactory;
            _userId = userId;
            _periodKind = periodKind;

            _periodSourceVm = periodSourceVm;
            _periodSourcePropertyName = periodSourcePropertyName;

            PrevCommand = new RelayCommand(() => Move(-1));
            NextCommand = new RelayCommand(() => Move(+1));

            if (_periodSourceVm != null && !string.IsNullOrWhiteSpace(_periodSourcePropertyName))
                _periodSourceVm.PropertyChanged += PeriodSource_PropertyChanged;

            _rotateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(_autoRotateSeconds)
            };
            _rotateTimer.Tick += (_, __) =>
            {
                if (_rows.Count > 1 && _autoRotateEnabled)
                    Move(+1, fromAutoRotate: true);
            };

            _ = ReloadAsyncSafe();
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            try { _periodSourceVm!.PropertyChanged -= PeriodSource_PropertyChanged; } catch { }
            try { _reloadCts?.Cancel(); } catch { }
            try { _rotateTimer.Stop(); } catch { }
        }

        private async void PeriodSource_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == _periodSourcePropertyName)
            {
                OnPropertyChanged(nameof(PeriodText));
                await ReloadAsyncSafe();
            }
        }

        private async Task ReloadAsyncSafe()
        {
            try
            {
                _reloadCts?.Cancel();
                _reloadCts = new CancellationTokenSource();
                await ReloadAsync(_reloadCts.Token);
            }
            catch { }
        }

        public string HeaderText => _periodKind switch
        {
            BudgetPeriodKind.Monthly => "Budget progress (Monthly)",
            BudgetPeriodKind.Quarterly => "Budget progress (Quarterly)",
            BudgetPeriodKind.Yearly => "Budget progress (Yearly)",
            _ => "Budget progress"
        };

        public string PeriodText
        {
            get
            {
                var start = GetPeriodStart();
                return _periodKind switch
                {
                    BudgetPeriodKind.Monthly => start.ToString("MMMM yyyy"),
                    BudgetPeriodKind.Quarterly => $"Q{((start.Month - 1) / 3) + 1} {start.Year}",
                    BudgetPeriodKind.Yearly => start.Year.ToString(),
                    _ => ""
                };
            }
        }

        public string CategoryName => Current?.CategoryName ?? "(No data)";
        public string CategoryTypeText => Current?.ItemType.ToString() ?? "";

        public string BudgetText => $"{Current?.Budget ?? 0:N2} kr";
        public string SpentText => $"{Current?.Spent ?? 0:N2} kr";
        public string RemainingText => $"{Current?.Remaining ?? 0:N2} kr";

        public Brush RemainingBrush =>
            (Current?.Remaining ?? 0) < 0
                ? Brushes.IndianRed
                : Brushes.ForestGreen;

        public double ProgressRatio =>
            Current == null || Current.Budget <= 0
                ? (Current?.Spent > 0 ? 1 : 0)
                : Math.Min(1, (double)(Current.Spent / Current.Budget));

        public Brush ProgressBrush =>
            (Current?.Budget > 0 && Current.Spent > Current.Budget)
                ? Brushes.IndianRed
                : Brushes.SteelBlue;

        private BudgetProgressRow? Current =>
            _rows.Count == 0 ? null : _rows[_index];

        private void Move(int delta, bool fromAutoRotate = false)
        {
            if (_rows.Count == 0) return;
            _index = (_index + delta + _rows.Count) % _rows.Count;
            RaiseAll();

            if (!fromAutoRotate)
            {
                _rotateTimer.Stop();
                if (_autoRotateEnabled) _rotateTimer.Start();
            }
        }

        private void RaiseAll()
        {
            OnPropertyChanged(nameof(CategoryName));
            OnPropertyChanged(nameof(CategoryTypeText));
            OnPropertyChanged(nameof(BudgetText));
            OnPropertyChanged(nameof(SpentText));
            OnPropertyChanged(nameof(RemainingText));
            OnPropertyChanged(nameof(RemainingBrush));
            OnPropertyChanged(nameof(ProgressRatio));
            OnPropertyChanged(nameof(ProgressBrush));
        }

        private async Task ReloadAsync(CancellationToken ct)
        {
            await _opGate.WaitAsync(ct);
            try
            {
                _rows.Clear();
                _index = 0;

                var start = GetPeriodStart();
                var periodEnd = _periodKind switch
                {
                    BudgetPeriodKind.Monthly => start.AddMonths(1),
                    BudgetPeriodKind.Quarterly => start.AddMonths(3),
                    BudgetPeriodKind.Yearly => start.AddYears(1),
                    _ => start.AddMonths(1)
                };


                var today = DateTime.Today;
                var spentEndExclusive = periodEnd <= today.AddDays(1) ? periodEnd : today.AddDays(1);

                await using var db = _dbFactory();

                var budgets = await db.MonthlyBudgets
                    .Include(b => b.Category)
                    .Where(b => b.UserId == _userId &&
                                b.Category != null &&
                                (b.Category.ItemType == ItemType.Expense ||
                                 b.Category.ItemType == ItemType.Savings) &&
                                b.Month >= start && b.Month < periodEnd)
                    .ToListAsync(ct);

                var items = await db.Items
                    .Include(i => i.Category)
                    .Where(i => i.UserId == _userId &&
                                i.TransactionDate >= start &&
                                i.TransactionDate < spentEndExclusive &&   // <-- changed
                                (i.ItemType == ItemType.Expense ||
                                 i.ItemType == ItemType.Savings))
                    .ToListAsync(ct);

                var budgetMap = budgets
                    .GroupBy(b => b.CategoryId)
                    .ToDictionary(
                        g => g.Key,
                        g => new
                        {
                            g.First().Category!.Name,
                            g.First().Category!.ItemType,
                            Budget = g.Sum(x => x.Amount)
                        });

                var spentMap = items
                    .GroupBy(i => i.CategoryId)
                    .ToDictionary(
                        g => g.Key,
                        g => new
                        {
                            g.First().Category!.Name,
                            g.First().Category!.ItemType,
                            Spent = g.Sum(x => x.Amount)
                        });

                var allIds = budgetMap.Keys.Union(spentMap.Keys);

                foreach (var id in allIds)
                {
                    budgetMap.TryGetValue(id, out var b);
                    spentMap.TryGetValue(id, out var s);

                    var budget = b?.Budget ?? 0;
                    var spent = s?.Spent ?? 0;

                    if (budget == 0 && spent == 0)
                        continue;

                    _rows.Add(new BudgetProgressRow
                    {
                        CategoryId = id,
                        CategoryName = b?.Name ?? s?.Name ?? "",
                        ItemType = b?.ItemType ?? s?.ItemType ?? ItemType.Expense,
                        Budget = budget,
                        Spent = spent,
                        Remaining = budget - spent,
                        ProgressPercent = budget <= 0 ? 100 : (spent / budget) * 100
                    });
                }

                RaiseAll();

                if (_rows.Count > 1 && _autoRotateEnabled)
                    _rotateTimer.Start();
            }
            finally
            {
                _opGate.Release();
            }
        }

        private DateTime GetPeriodStart()
        {
            var now = DateTime.Now;
            return new DateTime(now.Year, now.Month, 1);
        }

        private class BudgetProgressRow
        {
            public int CategoryId { get; set; }
            public string CategoryName { get; set; } = "";
            public ItemType ItemType { get; set; }
            public decimal Budget { get; set; }
            public decimal Spent { get; set; }
            public decimal Remaining { get; set; }
            public decimal ProgressPercent { get; set; }
        }
    }
}
