using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Wpf_Budgetplanerare.Models;
using Wpf_Budgetplanerare.Data.Repositories.Interfaces;

namespace Wpf_Budgetplanerare.Data.Repositories.Implementations
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly BudgetDbContext con;

        public CategoryRepository(BudgetDbContext context)
        {
            con = context;
        }

        public async Task<List<Category>> GetAllAsync()
        {
            return await con.Categories.ToListAsync();
        }

        public async Task<Category?> GetByIdAsync(int id)
        {
            return await con.Categories.FindAsync(id);
        }

        public async Task<List<Category>> GetByItemTypeAsync(ItemType itemType)
        {
            return await con.Categories
                .Where(c => c.ItemType == itemType)
                .ToListAsync();
        }

        public async Task AddAsync(Category category)
        {
            con.Categories.Add(category);
            await con.SaveChangesAsync();
        }

        public async Task UpdateAsync(Category category)
        {
            con.Categories.Update(category);
            await con.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var category = await con.Categories.FindAsync(id);
            if (category != null)
            {
                con.Categories.Remove(category);
                await con.SaveChangesAsync();
            }
        }
    }
}
