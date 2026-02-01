using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wpf_Budgetplanerare.Services.ModelServices
{
    public class ForecastResult
    {
        public int Year { get; set; }
        public int Month { get; set; }

        public decimal TotalIncome { get; set; }
        public decimal TotalExpenses { get; set; }

        public decimal TotalSavings { get; set; }

        public SalaryImpactResult SalaryImpact { get; set; } = new SalaryImpactResult();

        public decimal Balance { get; set; }
    }
}

