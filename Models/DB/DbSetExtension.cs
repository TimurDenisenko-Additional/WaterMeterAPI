using Microsoft.EntityFrameworkCore;

namespace WaterMeterAPI.Models.DB
{
    public static class DbSetExtension
    {
        public static async Task<T?> ElementAtNoTrack<T>(this DbSet<T> DB, int id) where T : DBModel =>
            await DB.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id) ?? null;
        public static async Task<T?> ElementAtWithTrack<T>(this DbSet<T> DB, int id) where T : DBModel =>
            await DB.FirstAsync(x => x.Id == id) ?? null;
    }
}
