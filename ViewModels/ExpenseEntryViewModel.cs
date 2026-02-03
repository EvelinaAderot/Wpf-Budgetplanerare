using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Wpf_Budgetplanerare.Data;
using Wpf_Budgetplanerare.Models;
using Wpf_Budgetplanerare.ViewModels.Base;

namespace Wpf_Budgetplanerare.ViewModels
{
    public class ExpenseEntryViewModel : ViewModelBase
    {
        private readonly BudgetDbContext _db;
        private readonly int _userId;

        public ObservableCollection<ItemType> ItemTypes { get; } =
            new ObservableCollection<ItemType>(Enum.GetValues(typeof(ItemType)).Cast<ItemType>());

        public ObservableCollection<ItemType> ExpenseEntryItemTypes { get; } =
            new ObservableCollection<ItemType>(
        Enum.GetValues(typeof(ItemType))
            .Cast<ItemType>()
            .Where(t => t != ItemType.Income)
            );

        public ObservableCollection<RecurrenceType> RecurrenceTypes { get; } =
            new ObservableCollection<RecurrenceType>(Enum.GetValues(typeof(RecurrenceType)).Cast<RecurrenceType>());

        public ObservableCollection<MonthType> MonthTypes { get; } =
            new ObservableCollection<MonthType>(Enum.GetValues(typeof(MonthType)).Cast<MonthType>());

        public ObservableCollection<Category> AllCategories { get; } = new();
        public ObservableCollection<Category> FilteredCategories { get; } = new();

        public ObservableCollection<string> BudgetSources { get; } =
            new ObservableCollection<string>(new[] { "Monthly", "Quarterly", "Yearly" });

        private string _selectedBudgetSource = "Monthly";
        public string SelectedBudgetSource
        {
            get => _selectedBudgetSource;
            set => SetProperty(ref _selectedBudgetSource, value);
        }

        private ItemType _selectedItemType = ItemType.Expense;
        public ItemType SelectedItemType
        {
            get => _selectedItemType;
            set
            {
                if (SetProperty(ref _selectedItemType, value))
                    ApplyCategoryFilter();
            }
        }

        private Category? _selectedCategory;
        public Category? SelectedCategory
        {
            get => _selectedCategory;
            set => SetProperty(ref _selectedCategory, value);
        }

