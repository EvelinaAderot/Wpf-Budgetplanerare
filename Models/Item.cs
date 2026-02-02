// ff — Item.cs (fix "Invalid column name ...")
// These properties are UI/recurrence helper fields. If you are NOT storing them in DB yet,
// mark them as [NotMapped] so EF stops expecting columns.
// (Fastest fix without migrations / dropping DB.)

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Wpf_Budgetplanerare.Models
{
    public class Item
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public decimal Amount { get; set; }

        public DateTime PostingDate { get; set; } = DateTime.Today;
        public DateTime TransactionDate { get; set; }

        public ItemType ItemType { get; set; }
        public RecurrenceType RecurrenceType { get; set; }

        [NotMapped]
        [Range(1, 12, ErrorMessage = "MonthlyEndMonth must be between 1 and 12")]
        public MonthType? MonthlyEndMonth { get; set; }

        [NotMapped]
        public int? MonthlyEndYear { get; set; }

        // Yearly: user picks which month the payment happens in + end year (UI helper)
        [NotMapped]
        [Range(1, 12, ErrorMessage = "YearlyMonth must be between 1 and 12")]
        public MonthType? YearlyMonth { get; set; }

        [NotMapped]
        public int? YearlyEndYear { get; set; }

        // -------------------------
        // Persisted fields
        // -------------------------
        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        public string? Note { get; set; }

        // Computed helpers
        public bool IsYearly => RecurrenceType == RecurrenceType.Yearly;
        public bool IsMonthly => RecurrenceType == RecurrenceType.Monthly;
        public bool IsOneTime => RecurrenceType == RecurrenceType.Once;
        public bool IsRecurring => RecurrenceType != RecurrenceType.Once;
    }
}
