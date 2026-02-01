using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wpf_Budgetplanerare.Models
{
    public class MonthClose
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        public DateTime Month { get; set; }

        public DateTime ClosedAt { get; set; } = DateTime.UtcNow;
    }
}