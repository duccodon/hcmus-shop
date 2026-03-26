using hcmus_shop.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace hcmus_shop.Data
{
    public class MyShopDbContext : DbContext
    {
        public MyShopDbContext(DbContextOptions<MyShopDbContext> options)
            : base(options)
        {
        }

        // DbSets
        public DbSet<Brand> Brands { get; set; }
        public DbSet<Series> Series { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<ProductInstance> ProductInstances { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Promotion> Promotions { get; set; }
        public DbSet<InventoryLog> InventoryLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Áp dụng tất cả configurations từ thư mục Configurations
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(MyShopDbContext).Assembly);

            // Cấu hình chung cho UUID Primary Key
            modelBuilder.Entity<User>().HasKey(u => u.UserId);
            modelBuilder.Entity<Customer>().HasKey(c => c.CustomerId);
            modelBuilder.Entity<Order>().HasKey(o => o.OrderId);

            // Cấu hình PK cho các entity không theo convention mặc định
            modelBuilder.Entity<ProductImage>().HasKey(pi => pi.ImageId);
            modelBuilder.Entity<ProductInstance>().HasKey(pi => pi.InstanceId);
            modelBuilder.Entity<InventoryLog>().HasKey(il => il.LogId);

            // Một số index thường dùng
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Sku)
                .IsUnique();

            modelBuilder.Entity<ProductInstance>()
                .HasIndex(pi => pi.SerialNumber)
                .IsUnique();

            modelBuilder.Entity<Promotion>()
                .HasIndex(p => p.Code)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();
        }
    }
}
