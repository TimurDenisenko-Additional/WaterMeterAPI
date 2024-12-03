using Microsoft.EntityFrameworkCore;

namespace WaterMeterAPI.Models.DB
{
    public class DBContext : DbContext
    {
        public DbSet<AccountModel> Accounts { get; set; }
        public DbSet<WaterMeterModel> WaterMeters { get; set; }
        public DBContext(DbContextOptions<DBContext> options) : base(options)
        {
            if (Accounts != null && (!Accounts?.Where(x => x.Role.Equals("Admin")).Any() ?? false))
            {
                _ = Accounts?.Add(new(0, "Admin", "Admin", "Male", "admin@gmail.com", "admin", "Admin"));
                SaveChanges();
            }
        }
    }
}
