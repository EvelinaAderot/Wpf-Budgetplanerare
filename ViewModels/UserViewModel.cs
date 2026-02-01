using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Wpf_Budgetplanerare.Data.Repositories.Interfaces;
using Wpf_Budgetplanerare.Models;
using Wpf_Budgetplanerare.ViewModels.Base;

namespace Wpf_Budgetplanerare.ViewModels
{
    public class UserViewModel : ViewModelBase
    {
        private readonly IUserRepository _userRepository;

        private User? _user;
        public User? User
        {
            get => _user;
            set => SetProperty(ref _user, value);
        }

        public ICommand SaveCommand { get; }

        public UserViewModel(IUserRepository userRepository)
        {
            _userRepository = userRepository;
            SaveCommand = new AsyncRelayCommand(SaveAsync);
            LoadAsync();
        }

        private async void LoadAsync()
        {
            User = await _userRepository.GetActiveAsync();
        }

        private async Task SaveAsync()
        {
            if (User != null)
                await _userRepository.UpdateAsync(User);
        }
        public async Task ReloadAsync()
        {
            User = await _userRepository.GetActiveAsync();
        }
    }
}