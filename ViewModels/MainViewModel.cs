using System.Threading.Tasks;
using System.Windows.Input;
using Wpf_Budgetplanerare.Data.Repositories.Interfaces;
using Wpf_Budgetplanerare.ViewModels.Base;

namespace Wpf_Budgetplanerare.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly IUserRepository _userRepository;

        private readonly ItemsViewModel _itemsVM;
        private readonly ForecastViewModel _forecastVM;
        private readonly SavingsViewModel _savingsVM;
        private readonly AbsenceViewModel _absenceVM;
        private readonly UserViewModel _userVM;

        private ViewModelBase _currentViewModel = null!;
        public ViewModelBase CurrentViewModel
        {
            get => _currentViewModel;
            set => SetProperty(ref _currentViewModel, value);
        }

        public UserChooserViewModel UserSelection { get; }

        private bool _isUserSelectionOpen;
        public bool IsUserSelectionOpen
        {
            get => _isUserSelectionOpen;
            set => SetProperty(ref _isUserSelectionOpen, value);
        }

        public ICommand ShowItemsCommand { get; }
        public ICommand ShowForecastCommand { get; }
        public ICommand ShowSavingsCommand { get; }
        public ICommand ShowAbsenceCommand { get; }
        public ICommand ShowUserCommand { get; }

        public ICommand OpenUserSelectionCommand { get; }
        public ICommand CloseUserSelectionCommand { get; }

        public MainViewModel(
            IUserRepository userRepository,
            ItemsViewModel itemsVM,
            ForecastViewModel forecastVM,
            SavingsViewModel savingsVM,
            AbsenceViewModel absenceVM,
            UserViewModel userVM)
        {
            _userRepository = userRepository;

            _itemsVM = itemsVM;
            _forecastVM = forecastVM;
            _savingsVM = savingsVM;
            _absenceVM = absenceVM;
            _userVM = userVM;

            ShowItemsCommand = new RelayCommand(() => CurrentViewModel = _itemsVM);
            ShowForecastCommand = new RelayCommand(() => CurrentViewModel = _forecastVM);
            ShowSavingsCommand = new RelayCommand(() => CurrentViewModel = _savingsVM);
            ShowAbsenceCommand = new RelayCommand(() => CurrentViewModel = _absenceVM);
            ShowUserCommand = new RelayCommand(() => CurrentViewModel = _userVM);

            OpenUserSelectionCommand = new RelayCommand(() => IsUserSelectionOpen = true);
            CloseUserSelectionCommand = new RelayCommand(() => IsUserSelectionOpen = false);

            UserSelection = new UserChooserViewModel(
                _userRepository,
                onUserReady: async () =>
                {
                    IsUserSelectionOpen = false;
                    await ReloadAllAsync();
                    CurrentViewModel = _itemsVM;
                });

            CurrentViewModel = _itemsVM;

            _ = EnsureActiveUserAsync();
        }

        private async Task EnsureActiveUserAsync()
        {
            var active = await _userRepository.GetActiveAsync();
            if (active == null)
            {
                IsUserSelectionOpen = true;
                await UserSelection.ReloadAsync();
            }
            else
            {
                IsUserSelectionOpen = false;
                await ReloadAllAsync();
            }
        }

        private async Task ReloadAllAsync()
        {
            await _itemsVM.ReloadAsync();
            await _absenceVM.ReloadAsync();
            await _forecastVM.ReloadAsync();
            await _userVM.ReloadAsync();

        }
    }
}
