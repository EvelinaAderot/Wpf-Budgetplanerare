using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wpf_Budgetplanerare.Models;

namespace Wpf_Budgetplanerare.Services
{
    public class RecurrenceService
    {
        public bool AppliesToMonth(Item item, int year, int month)
        {
            if (item.RecurrenceType == RecurrenceType.Monthly)
                return true;

            if (item.RecurrenceType == RecurrenceType.Yearly)
                return (int?)item.YearlyMonth == month;

            // Once
            return item.TransactionDate.Year == year && item.TransactionDate.Month == month;
        }
    }
}