        private DateTime _selectedDate = DateTime.Today;
        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                if (SetProperty(ref _selectedDate, value))
                {
                    // Keep defaults sane if user changes date
                    if (SelectedRecurrenceType == RecurrenceType.Monthly)
                    {
                        EnsureMonthlyMinEndYear();
                        EnsureMonthlyEndNotBeforeStart();
                    }
                    else if (SelectedRecurrenceType == RecurrenceType.Yearly)
                    {
                        EnsureYearlyMinEndYear();
                    }
                }
            }
        }

        private RecurrenceType _selectedRecurrenceType = RecurrenceType.Once;
        public RecurrenceType SelectedRecurrenceType
        {
            get => _selectedRecurrenceType;
            set
            {
                if (SetProperty(ref _selectedRecurrenceType, value))
                {
                    if (_selectedRecurrenceType == RecurrenceType.Monthly)
                    {
                        // Monthly: default end = same month/year as selected date
                        MonthlyEndMonth = (MonthType)SelectedDate.Month;
                        MonthlyEndYear = SelectedDate.Year;
                        EnsureMonthlyMinEndYear();
                        EnsureMonthlyEndNotBeforeStart();
                    }
                    else if (_selectedRecurrenceType == RecurrenceType.Yearly)
                    {
                        // Yearly: default payment month = selected date month
                        YearlyPaymentMonth = (MonthType)SelectedDate.Month;
                        EnsureYearlyMinEndYear();
                    }

                    OnPropertyChanged(nameof(MonthlyYearMinHint));
                    OnPropertyChanged(nameof(YearlyYearMinHint));
                }
            }
        }

        // -------------------------
        // MONTHLY OPTIONS (end month/year)
        // -------------------------
        private MonthType _monthlyEndMonth = (MonthType)DateTime.Today.Month;
        public MonthType MonthlyEndMonth
        {
            get => _monthlyEndMonth;
            set
            {
                if (SetProperty(ref _monthlyEndMonth, value))
                    EnsureMonthlyEndNotBeforeStart();
            }
        }

        private int _monthlyEndYear = DateTime.Today.Year;
        public int MonthlyEndYear
        {
            get => _monthlyEndYear;
            set
            {
                var clamped = value < MonthlyMinEndYear ? MonthlyMinEndYear : value;
                if (SetProperty(ref _monthlyEndYear, clamped))
                {
                    EnsureMonthlyEndNotBeforeStart();
                    OnPropertyChanged(nameof(MonthlyYearMinHint));
                }
            }
        }

        public int MonthlyMinEndYear => SelectedDate.Year;
        public string MonthlyYearMinHint => $"Min end year: {MonthlyMinEndYear}";

        public ICommand IncreaseMonthlyEndYearCommand { get; }
        public ICommand DecreaseMonthlyEndYearCommand { get; }


        private MonthType _yearlyPaymentMonth = (MonthType)DateTime.Today.Month;
        public MonthType YearlyPaymentMonth
        {
            get => _yearlyPaymentMonth;
            set => SetProperty(ref _yearlyPaymentMonth, value);
        }

        private int _yearlyEndYear = DateTime.Today.Year + 1;
        public int YearlyEndYear
        {
            get => _yearlyEndYear;
            set
            {
                var clamped = value < YearlyMinEndYear ? YearlyMinEndYear : value;
                if (SetProperty(ref _yearlyEndYear, clamped))
                    OnPropertyChanged(nameof(YearlyYearMinHint));
            }
        }

        public int YearlyMinEndYear => DateTime.Today.Year + 1;
        public string YearlyYearMinHint => $"Min end year: {YearlyMinEndYear}";

        public ICommand IncreaseYearlyEndYearCommand { get; }
        public ICommand DecreaseYearlyEndYearCommand { get; }

        private decimal _amount;
        public decimal Amount
        {
            get => _amount;
            set => SetProperty(ref _amount, value);
        }

        private string? _note;
        public string? Note
        {
            get => _note;
            set => SetProperty(ref _note, value);
        }

        public ICommand SaveCommand { get; }

        public ExpenseEntryViewModel(BudgetDbContext db, int userId)
        {
            _db = db;
            _userId = userId;

            IncreaseMonthlyEndYearCommand = new RelayCommand(() => MonthlyEndYear = MonthlyEndYear + 1);
            DecreaseMonthlyEndYearCommand = new RelayCommand(() => MonthlyEndYear = MonthlyEndYear - 1);

            IncreaseYearlyEndYearCommand = new RelayCommand(() => YearlyEndYear = YearlyEndYear + 1);
            DecreaseYearlyEndYearCommand = new RelayCommand(() => YearlyEndYear = YearlyEndYear - 1);

            SaveCommand = new RelayCommand(async () => await SaveAsync());

            EnsureMonthlyMinEndYear();
            EnsureYearlyMinEndYear();

            _ = LoadCategoriesAsync();
        }

        private async Task LoadCategoriesAsync()
        {
            AllCategories.Clear();
            FilteredCategories.Clear();

            var cats = await _db.Categories
                .OrderBy(c => c.ItemType)
                .ThenBy(c => c.Name)
                .ToListAsync();

            foreach (var c in cats)
                AllCategories.Add(c);

            ApplyCategoryFilter();
        }

        private void ApplyCategoryFilter()
        {
            FilteredCategories.Clear();

            var filtered = AllCategories
                .Where(c => c.ItemType == SelectedItemType)
                .OrderBy(c => c.Name)
                .ToList();

            foreach (var c in filtered)
                FilteredCategories.Add(c);

            if (SelectedCategory == null || SelectedCategory.ItemType != SelectedItemType)
                SelectedCategory = FilteredCategories.FirstOrDefault();
        }

        private void EnsureMonthlyMinEndYear()
        {
            if (MonthlyEndYear < MonthlyMinEndYear)
                MonthlyEndYear = MonthlyMinEndYear;
        }

        private void EnsureYearlyMinEndYear()
        {
            if (YearlyEndYear < YearlyMinEndYear)
                YearlyEndYear = YearlyMinEndYear;
        }

  
        private void EnsureMonthlyEndNotBeforeStart()
        {
            var startMonth = SelectedDate.Month;

            if (MonthlyEndYear == SelectedDate.Year && (int)MonthlyEndMonth < startMonth)
            {
                MonthlyEndYear = SelectedDate.Year + 1;
            }
        }

        private async Task SaveAsync()
        {
            if (SelectedCategory == null)
                return;

            if (Amount <= 0m)
                return;

            var baseNote = string.IsNullOrWhiteSpace(Note) ? null : Note.Trim();
            var postingDate = DateTime.Today;

            // -------------------------
            // MONTHLY => skapa en rad per månad
            // -------------------------
            if (SelectedRecurrenceType == RecurrenceType.Monthly)
            {
                EnsureMonthlyMinEndYear();
                EnsureMonthlyEndNotBeforeStart();

                var start = new DateTime(SelectedDate.Year, SelectedDate.Month, 1);
                var end = new DateTime(MonthlyEndYear, (int)MonthlyEndMonth, 1);

                var month = start;
                while (month <= end)
                {
                    var item = new Item
                    {
                        UserId = _userId,
                        CategoryId = SelectedCategory.Id,
                        ItemType = SelectedItemType,
                        RecurrenceType = RecurrenceType.Once,
                        TransactionDate = month.Date,
                        PostingDate = postingDate,
                        Amount = Amount,
                        Note = baseNote,

                        MonthlyEndMonth = null,
                        MonthlyEndYear = null,
                        YearlyMonth = null,
                        YearlyEndYear = null
                    };

                    _db.Items.Add(item);
                    month = month.AddMonths(1);
                }

                await _db.SaveChangesAsync();

                Amount = 0m;
                Note = null;
                SelectedDate = DateTime.Today;

                MonthlyEndMonth = (MonthType)SelectedDate.Month;
                MonthlyEndYear = SelectedDate.Year;
                EnsureMonthlyMinEndYear();
                EnsureMonthlyEndNotBeforeStart();

                return;
            }

            // -------------------------
            // YEARLY => skapa en rad per år
            // -------------------------
            if (SelectedRecurrenceType == RecurrenceType.Yearly)
            {
                EnsureYearlyMinEndYear();

                // Startår: från vald "SelectedDate"
                var startYear = SelectedDate.Year;
                var endYear = YearlyEndYear;

                // Betalningsmånad: YearlyPaymentMonth
                var paymentMonth = (int)YearlyPaymentMonth;

                for (int year = startYear; year <= endYear; year++)
                {
                    var transactionDate = new DateTime(year, paymentMonth, 1);

                    // Om du vill vara extra strikt:
                    // - hoppa över om transactionDate är före SelectedDate (t.ex. om SelectedDate är senare samma år)
                    // I ditt UI sätter du ofta YearlyPaymentMonth = SelectedDate.Month,
                    // men användaren kan ändra den, så detta skyddar mot "tidigare än start".
                    if (transactionDate.Date < new DateTime(SelectedDate.Year, SelectedDate.Month, 1))
                        continue;

                    var item = new Item
                    {
                        UserId = _userId,
                        CategoryId = SelectedCategory.Id,
                        ItemType = SelectedItemType,
                        RecurrenceType = RecurrenceType.Once,
                        TransactionDate = transactionDate.Date,
                        PostingDate = postingDate,
                        Amount = Amount,
                        Note = baseNote,

                        MonthlyEndMonth = null,
                        MonthlyEndYear = null,
                        YearlyMonth = null,
                        YearlyEndYear = null
                    };

                    _db.Items.Add(item);
                }

                await _db.SaveChangesAsync();

                Amount = 0m;
                Note = null;
                SelectedDate = DateTime.Today;

                YearlyPaymentMonth = (MonthType)SelectedDate.Month;
                EnsureYearlyMinEndYear();

                return;
            }

            // -------------------------
            // ONCE => som tidigare, en rad
            // -------------------------
            var singleItem = new Item
            {
                UserId = _userId,
                CategoryId = SelectedCategory.Id,
                ItemType = SelectedItemType,
                RecurrenceType = RecurrenceType.Once,
                TransactionDate = SelectedDate.Date,
                PostingDate = postingDate,
                Amount = Amount,
                Note = baseNote,

                MonthlyEndMonth = null,
                MonthlyEndYear = null,
                YearlyMonth = null,
                YearlyEndYear = null
            };

            _db.Items.Add(singleItem);
            await _db.SaveChangesAsync();

            Amount = 0m;
            Note = null;
            SelectedDate = DateTime.Today;
        }

    }
}


