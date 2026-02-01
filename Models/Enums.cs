using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wpf_Budgetplanerare.Models
{

        public enum ItemType
        {
            Income = 0,
            Expense = 1,
            Savings = 2
        }

        public enum RecurrenceType
        {
            Once = 0,
            Monthly = 1,
            Yearly = 2
        }
        public enum MonthType 
        {
        January = 1,
        February = 2,
        March = 3,
        April = 4,
        May = 5,
        June= 6,
        July = 7,
        August = 8,
        September = 9,
        October = 10,
        November = 11,
        December = 12
    }
        public enum AbsenceType
        {
            Sick = 0,
            VAB = 1
        }
}
