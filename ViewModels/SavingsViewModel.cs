using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Wpf_Budgetplanerare.Data.Repositories.Interfaces;
using Wpf_Budgetplanerare.Models;
using Wpf_Budgetplanerare.Services;
using Wpf_Budgetplanerare.ViewModels.Base;

namespace Wpf_Budgetplanerare.ViewModels
{
    public class SavingsViewModel : ViewModelBase
    {
        private readonly SavingsService _savingsService;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IUserRepository _userRepository;

        public ObservableCollection<Category> SavingsCategories { get; } = new();

        private Category? _selectedCategory;
        public Category? SelectedCategory
        {
            get => _selectedCategory;
            set => SetProperty(ref _selectedCategory, value);
        }

        public decimal Amount { get; set; }


        public ICommand DepositCommand { get; }

        public SavingsViewModel(
            SavingsService savingsService,
            ICategoryRepository categoryRepository,
            IUserRepository userRepository)
        {
            _savingsService = savingsService;
            _categoryRepository = categoryRepository;
            _userRepository = userRepository;

            DepositCommand = new AsyncRelayCommand(DepositAsync);
            LoadAsync();
        }

        private async void LoadAsync()
        {
            SavingsCategories.Clear();
            foreach (var c in await _categoryRepository.GetByItemTypeAsync(ItemType.Savings))
                SavingsCategories.Add(c);
        }

        private async Task DepositAsync()
        {
            var user = await _userRepository.GetActiveAsync();
            if (user == null || SelectedCategory == null) return;

            await _savingsService.DepositToSavingsAsync(
                user.Id,
                SelectedCategory.Id,
                Amount,
                System.DateTime.Today);
        }
    }
}
