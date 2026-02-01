using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wpf_Budgetplanerare.Data.Repositories.Interfaces;
using Wpf_Budgetplanerare.Models;

namespace Wpf_Budgetplanerare.Services
{

    public class SavingsService
    {
        private readonly IItemRepository _itemRepository;

        public SavingsService(IItemRepository itemRepository)
        {
            _itemRepository = itemRepository;
        }

        public async Task DepositToSavingsAsync(
            int userId,
            int savingsCategoryId,
            decimal amount,
            DateTime transactionDate)
        {
            if (amount <= 0)
                throw new ArgumentException("Amount must be > 0.", nameof(amount));

            var item = new Item
            {
                UserId = userId,
                Amount = amount,
                PostingDate = DateTime.Today,
                TransactionDate = transactionDate.Date,
                ItemType = ItemType.Savings,
                RecurrenceType = RecurrenceType.Once,
                CategoryId = savingsCategoryId
            };

            await _itemRepository.AddAsync(item);
        }

        public async Task WithdrawFromSavingsAsync(
            int userId,
            int savingsCategoryId,
            int withdrawalIncomeCategoryId,
            decimal amount,
            DateTime transactionDate)
        {
            if (amount <= 0)
                throw new ArgumentException("Amount must be > 0.", nameof(amount));

            var incomeItem = new Item
            {
                UserId = userId,
                Amount = amount,
                PostingDate = DateTime.Today,
                TransactionDate = transactionDate.Date,
                ItemType = ItemType.Income,
                RecurrenceType = RecurrenceType.Once,
                CategoryId = withdrawalIncomeCategoryId
            };

            var savingsItem = new Item
            {
                UserId = userId,
                Amount = -amount,
                PostingDate = DateTime.Today,
                TransactionDate = transactionDate.Date,
                ItemType = ItemType.Savings,
                RecurrenceType = RecurrenceType.Once,
                CategoryId = savingsCategoryId
            };

            await _itemRepository.AddAsync(incomeItem);
            await _itemRepository.AddAsync(savingsItem);
        }
    }
}
