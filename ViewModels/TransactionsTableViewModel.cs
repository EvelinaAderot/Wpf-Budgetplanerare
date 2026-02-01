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

        public ObservableCollection<TransactionRowVM> Transactions { get; }
            = new();

        public TransactionsTableViewModel(BudgetDbContext db, int userId)
        {
            _db = db;
            _userId = userId;

            Load();
        }

        private void Load()
        {
            var items = _db.Items
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
                    Amount = i.Amount
                });
            }
        }
    }

    public class TransactionRowVM
    {
        public string CategoryName { get; set; } = "";
        public string ItemType { get; set; } = "";
        public DateTime TransactionDate { get; set; }
        public decimal Amount { get; set; }
    }
}
