using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Wpf_Budgetplanerare.Data.Repositories.Interfaces;
using Wpf_Budgetplanerare.Models;
using Wpf_Budgetplanerare.ViewModels.Base;

namespace Wpf_Budgetplanerare.ViewModels
{
    public class AbsenceViewModel : ViewModelBase
    {
        private readonly IAbsenceRepository _absenceRepository;
        private readonly IUserRepository _userRepository;

        public ObservableCollection<Absence> Absences { get; } = new();

        public ICommand AddCommand { get; }
        public ICommand DeleteCommand { get; }

        public AbsenceViewModel(
            IAbsenceRepository absenceRepository,
            IUserRepository userRepository)
        {
            _absenceRepository = absenceRepository;
            _userRepository = userRepository;

            AddCommand = new AsyncRelayCommand(AddAsync);
            DeleteCommand = new AsyncRelayCommand(DeleteAsync);

            LoadAsync();
        }

        private async void LoadAsync()
        {
            var user = await _userRepository.GetActiveAsync();
            if (user == null) return;

            Absences.Clear();
            foreach (var a in await _absenceRepository.GetByUserIdAsync(user.Id))
                Absences.Add(a);
        }

        private async Task AddAsync()
        {
            var user = await _userRepository.GetActiveAsync();
            if (user == null) return;

            var absence = new Absence
            {
                UserId = user.Id,
                DateStart = System.DateTime.Today,
                DateEnd = System.DateTime.Today,
                Hours = 8,
                Type = AbsenceType.Sick
            };

            await _absenceRepository.AddAsync(absence);
            Absences.Add(absence);
        }

        private async Task DeleteAsync()
        {
            if (Absences.Count == 0) return;

            var last = Absences[^1];
            await _absenceRepository.DeleteAsync(last.Id);
            Absences.Remove(last);
        }
        public async Task ReloadAsync()
        {
            var user = await _userRepository.GetActiveAsync();
            if (user == null) return;

            Absences.Clear();
            foreach (var a in await _absenceRepository.GetByUserIdAsync(user.Id))
                Absences.Add(a);
        }

    }
}
