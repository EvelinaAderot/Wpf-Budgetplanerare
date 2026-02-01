using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wpf_Budgetplanerare.Models;
using Wpf_Budgetplanerare.Services.ModelServices;

namespace Wpf_Budgetplanerare.Services
{
    public class SalaryCalculationService
    {
        private const decimal CompensationRate = 0.8m;

        public SalaryImpactResult CalculateMonthlyImpact(
            User user,
            IEnumerable<Absence> absences,
            int year,
            int month)
        {
            if (user.WorkHoursMonthly <= 0)
                return new SalaryImpactResult();

            decimal totalDeduction = 0m;
            decimal totalCompensation = 0m;

            foreach (var a in absences)
            {
                if (a.Hours <= 0)
                    continue;

                var hourlyRate = user.IncomeMonthly / user.WorkHoursMonthly;
                var deduction = hourlyRate * a.Hours;
                var compensation = deduction * CompensationRate;

                totalDeduction += deduction;
                totalCompensation += compensation;
            }

            return new SalaryImpactResult
            {
                Deduction = totalDeduction,
                Compensation = totalCompensation
            };
        }
    }
}
