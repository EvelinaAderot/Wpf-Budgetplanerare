// UserChooserViewModel.cs (LÄGG TILL / SÄKERSTÄLL ATT DETTA FINNS)
// OBS: Filen kan redan finnas hos dig. Se till att properties + commands matchar XAML ovan.

using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Input;
using Wpf_Budgetplanerare.Data.Repositories.Interfaces;
using Wpf_Budgetplanerare.Models;
using Wpf_Budgetplanerare.ViewModels.Base;

namespace Wpf_Budgetplanerare.ViewModels
{
    public class UserChooserViewModel : ViewModelBase
    {
        private readonly IUserRepository _userRepository;
        private readonly Func<Task>? _onUserReady;

        public ObservableCollection<User> Users { get; } = new();

        private User? _selectedUser;
        public User? SelectedUser
        {
            get => _selectedUser;
            set => SetProperty(ref _selectedUser, value);
        }

        private string _newFirstName = "";
        public string NewFirstName
        {
            get => _newFirstName;
            set => SetProperty(ref _newFirstName, value);
        }

        private string _newLastName = "";
        public string NewLastName
        {
            get => _newLastName;
            set => SetProperty(ref _newLastName, value);
        }

        // Bind as string to avoid TextBox->decimal/int conversion issues while typing
        private string _newIncomeMonthly = "";
        public string NewIncomeMonthly
        {
            get => _newIncomeMonthly;
            set => SetProperty(ref _newIncomeMonthly, value);
        }

        private string _newWorkHoursMonthly = "";
        public string NewWorkHoursMonthly
        {
            get => _newWorkHoursMonthly;
            set => SetProperty(ref _newWorkHoursMonthly, value);
        }

        public ICommand SelectUserCommand { get; }
        public ICommand CreateUserCommand { get; }

        public UserChooserViewModel(IUserRepository userRepository, Func<Task>? onUserReady = null)
        {
            _userRepository = userRepository;
            _onUserReady = onUserReady;

            SelectUserCommand = new AsyncRelayCommand(SelectUserAsync);
            CreateUserCommand = new AsyncRelayCommand(CreateUserAsync);
        }

        public async Task ReloadAsync()
        {
            var list = await _userRepository.GetAllAsync();

            Users.Clear();
            foreach (var u in list)
                Users.Add(u);

            SelectedUser = Users.FirstOrDefault(u => u.Active) ?? Users.FirstOrDefault();
        }

        private async Task SelectUserAsync()
        {
            if (SelectedUser == null) return;

            await _userRepository.SetActiveAsync(SelectedUser.Id);

            if (_onUserReady != null)
                await _onUserReady();
        }

        private async Task CreateUserAsync()
        {
            // Basic parsing (Swedish culture often uses comma, handle both)
            if (!TryParseDecimal(NewIncomeMonthly, out var income)) return;
            if (!int.TryParse(NewWorkHoursMonthly, out var hours)) return;
            if (string.IsNullOrWhiteSpace(NewFirstName)) return;

            var user = new User
            {
                FirstName = NewFirstName.Trim(),
                LastName = NewLastName.Trim(),
                IncomeMonthly = income,
                WorkHoursMonthly = hours,
                Active = true
            };

            await _userRepository.AddAsync(user);

            // Set new user as active (in case others exist)
            await _userRepository.SetActiveAsync(user.Id);

            // Refresh list & select the new one
            await ReloadAsync();

            // Clear inputs
            NewFirstName = "";
            NewLastName = "";
            NewIncomeMonthly = "";
            NewWorkHoursMonthly = "";

            if (_onUserReady != null)
                await _onUserReady();
        }

        private static bool TryParseDecimal(string input, out decimal value)
        {
            input = (input ?? "").Trim();

            // allow both "1234,56" and "1234.56"
            var se = CultureInfo.GetCultureInfo("sv-SE");
            if (decimal.TryParse(input, NumberStyles.Number, se, out value))
                return true;

            var inv = CultureInfo.InvariantCulture;
            return decimal.TryParse(input.Replace(',', '.'), NumberStyles.Number, inv, out value);
        }
    }
}
