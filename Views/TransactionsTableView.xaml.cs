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
    }
}
