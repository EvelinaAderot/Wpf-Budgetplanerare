using System.Windows;
using System.Windows.Controls;
using Wpf_Budgetplanerare.Data;
using Wpf_Budgetplanerare.ViewModels;

namespace Wpf_Budgetplanerare.Views
{
    public partial class TransactionsTableView : UserControl
    {
        public TransactionsTableView()
        {
            InitializeComponent();
        }

        // Call this after creating the control
        public void Init(BudgetDbContext db, int userId)
        {
            DataContext = new TransactionsTableViewModel(db, userId);
        }

        private void NoteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is not null)
            {
                var noteProp = btn.DataContext.GetType().GetProperty("Note");
                var note = noteProp?.GetValue(btn.DataContext) as string;

                if (!string.IsNullOrWhiteSpace(note))
                {
                    MessageBox.Show(note, "note", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn)
                return;

            if (btn.DataContext is not TransactionRowVM row)
                return;

            var result = MessageBox.Show(
                $"Are you sure you want to delete this transaction?\n\n" +
                $"{row.CategoryName}\n{row.TransactionDate:yyyy-MM-dd}\n{row.Amount:N2} kr",
                "Confirm delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            if (DataContext is TransactionsTableViewModel vm)
            {
                vm.DeleteTransaction(row);
            }
        }
    }
}

