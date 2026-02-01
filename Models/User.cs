using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wpf_Budgetplanerare.Models
{
    public class User
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } =string.Empty;
        public decimal IncomeMonthly { get; set; } 
        public int WorkHoursMonthly { get; set; }

        public decimal Balance { get; set; }
        public bool Active { get; set; }

    }
}
