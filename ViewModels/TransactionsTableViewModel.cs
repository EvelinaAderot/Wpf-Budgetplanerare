// ff — TransactionsTableViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Wpf_Budgetplanerare.Data;
using Wpf_Budgetplanerare.ViewModels.Base;

namespace Wpf_Budgetplanerare.ViewModels
{
    public class TransactionsTableViewModel : ViewModelBase
    {
        private readonly BudgetDbContext _db;
        private readonly int _userId;

        public ObservableCollection<TransactionRowVM> Transactions { get; } = new();

        public TransactionsTableViewModel(BudgetDbContext db, int userId)
        {
            _db = db;
            _userId = userId;

            Load();
        }

        private void Load()
        {
            // NOTE:
            // PostingDate is NOT nullable in your model (DateTime), so checking != null is unnecessary.
            // This loads ALL items for the user and shows them in the table.
            var items = _db.Items
                .AsNoTracking()
                .Include(i => i.Category)
                .Where(i => i.UserId == _userId)
                .OrderByDescending(i => i.TransactionDate)
                .ToList();

            Transactions.Clear();

            foreach (var i in items)
            {
                Transactions.Add(new TransactionRowVM
                {
                    CategoryName = i.Category?.Name ?? "",
                    ItemType = i.ItemType.ToString(),
                    TransactionDate = i.TransactionDate,
                    Amount = i.Amount,
                    Note = i.Note
                });
            }

            // Optional debug (remove later)
            // System.Diagnostics.Debug.WriteLine($"Loaded transactions: {Transactions.Count} (user {_userId})");
        }
    }

    public class TransactionRowVM
    {
        public string CategoryName { get; set; } = "";
        public string ItemType { get; set; } = "";
        public DateTime TransactionDate { get; set; }
        public decimal Amount { get; set; }
        public string? Note { get; set; }
    }
}
