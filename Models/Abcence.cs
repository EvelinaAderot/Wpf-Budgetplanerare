using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wpf_Budgetplanerare.Models
{
    public class Absence
    {
        public int Id { get; set; }

        public DateTime DateInput { get; set; } = DateTime.Today;

        public DateTime DateStart { get; set; }
        public DateTime DateEnd { get; set; }

        public AbsenceType Type { get; set; }

        public int Hours { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }
    }
}
