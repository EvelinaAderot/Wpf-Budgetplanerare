using System.Windows;
using Wpf_Budgetplanerare.Data;
using Wpf_Budgetplanerare.Data.Seed;

namespace Wpf_Budgetplanerare
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            using var db = new BudgetDbContext();
            Seed.Initialize(db);
        }
    }
}
