using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Wpf_Budgetplanerare.Models
{
    public class Item
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public decimal Amount {  get; set; }
        public DateTime PostingDate { get; set; } = DateTime.Today;
        public DateTime TransactionDate { get; set; }
        public ItemType ItemType { get; set; }
        public RecurrenceType RecurrenceType { get; set; }
        
        [Range(1, 12, ErrorMessage = "YearlyMonth must be between 1 and 12")]
        public MonthType? YearlyMonth { get; set; } 
        public int CategoryId { get; set; }
        public Category? Category { get; set; }
        public string? Note { get; set; }
        public bool IsYearly => RecurrenceType == RecurrenceType.Yearly;
        public bool IsMonthly => RecurrenceType == RecurrenceType.Monthly;
        public bool IsOneTime => RecurrenceType == RecurrenceType.Once;
        public bool IsRecurring => RecurrenceType != RecurrenceType.Once;

    }
}
