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

        /* ---------- Item type ---------- */

        public ObservableCollection<ItemType> ItemTypes { get; }
            = new(Enum.GetValues(typeof(ItemType)).Cast<ItemType>()
                    .Where(t => t != ItemType.Income));

        private ItemType _selectedItemType;
        public ItemType SelectedItemType
        {
            get => _selectedItemType;
            set
            {
                if (SetProperty(ref _selectedItemType, value))
                    LoadCategories();
            }
        }

        /* ---------- Categories ---------- */

        public ObservableCollection<Category> FilteredCategories { get; } = new();

        private Category? _selectedCategory;
        public Category? SelectedCategory
        {
            get => _selectedCategory;
            set => SetProperty(ref _selectedCategory, value);
        }

        private async void LoadCategories()
        {
            FilteredCategories.Clear();

            var cats = await _db.Categories
                .Where(c => c.ItemType == SelectedItemType)
                .OrderBy(c => c.Name)
                .ToListAsync();

            foreach (var c in cats)
                FilteredCategories.Add(c);

            SelectedCategory = FilteredCategories.FirstOrDefault();
        }

        /* ---------- Dates ---------- */

        public DateTime SelectedDate { get; set; } = DateTime.Today;

        /* ---------- Recurrence ---------- */

        public ObservableCollection<RecurrenceType> RecurrenceTypes { get; }
            = new(Enum.GetValues(typeof(RecurrenceType)).Cast<RecurrenceType>());

        private RecurrenceType _selectedRecurrenceType = RecurrenceType.Once;
        public RecurrenceType SelectedRecurrenceType
        {
            get => _selectedRecurrenceType;
            set => SetProperty(ref _selectedRecurrenceType, value);
        }

        private DateTime? _endMonth;
        public DateTime? EndMonth
        {
            get => _endMonth;
            set => SetProperty(ref _endMonth, value);
        }

        /* ---------- Budget source ---------- */

        public ObservableCollection<string> BudgetSources { get; }
            = new() { "Monthly", "Quarterly", "Yearly" };

        private string _selectedBudgetSource = "Monthly";
        public string SelectedBudgetSource
        {
            get => _selectedBudgetSource;
            set => SetProperty(ref _selectedBudgetSource, value);
        }

        /* ---------- Amount ---------- */

        private decimal _amount;
        public decimal Amount
        {
            get => _amount;
            set => SetProperty(ref _amount, value);
        }

        /* ---------- Commands ---------- */

        public ICommand SaveCommand { get; }

        public ExpenseEntryViewModel(BudgetDbContext db, int userId)
        {
            _db = db;
            _userId = userId;

            SaveCommand = new RelayCommand(async () => await SaveAsync());

            SelectedItemType = ItemType.Expense;
        }

        /* ---------- Save logic ---------- */

        private async Task SaveAsync()
        {
            if (Amount <= 0 || SelectedCategory == null)
                return;

            var item = new Item
            {
                UserId = _userId,
                Amount = Amount,
                PostingDate = DateTime.Today,
                TransactionDate = SelectedDate,
                ItemType = SelectedItemType,
                RecurrenceType = SelectedRecurrenceType,
                CategoryId = SelectedCategory.Id,
                YearlyMonth = SelectedRecurrenceType == RecurrenceType.Yearly
                    ? (MonthType?)SelectedDate.Month
                    : null
            };

            _db.Items.Add(item);

            // 🔻 Deduct from selected budget
            await DeductFromBudgetAsync(SelectedBudgetSource, Amount);

            await _db.SaveChangesAsync();

            ResetForm();
        }

        /* ---------- Budget deduction ---------- */

        private async Task DeductFromBudgetAsync(string source, decimal amount)
        {
            var today = DateTime.Today;
            DateTime periodStart = source switch
            {
                "Monthly" => new DateTime(today.Year, today.Month, 1),
                "Quarterly" => new DateTime(today.Year, ((today.Month - 1) / 3) * 3 + 1, 1),
                "Yearly" => new DateTime(today.Year, 1, 1),
                _ => today
            };

            var plan = await _db.BudgetPlans
                .FirstOrDefaultAsync(p => p.UserId == _userId && p.Month == periodStart);

            if (plan == null)
                return;

            switch (source)
            {
                case "Monthly":
                    plan.MonthlyBudget -= amount;
                    break;
                case "Quarterly":
                    plan.QuarterlyBudget -= amount;
                    break;
                case "Yearly":
                    plan.YearlyBudget -= amount;
                    break;
            }
        }

        private void ResetForm()
        {
            Amount = 0;
            SelectedDate = DateTime.Today;
            SelectedRecurrenceType = RecurrenceType.Once;
            EndMonth = null;
        }
    }
}
