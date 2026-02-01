using Wpf_Budgetplanerare.Models;

namespace Wpf_Budgetplanerare.Models
{
    public class MonthlyBudget
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        public DateTime Month { get; set; }

        public decimal Amount { get; set; }
        public DateTime EndMonth { get; set; }


    }
}
