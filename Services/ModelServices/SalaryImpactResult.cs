using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wpf_Budgetplanerare.Services.ModelServices
{
    public class SalaryImpactResult
    {
        public decimal Deduction { get; set; }
        public decimal Compensation { get; set; }
        public decimal NetImpact => Compensation - Deduction;
    }
}
