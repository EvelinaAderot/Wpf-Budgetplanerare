using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wpf_Budgetplanerare.Models
{
    public class Category
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public ItemType ItemType { get; set; }

        public decimal BudgetMonthly { get; set; } = 0m;
        // Navigation (EF Core)
        public ICollection<Item> Items { get; set; } = new List<Item>();
    }
}
