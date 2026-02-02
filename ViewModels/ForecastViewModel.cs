using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Wpf_Budgetplanerare.Data.Repositories.Interfaces;
using Wpf_Budgetplanerare.Services;
using Wpf_Budgetplanerare.Services.ModelServices;
using Wpf_Budgetplanerare.ViewModels.Base;

namespace Wpf_Budgetplanerare.ViewModels
{
    public class ForecastViewModel : ViewModelBase
    {
        private readonly ForecastService _forecastService;
        private readonly IUserRepository _userRepository;

        private ForecastResult? _forecast;
        public ForecastResult? Forecast
        {
            get => _forecast;
            set => SetProperty(ref _forecast, value);
        }

        public int Year { get; set; } = DateTime.Today.Year;
        public int Month { get; set; } = DateTime.Today.Month;

        public ICommand RefreshCommand { get; }

        public ForecastViewModel(
            ForecastService forecastService,
            IUserRepository userRepository)
        {
            _forecastService = forecastService;
            _userRepository = userRepository;

            RefreshCommand = new AsyncRelayCommand(LoadAsync);

            _ = LoadAsync(); // explicitly ignored task
        }


        private async Task LoadAsync()
        {
            var user = await _userRepository.GetActiveAsync();
            if (user == null) return;

            Forecast = await _forecastService.BuildMonthlyForecastAsync(
                user.Id, Year, Month);
        }
        public async Task ReloadAsync() => await LoadAsync();

    }
}
