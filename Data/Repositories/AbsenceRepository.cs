using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Wpf_Budgetplanerare.Data.Repositories.Interfaces;
using Wpf_Budgetplanerare.Models;

namespace Wpf_Budgetplanerare.Data.Repositories.Implementations
{
    public class AbsenceRepository : IAbsenceRepository
    {
        private readonly BudgetDbContext con;

        public AbsenceRepository(BudgetDbContext context)
        {
            con = context;
        }

        public async Task<List<Absence>> GetAllAsync()
        {
            return await con.Absences
                .Include(a => a.User)
                .ToListAsync();
        }

        public async Task<Absence?> GetByIdAsync(int id)
        {
            return await con.Absences
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<List<Absence>> GetByUserIdAsync(int userId)
        {
            return await con.Absences
                .Where(a => a.UserId == userId)
                .ToListAsync();
        }

        public async Task<List<Absence>> GetByUserIdAndMonthAsync(int userId, int year, int month)
        {
            // Månadens intervall: [monthStart, monthEnd)
            var monthStart = new DateTime(year, month, 1);
            var monthEnd = monthStart.AddMonths(1);

            // Overlap-regel: start < monthEnd AND end >= monthStart
            // OBS: om DateEnd aldrig sätts, bör du sätta DateEnd = DateStart i din VM/Service.
            return await con.Absences
                .Where(a =>
                    a.UserId == userId &&
                    a.DateStart < monthEnd &&
                    a.DateEnd >= monthStart)
                .ToListAsync();
        }

        public async Task AddAsync(Absence absence)
        {
            con.Absences.Add(absence);
            await con.SaveChangesAsync();
        }

        public async Task UpdateAsync(Absence absence)
        {
            con.Absences.Update(absence);
            await con.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var absence = await con.Absences.FindAsync(id);
            if (absence != null)
            {
                con.Absences.Remove(absence);
                await con.SaveChangesAsync();
            }
        }
    }
}

