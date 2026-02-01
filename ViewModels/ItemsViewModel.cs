using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Wpf_Budgetplanerare.Data.Repositories.Interfaces;
using Wpf_Budgetplanerare.Models;
using Wpf_Budgetplanerare.ViewModels.Base;

namespace Wpf_Budgetplanerare.ViewModels
{
    public class ItemsViewModel : ViewModelBase
    {
        private readonly IItemRepository _itemRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IUserRepository _userRepository;

        public ObservableCollection<Item> Items { get; } = new();
        public ObservableCollection<Category> Categories { get; } = new();

        private Item? _selectedItem;
        public Item? SelectedItem
        {
            get => _selectedItem;
            set => SetProperty(ref _selectedItem, value);
        }

        public ICommand AddCommand { get; }
        public ICommand DeleteCommand { get; }

        public ItemsViewModel(
            IItemRepository itemRepository,
            ICategoryRepository categoryRepository,
            IUserRepository userRepository)
        {
            _itemRepository = itemRepository;
            _categoryRepository = categoryRepository;
            _userRepository = userRepository;

            AddCommand = new AsyncRelayCommand(AddAsync);
            DeleteCommand = new AsyncRelayCommand(DeleteAsync);

            LoadAsync();
        }

        private async void LoadAsync()
        {
            var user = await _userRepository.GetActiveAsync();
            if (user == null) return;

            Items.Clear();
            foreach (var i in await _itemRepository.GetByUserIdAsync(user.Id))
                Items.Add(i);

            Categories.Clear();
            foreach (var c in await _categoryRepository.GetAllAsync())
                Categories.Add(c);
        }

        private async Task AddAsync()
        {
            var user = await _userRepository.GetActiveAsync();
            if (user == null) return;

            var item = new Item
            {
                UserId = user.Id,
                Amount = 0,
                ItemType = ItemType.Expense,
                RecurrenceType = RecurrenceType.Once,
                TransactionDate = System.DateTime.Today
            };

            await _itemRepository.AddAsync(item);
            Items.Add(item);
        }

        private async Task DeleteAsync()
        {
            if (SelectedItem == null) return;

            await _itemRepository.DeleteAsync(SelectedItem.Id);
            Items.Remove(SelectedItem);
        }
        public async Task ReloadAsync()
        {
            var user = await _userRepository.GetActiveAsync();
            if (user == null) return;

            Items.Clear();
            foreach (var i in await _itemRepository.GetByUserIdAsync(user.Id))
                Items.Add(i);

            Categories.Clear();
            foreach (var c in await _categoryRepository.GetAllAsync())
                Categories.Add(c);
        }

    }
}
