using System.Linq;
using Wpf_Budgetplanerare.Models;

namespace Wpf_Budgetplanerare.Data.Seed
{
    public static class Seed
    {
        public static void Initialize(BudgetDbContext context)
        {
            context.Database.EnsureCreated();

            if (!context.Users.Any())
            {
                context.Users.Add(new User
                {
                    FirstName = "FirstName",
                    LastName = "LastName",
                    IncomeMonthly = 0,
                    WorkHoursMonthly = 0,
                    Active = true 
                });

                context.SaveChanges();
            }

            var activeUsers = context.Users.Where(u => u.Active).ToList();

            if (activeUsers.Count == 0)
            {
                var firstUser = context.Users.OrderBy(u => u.Id).First();
                firstUser.Active = true;
                context.SaveChanges();
            }
            else if (activeUsers.Count > 1)
            {
                var keep = activeUsers.OrderBy(u => u.Id).First();
                foreach (var u in activeUsers.Where(x => x.Id != keep.Id))
                    u.Active = false;

                context.SaveChanges();
            }

            if (!context.Categories.Any())
            {
                context.Categories.AddRange(
                    // Income
                    new Category { Name = "Lön", ItemType = ItemType.Income },
                    new Category { Name = "Bidrag", ItemType = ItemType.Income },
                    new Category { Name = "Hobby", ItemType = ItemType.Income },
                    new Category { Name = "Spar-Uttag", ItemType = ItemType.Income },
                    new Category { Name = "Lån", ItemType = ItemType.Income },
                    new Category { Name = "Annat", ItemType = ItemType.Income },

                    // Expense
                    new Category { Name = "Mat", ItemType = ItemType.Expense },
                    new Category { Name = "Hus & Drift", ItemType = ItemType.Expense },
                    new Category { Name = "Transport", ItemType = ItemType.Expense },
                    new Category { Name = "Fritid", ItemType = ItemType.Expense },
                    new Category { Name = "Barn", ItemType = ItemType.Expense },
                    new Category { Name = "Annuiteter", ItemType = ItemType.Expense },
                    new Category { Name = "Abonnemang", ItemType = ItemType.Expense },
                    new Category { Name = "Övrigt", ItemType = ItemType.Expense },

                    // Savings
                    new Category { Name = "Resa", ItemType = ItemType.Savings },
                    new Category { Name = "Nödfall", ItemType = ItemType.Savings },
                    new Category { Name = "Investering", ItemType = ItemType.Savings },
                    new Category { Name = "Pension", ItemType = ItemType.Savings },
                    new Category { Name = "Spar", ItemType = ItemType.Savings },              
                    new Category { Name = "ÖverBlivet", ItemType = ItemType.Savings }

                );

                context.SaveChanges();
            }

        }
    }
}
