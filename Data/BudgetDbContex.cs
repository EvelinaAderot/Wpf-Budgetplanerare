// ff — BudgetDbContext.cs
using Microsoft.EntityFrameworkCore;
using Wpf_Budgetplanerare.Models;

namespace Wpf_Budgetplanerare.Data
{
    public class BudgetDbContext : DbContext
    {
        public BudgetDbContext()
        {
        }

        public BudgetDbContext(DbContextOptions<BudgetDbContext> options)
            : base(options)
        {
        }

        public DbSet<Item> Items => Set<Item>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<User> Users => Set<User>();
        public DbSet<Absence> Absences => Set<Absence>();

        public DbSet<BudgetPlan> BudgetPlans => Set<BudgetPlan>();
        public DbSet<MonthlyBudget> MonthlyBudgets => Set<MonthlyBudget>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(
                    @"Server=(localdb)\MSSQLLocalDB;Database=BudgetPlannerDb;Trusted_Connection=True;TrustServerCertificate=True");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =========================
            modelBuilder.Entity<BudgetPlan>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Month)
                      .IsRequired();

                entity.Property(x => x.MonthlyBudget)
                      .HasColumnType("decimal(18,2)")
                      .IsRequired();

                entity.Property(x => x.QuarterlyBudget)
                      .HasColumnType("decimal(18,2)")
                      .IsRequired();

                entity.Property(x => x.YearlyBudget)
                      .HasColumnType("decimal(18,2)")
                      .IsRequired();

                entity.HasIndex(x => new { x.UserId, x.Month })
                      .IsUnique();

                entity.HasOne(x => x.User)
                      .WithMany()
                      .HasForeignKey(x => x.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Item>(entity =>
            {
                entity.HasKey(i => i.Id);

                entity.Property(i => i.Amount)
                      .HasColumnType("decimal(18,2)")
                      .IsRequired();

                entity.Property(i => i.PostingDate).IsRequired();
                entity.Property(i => i.TransactionDate).IsRequired();
                entity.Property(i => i.ItemType).IsRequired();
                entity.Property(i => i.RecurrenceType).IsRequired();

                // entity.Property(i => i.YearlyMonth)
                //       .HasConversion<int?>();

                entity.HasOne(i => i.Category)
                      .WithMany(c => c.Items)
                      .HasForeignKey(i => i.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(i => i.User)
                      .WithMany()
                      .HasForeignKey(i => i.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(c => c.Id);

                entity.Property(c => c.Name)
                      .HasMaxLength(100)
                      .IsRequired();

                entity.Property(c => c.ItemType)
                      .IsRequired();
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);

                entity.Property(u => u.FirstName)
                      .HasMaxLength(100);

                entity.Property(u => u.LastName)
                      .HasMaxLength(100);

                entity.Property(u => u.IncomeMonthly)
                      .HasColumnType("decimal(18,2)");

                entity.Property(u => u.WorkHoursMonthly)
                      .IsRequired();
            });

            modelBuilder.Entity<Absence>(entity =>
            {
                entity.HasKey(a => a.Id);

                entity.Property(a => a.DateInput).IsRequired();
                entity.Property(a => a.Type).IsRequired();
            });

            modelBuilder.Entity<MonthlyBudget>(entity =>
            {
                entity.HasKey(mb => mb.Id);

                entity.Property(mb => mb.Month)
                      .IsRequired();

                entity.Property(mb => mb.EndMonth)
                      .IsRequired();

                entity.Property(mb => mb.Amount)
                      .HasColumnType("decimal(18,2)")
                      .IsRequired();

                entity.HasIndex(mb => new { mb.UserId, mb.CategoryId, mb.Month })
                      .IsUnique();

                entity.HasOne(mb => mb.User)
                      .WithMany()
                      .HasForeignKey(mb => mb.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(mb => mb.Category)
                      .WithMany()
                      .HasForeignKey(mb => mb.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
