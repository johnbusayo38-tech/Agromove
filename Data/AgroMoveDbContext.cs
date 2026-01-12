


using Microsoft.EntityFrameworkCore;
using AgroMove.API.Models;

namespace AgroMove.API.Data
{
    public class AgroMoveDbContext : DbContext
    {
        public AgroMoveDbContext(DbContextOptions<AgroMoveDbContext> options) 
            : base(options) { }

        // Core DbSets
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Wallet> Wallets { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<WalletTransaction> WalletTransactions { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;

        // AgroShop & Logistics Hubs DbSets
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<OrderItem> OrderItems { get; set; }
        // THIS FIXES: error CS1061 regarding 'ProductGauges' definition
        public DbSet<ProductGauge> ProductGauges { get; set; } = null!;
        
public DbSet<MarketplaceOrder> MarketplaceOrders { get; set; }
    public DbSet<MarketplaceOrderItem> MarketplaceOrderItems { get; set; }

        public DbSet<Hub> Hubs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ========== User Configuration ==========
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.Property(u => u.Id).ValueGeneratedOnAdd();
                entity.HasIndex(u => u.Email).IsUnique();
                entity.HasIndex(u => u.Phone);

                entity.Property(u => u.Role)
                      .HasDefaultValue("FARMER")
                      .HasMaxLength(20);

                entity.Property(u => u.PasswordHash)
                      .HasMaxLength(255);

                entity.HasMany(u => u.OrdersAsShipper)
                      .WithOne(o => o.Shipper)
                      .HasForeignKey(o => o.ShipperId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(u => u.OrdersAsDriver)
                      .WithOne(o => o.Driver)
                      .HasForeignKey(o => o.DriverId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // ========== Wallet Configuration ==========
            modelBuilder.Entity<Wallet>(entity =>
            {
                entity.HasKey(w => w.Id);
                entity.Property(w => w.Id).ValueGeneratedOnAdd();
                entity.HasIndex(w => w.UserId).IsUnique();

                entity.Property(w => w.Balance)
                      .HasColumnType("decimal(18,2)")
                      .HasDefaultValue(50000.00m);

                entity.HasOne(w => w.User)
                      .WithOne(u => u.Wallet)
                      .HasForeignKey<Wallet>(w => w.UserId)
                      .OnDelete(DeleteBehavior.Cascade)
                      .IsRequired();
            });

            // ========== WalletTransaction Configuration ==========
            modelBuilder.Entity<WalletTransaction>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.Property(t => t.Id).ValueGeneratedOnAdd();
                entity.Property(t => t.Amount).HasColumnType("decimal(18,2)");

                entity.HasOne(t => t.Wallet)
                      .WithMany(w => w.Transactions)
                      .HasForeignKey(t => t.WalletId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(t => t.WalletId);
                entity.HasIndex(t => t.Timestamp);
            });

            // ========== Order Configuration ==========
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(o => o.Id);
                entity.Property(o => o.Id).ValueGeneratedOnAdd();

                entity.Property(o => o.OrderDetailsJson)
                      .HasColumnType("jsonb")
                      .HasDefaultValueSql("'{}'::jsonb");

                entity.Property(o => o.Status)
                      .HasConversion<string>()
                      .HasColumnType("text")
                      .HasDefaultValue(OrderStatus.Pending);

                entity.HasIndex(o => o.Status);
                entity.HasIndex(o => o.CreatedAt);
                entity.HasIndex(o => o.ShipperId);
                entity.HasIndex(o => o.DriverId);
                entity.HasIndex(o => o.IsInternational);

                entity.HasOne(o => o.Shipper)
                      .WithMany(u => u.OrdersAsShipper)
                      .HasForeignKey(o => o.ShipperId)
                      .OnDelete(DeleteBehavior.Restrict)
                      .IsRequired();

                entity.HasOne(o => o.Driver)
                      .WithMany(u => u.OrdersAsDriver)
                      .HasForeignKey(o => o.DriverId)
                      .OnDelete(DeleteBehavior.SetNull)
                      .IsRequired(false);
            });

            // ========== Product (AgroShop) Configuration ==========
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Id).ValueGeneratedOnAdd();

                entity.Property(p => p.Label).IsRequired().HasMaxLength(100);
                entity.Property(p => p.Category).IsRequired().HasMaxLength(50);

                // RELATIONAL CONFIGURATION: One Product to Many Gauges
                entity.HasMany(p => p.Gauges)
                      .WithOne(g => g.Product)
                      .HasForeignKey(g => g.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(p => p.Category);
                entity.HasIndex(p => p.IsAvailable);
            });

            // ========== ProductGauge Configuration ==========
            modelBuilder.Entity<ProductGauge>(entity =>
            {
                entity.HasKey(g => g.Id);
                entity.Property(g => g.Id).ValueGeneratedOnAdd();

                entity.Property(g => g.Label).IsRequired().HasMaxLength(100);
                
                entity.Property(g => g.Price)
                      .HasColumnType("decimal(18,2)");

                entity.Property(g => g.Weight)
                      .HasColumnType("double precision"); // Double is standard for weight
            });

            // ========== Hub (Warehouse) Configuration ==========
            modelBuilder.Entity<Hub>(entity =>
            {
                entity.HasKey(h => h.Id);
                entity.Property(h => h.Id).ValueGeneratedOnAdd();
                
                entity.Property(h => h.Name).IsRequired().HasMaxLength(100);
                entity.Property(h => h.Address).IsRequired();
                
                entity.Property(h => h.Latitude).HasColumnType("double precision");
                entity.Property(h => h.Longitude).HasColumnType("double precision");

                entity.HasIndex(h => h.IsActive);
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}