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
            var today = DateTime.Today;

            var items = _db.Items
                .AsNoTracking()
                .Include(i => i.Category)
                .Where(i => i.UserId == _userId &&
                            i.TransactionDate.Date <= today)
                .OrderByDescending(i => i.TransactionDate)
                .ToList();

            Transactions.Clear();

            foreach (var i in items)
            {
                Transactions.Add(new TransactionRowVM
                {
                    Id = i.Id,
                    CategoryName = i.Category?.Name ?? "",
                    ItemType = i.ItemType.ToString(),
                    TransactionDate = i.TransactionDate,
                    Amount = i.Amount,
                    Note = i.Note
                });
            }
        }

        public void DeleteTransaction(TransactionRowVM row)
        {
            var entity = _db.Items.FirstOrDefault(i => i.Id == row.Id);
            if (entity == null)
                return;

            _db.Items.Remove(entity);
            _db.SaveChanges();

            Transactions.Remove(row);
        }
    }

    public class TransactionRowVM
    {
        public int Id { get; set; }
        public string CategoryName { get; set; } = "";
        public string ItemType { get; set; } = "";
        public DateTime TransactionDate { get; set; }
        public decimal Amount { get; set; }
        public string? Note { get; set; }
    }
}
