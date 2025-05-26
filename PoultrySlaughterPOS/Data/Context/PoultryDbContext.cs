using Microsoft.EntityFrameworkCore;
using PoultrySlaughterPOS.Models.Entities;

namespace PoultrySlaughterPOS.Data.Context
{
    /// <summary>
    /// Entity Framework Database Context for Poultry Slaughter POS System
    /// </summary>
    public class PoultryDbContext : DbContext
    {
        public PoultryDbContext(DbContextOptions<PoultryDbContext> options) : base(options)
        {
        }

        // DbSets for all entities
        public DbSet<Truck> Trucks { get; set; }
        public DbSet<TruckLoad> TruckLoads { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<Payment> Payments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply entity configurations
            ConfigureTruckEntity(modelBuilder);
            ConfigureTruckLoadEntity(modelBuilder);
            ConfigureCustomerEntity(modelBuilder);
            ConfigureInvoiceEntity(modelBuilder);
            ConfigurePaymentEntity(modelBuilder);

            // Seed initial data
            SeedInitialData(modelBuilder);
        }

        private static void ConfigureTruckEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Truck>(entity =>
            {
                entity.HasIndex(e => e.TruckNumber).IsUnique();
                entity.Property(e => e.TruckNumber).IsRequired();
                entity.Property(e => e.DriverName).IsRequired();
            });
        }

        private static void ConfigureTruckLoadEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TruckLoad>(entity =>
            {
                entity.HasOne(d => d.Truck)
                    .WithMany(p => p.TruckLoads)
                    .HasForeignKey(d => d.TruckId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.TruckId, e.LoadDate });
            });
        }

        private static void ConfigureCustomerEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.HasIndex(e => e.CustomerName);
                entity.Property(e => e.CustomerName).IsRequired();
            });
        }

        private static void ConfigureInvoiceEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Invoice>(entity =>
            {
                entity.HasIndex(e => e.InvoiceNumber).IsUnique();
                entity.HasIndex(e => e.InvoiceDate);
                entity.HasIndex(e => new { e.CustomerId, e.InvoiceDate });

                entity.HasOne(d => d.Customer)
                    .WithMany(p => p.Invoices)
                    .HasForeignKey(d => d.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.Truck)
                    .WithMany(p => p.Invoices)
                    .HasForeignKey(d => d.TruckId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void ConfigurePaymentEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.HasIndex(e => e.PaymentDate);
                entity.HasIndex(e => new { e.CustomerId, e.PaymentDate });

                entity.HasOne(d => d.Customer)
                    .WithMany(p => p.Payments)
                    .HasForeignKey(d => d.CustomerId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Invoice)
                    .WithMany(p => p.Payments)
                    .HasForeignKey(d => d.InvoiceId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }

        private static void SeedInitialData(ModelBuilder modelBuilder)
        {
            // Seed default trucks
            modelBuilder.Entity<Truck>().HasData(
                new Truck
                {
                    TruckId = 1,
                    TruckNumber = "T001",
                    DriverName = "سائق 1",
                    IsActive = true,
                    CreatedDate = DateTime.Now
                },
                new Truck
                {
                    TruckId = 2,
                    TruckNumber = "T002",
                    DriverName = "سائق 2",
                    IsActive = true,
                    CreatedDate = DateTime.Now
                }
            );

            // Seed default customer
            modelBuilder.Entity<Customer>().HasData(
                new Customer
                {
                    CustomerId = 1,
                    CustomerName = "زبون نقدي",
                    IsActive = true,
                    CreatedDate = DateTime.Now
                }
            );
        }
    }
}