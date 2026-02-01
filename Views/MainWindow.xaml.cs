using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using Wpf_Budgetplanerare.Data;
using Wpf_Budgetplanerare.Data.Repositories.Implementations;
using Wpf_Budgetplanerare.Models;
using Wpf_Budgetplanerare.Views;

namespace Wpf_Budgetplanerare
{
    public partial class MainWindow : Window
    {
        private readonly UserRepository _userRepo;

        public MainWindow()
        {
            InitializeComponent();

            var db = new BudgetDbContext();
            _userRepo = new UserRepository(db);

            LoadUsers();
        }

        private async void LoadUsers()
        {
            UsersComboBox.ItemsSource = await _userRepo.GetAllAsync();
        }

        private async void Continue_Click(object sender, RoutedEventArgs e)
        {
            if (UsersComboBox.SelectedItem is not User user)
            {
                MessageBox.Show("Select a user first.");
                return;
            }

            await _userRepo.SetActiveAsync(user.Id);
            OpenDashboard();
        }

        private async void Create_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(FirstNameBox.Text))
            {
                MessageBox.Show("First name is required.");
                return;
            }

            if (!decimal.TryParse(IncomeBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var income))
            {
                MessageBox.Show("Invalid income.");
                return;
            }

            if (!int.TryParse(HoursBox.Text, out var hours))
            {
                MessageBox.Show("Invalid work hours.");
                return;
            }

            var user = new User
            {
                FirstName = FirstNameBox.Text.Trim(),
                LastName = LastNameBox.Text.Trim(),
                IncomeMonthly = income,
                WorkHoursMonthly = hours,
                Active = true,
                Balance = 0m
            };

            await _userRepo.AddAsync(user);
            await _userRepo.SetActiveAsync(user.Id);

            OpenDashboard();
        }

        private void OpenDashboard()
        {
            var dashboard = new BudgetDashboard();
            dashboard.Show();
            Close();
        }

    }
}
