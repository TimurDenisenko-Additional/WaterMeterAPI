using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WaterMeterAPI.Models;
using WaterMeterAPI.Models.DB;

namespace WaterMeterAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class WaterMeterController(DBContext DB, IMemoryCache memoryCache) : ControllerBase
    {
        private IMemoryCache _memoryCache { get; } = memoryCache;

        // GET: WaterMeter
        [HttpGet]
        public List<WaterMeterModel> GetWaterMeters() => [.. DB.WaterMeters];

        // GET: WaterMeter/id
        [HttpGet]
        public async Task<IActionResult> GetWaterMeter(int id) => await DB.WaterMeters.ElementAtNoTrack(id) == null ?
            BadRequest(new { message = "Veenäitu ei leitud" }) : Ok(await DB.WaterMeters.ElementAtNoTrack(id));

        // DELETE: WaterMeter/delete/id
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            WaterMeterModel? waterMeter = await DB.WaterMeters.ElementAtWithTrack(id);
            if (waterMeter == null)
                return BadRequest(new { message = "Veenäitu ei leitud" });
            DB.WaterMeters.Remove(waterMeter);
            await DB.SaveChangesAsync();
            return Ok(DB.WaterMeters);
        }
        private async Task<IActionResult> CreateWaterMeter(string email, string address, int apartament, DateTime date, int coldWater, int warmWater, bool paymentStatus)
        {
            await DB.WaterMeters.AddAsync(new(0, email, address, apartament, date, coldWater, warmWater, paymentStatus));
            await DB.SaveChangesAsync();
            return Ok(DB.WaterMeters);
        }

        // POST: WaterMeter/create/email/address/apartament/date/coldWater/warmWater/paymentStatus
        [HttpPost("create/{email}/{address}/{apartament}/{date}/{coldWater}/{warmWater}/{paymentStatus}")]
        public async Task<IActionResult> Create(string email, string address, int apartament, DateTime date, int coldWater, int warmWater, bool paymentStatus)
        {
            if (await DB.Accounts.Where(x => x.Email == email).AnyAsync())
            {
                return await CreateWaterMeter(email, address, apartament, date, coldWater, warmWater, paymentStatus);
            }
            return BadRequest(new { message = "Kasutaja ei leitud" });
        }

        // POST: WaterMeter/createGivenNonExistentEmail/email/address/apartament/date/coldWater/warmWater/paymentStatus
        [HttpPost("createGivenNonExistentEmail/{email}/{address}/{apartament}/{date}/{coldWater}/{warmWater}/{paymentStatus}")]
        public async Task<IActionResult> CreateGivenNonExistentEmail(string email, string address, int apartament, DateTime date, int coldWater, int warmWater, bool paymentStatus) =>
            await CreateWaterMeter(email, address, apartament, date, coldWater, warmWater, paymentStatus);

        // GET: WaterMeter/getMonthlyBill/email/month
        [HttpGet("getMonthlyBill/{email}/{year}/{month}")]
        public async Task<IActionResult> GetMonthlyBill(string email, int year, int month)
        {
            WaterMeterModel? waterMeter = await DB.WaterMeters.FirstOrDefaultAsync(x => x.Date.Year == year && x.Date.Month == month && x.Email == email);
            if (waterMeter == null)
                return BadRequest(new { message = "Sellel inimesel puuduvad selle kuu veemõõtjate näidud" });
            return Ok(waterMeter);
        }
    }
}
