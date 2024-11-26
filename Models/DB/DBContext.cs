using Microsoft.EntityFrameworkCore;

namespace WaterMeterAPI.Models.DB
{
    public class DBContext : DbContext
    {
        public DbSet<AccountModel> Accounts { get; set; }
        public DBContext(DbContextOptions<DBContext> options) : base(options)
        {
            //if (!Kasutajad?.Where(x => x.IsAdmin).Any() ?? false)
            //{
            //    Kasutajad.Add(new(0, "admin", "admin", "admin", "admin", true));
            //    SaveChanges();
            //}
        }
    }
}
