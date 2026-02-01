using System;

namespace Wpf_Budgetplanerare.Models
{
    public class BudgetPlan
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        public DateTime Month { get; set; }

        public decimal MonthlyBudget { get; set; }
        public decimal QuarterlyBudget { get; set; }
        public decimal YearlyBudget { get; set; }
    }
}
