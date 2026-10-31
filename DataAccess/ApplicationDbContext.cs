 using Microsoft.EntityFrameworkCore;
using Online_Store.Models;

namespace Online_Store.DataAccess
{
    internal class ApplicationDbContext : DbContext
        {
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Customer> Customers { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.UseSqlServer(
                "Data Source=.;Initial Catalog=Online_Store;Integrated Security=True;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;");
        }
    }
}