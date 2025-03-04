using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using RetailerWholesalerSystem.Models;

namespace RetailerWholesalerSystem.Models
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        // Constructor accepting options for dependency injection
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Parameterless constructor that uses a default connection string
        // This should be used only when absolutely necessary
        public ApplicationDbContext()
            : base(GetDefaultOptions())
        {
        }

        private static DbContextOptions<ApplicationDbContext> GetDefaultOptions()
        {
            // This creates options with a SQL Server provider and a hardcoded connection string
            // This is not ideal, but is necessary for some scenarios
            return new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer("Server=.;Database=RetailerWholesalerDB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True")
                .Options;
        }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<WholesalerProduct> WholesalerProducts { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<TransactionDetail> TransactionDetails { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationship between Transaction and ApplicationUser (as Retailer)
            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Retailer)
                .WithMany(u => u.RetailerTransactions)
                .HasForeignKey(t => t.RetailerID)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure relationship between Transaction and ApplicationUser (as Wholesaler)
            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Wholesaler)
                .WithMany(u => u.WholesalerTransactions)
                .HasForeignKey(t => t.WholesalerID)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}