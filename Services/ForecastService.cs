using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wpf_Budgetplanerare.Data.Repositories.Interfaces;
using Wpf_Budgetplanerare.Models;
using Wpf_Budgetplanerare.Services.ModelServices;

namespace Wpf_Budgetplanerare.Services
{

    public class ForecastService
    {
        private readonly IItemRepository _itemRepository;
        private readonly IUserRepository _userRepository;
        private readonly IAbsenceRepository _absenceRepository;
        private readonly SalaryCalculationService _salaryService;
        private readonly RecurrenceService _recurrenceService;

        public ForecastService(
            IItemRepository itemRepository,
            IUserRepository userRepository,
            IAbsenceRepository absenceRepository,
            SalaryCalculationService salaryService,
            RecurrenceService recurrenceService)
        {
            _itemRepository = itemRepository;
            _userRepository = userRepository;
            _absenceRepository = absenceRepository;
            _salaryService = salaryService;
            _recurrenceService = recurrenceService;
        }

        public async Task<ForecastResult> BuildMonthlyForecastAsync(int userId, int year, int month)
        {
            var user = await _userRepository.GetByIdAsync(userId)
                       ?? throw new InvalidOperationException($"User {userId} not found.");

            var items = await _itemRepository.GetByUserIdAndMonthAsync(userId, year, month);

            items = items.Where(i => _recurrenceService.AppliesToMonth(i, year, month)).ToList();

            var absences = await _absenceRepository.GetByUserIdAndMonthAsync(userId, year, month);

            var totalIncome = items.Where(i => i.ItemType == ItemType.Income).Sum(i => i.Amount);
            var totalExpenses = items.Where(i => i.ItemType == ItemType.Expense).Sum(i => i.Amount);


            var totalSavings = items.Where(i => i.ItemType == ItemType.Savings).Sum(i => i.Amount);

            var salaryImpact = _salaryService.CalculateMonthlyImpact(user, absences, year, month);

            var balance =
                totalIncome
                - totalExpenses
                - totalSavings
                + salaryImpact.NetImpact;

            return new ForecastResult
            {
                Year = year,
                Month = month,
                TotalIncome = totalIncome,
                TotalExpenses = totalExpenses,
                TotalSavings = totalSavings,
                SalaryImpact = salaryImpact,
                Balance = balance
            };
        }
    }
}

