using Microsoft.EntityFrameworkCore;
using StoreAPI.Models.Entities;

namespace StoreAPI;

public class StoreDbContext : DbContext
{
    public DbSet<Order> Order { get; set; }
    public DbSet<Product> Product { get; set; }
    public DbSet<SystemUser> SystemUsers { get; set; }
    public DbSet<Store> Store { get; set; }
    public DbSet<OrderProduct> OrderProduct { get; set; }

    public StoreDbContext(DbContextOptions<StoreDbContext> options) : base(options)
    {
        
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<OrderProduct>()
            .HasKey(p => new { p.ProductId, p.OrderId });

        modelBuilder.Entity<SystemUser>()
            .HasData(
                new SystemUser
                {
                    Id = 1,
                    FirstName = "Leonardo Gael",
                    LastName = "Duran Torres",
                    Email = "gael.duran@gmail.com",
                    Password = "123456"

                }
            );
    }
}