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
        public DbSet<RetailerProduct> RetailerProducts { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<CartItem> CartItems { get; set; }

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

            // Configure relationship between Order and ApplicationUser (as Retailer)
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Retailer)
                .WithMany(u => u.RetailerOrders)
                .HasForeignKey(o => o.RetailerID)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure relationship between Order and ApplicationUser (as Wholesaler)
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Wholesaler)
                .WithMany(u => u.WholesalerOrders)
                .HasForeignKey(o => o.WholesalerID)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure CartItem relationships - ADDING THIS IS CRITICAL
            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Retailer)
                .WithMany()
                .HasForeignKey(ci => ci.RetailerID)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.WholesalerProduct)
                .WithMany()
                .HasForeignKey(ci => ci.WholesalerProductID)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure WholesalerProduct relationships - ALSO IMPORTANT
            modelBuilder.Entity<WholesalerProduct>()
                .HasOne(wp => wp.Product)
                .WithMany(p => p.WholesalerProducts)
                .HasForeignKey(wp => wp.ProductID)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<WholesalerProduct>()
                .HasOne(wp => wp.Wholesaler)
                .WithMany(u => u.WholesalerProducts)
                .HasForeignKey(wp => wp.WholesalerID)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure OrderItem relationships
            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.WholesalerProduct)
                .WithMany()
                .HasForeignKey(oi => oi.WholesalerProductID)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Product)
                .WithMany()
                .HasForeignKey(oi => oi.ProductID)
                .OnDelete(DeleteBehavior.NoAction);

            // Fix decimal precision warnings
            modelBuilder.Entity<Product>()
                .Property(p => p.DefaultPrice)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<RetailerProduct>()
                .Property(p => p.Price)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Transaction>()
                .Property(t => t.TotalAmount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<TransactionDetail>()
                .Property(td => td.Subtotal)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<TransactionDetail>()
                .Property(td => td.UnitPrice)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<WholesalerProduct>()
                .Property(wp => wp.Price)
                .HasColumnType("decimal(18,2)");

            // Add indexes on commonly queried fields
            modelBuilder.Entity<Product>().HasIndex(p => p.Name);
            modelBuilder.Entity<Product>().HasIndex(p => p.CategoryID);
            modelBuilder.Entity<Transaction>().HasIndex(t => t.Date);
            modelBuilder.Entity<Transaction>().HasIndex(t => t.RetailerID);
            modelBuilder.Entity<Transaction>().HasIndex(t => t.WholesalerID);
            modelBuilder.Entity<TransactionDetail>().HasIndex(td => td.ProductID);
        }
    }
}