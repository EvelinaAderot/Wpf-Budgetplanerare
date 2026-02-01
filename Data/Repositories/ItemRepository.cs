using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wpf_Budgetplanerare.Data.Repositories.Interfaces;
using Wpf_Budgetplanerare.Models;
using Microsoft.EntityFrameworkCore;

namespace Wpf_Budgetplanerare.Data.Repositories
{
    internal class ItemRepository : IItemRepository 
    {
        private readonly BudgetDbContext con;

        public ItemRepository(BudgetDbContext context)
        {
            con = context;
        }
        public async Task<List<Item>> GetAllAsync()
        {
            return await con.Items
                .Include(i => i.Category)
                .Include(i => i.User)
                .ToListAsync();
        }
        public async Task<Item?> GetByIdAsync(int id)
        {
            return await con.Items
                .Include(i => i.Category)
                .Include(i => i.User)
                .FirstOrDefaultAsync(i => i.Id == id);
        }
        public async Task<List<Item>> GetByUserIdAsync(int userId)
        {
            return await con.Items
                .Where(i => i.UserId == userId)
                .Include(i => i.Category)
                .ToListAsync();
        }

        public async Task<List<Item>> GetByUserIdAndMonthAsync(int userId, int year, int month)
        {
            return await con.Items
                .Where(i =>
                    i.UserId == userId &&
                    (
                        (i.RecurrenceType == RecurrenceType.Monthly) ||
                        (i.RecurrenceType == RecurrenceType.Yearly && (int?)i.YearlyMonth == month) ||
                        (i.RecurrenceType == RecurrenceType.Once &&
                         i.TransactionDate.Year == year &&
                         i.TransactionDate.Month == month)
                    ))
                .Include(i => i.Category)
                .ToListAsync();
        }

        public async Task AddAsync(Item item)
        {
            con.Items.Add(item);
            await con.SaveChangesAsync();
        }

        public async Task UpdateAsync(Item item)
        {
            con.Items.Update(item);
            await con.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var item = await con.Items.FindAsync(id);
            if (item != null)
            {
                con.Items.Remove(item);
                await con.SaveChangesAsync();
            }
        }

    }
}
