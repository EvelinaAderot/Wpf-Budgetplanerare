using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wpf_Budgetplanerare.Models;

namespace Wpf_Budgetplanerare.Data.Repositories.Interfaces
{
    public interface IAbsenceRepository
    {
        Task<List<Absence>> GetAllAsync();
        Task<Absence?> GetByIdAsync(int id);

        Task<List<Absence>> GetByUserIdAsync(int userId);

        Task<List<Absence>> GetByUserIdAndMonthAsync(int userId, int year, int month);

        Task AddAsync(Absence absence);
        Task UpdateAsync(Absence absence);
        Task DeleteAsync(int id);
    }
}
