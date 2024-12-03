using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Net.Mail;
using System.Net;
using System.Text;
using WaterMeterAPI.Models;
using WaterMeterAPI.Models.DB;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;

namespace WaterMeterAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class WaterMeterController(DBContext DB, IMemoryCache memoryCache) : ControllerBase
    {
        private IMemoryCache _memoryCache { get; } = memoryCache;
        private AccountModel? TryGetCurrentUser()
        {
            _memoryCache.TryGetValue("currentUser", out AccountModel? account);
            return account;
        }

        // GET: WaterMeter
        [HttpGet]
        public List<WaterMeterModel> GetWaterMeters() => [.. DB.WaterMeters];

        // GET: WaterMeter/id
        [HttpGet("{id}")]
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

        // POST: WaterMeter/addWaterMeter/address/apartament/coldWater/warmWater
        [HttpPost("addWaterMeter/{address}/{apartament}/{coldWater}/{warmWater}")]
        public async Task<IActionResult> AddWaterMeter(string address, int apartament, int coldWater, int warmWater)
        {
            AccountModel? currentUser = TryGetCurrentUser();
            if (currentUser == null)
                return BadRequest(new { message = "Te ei ole sisse logitud" });
            string subject = "Põhjalik kinnitamine: vee näitude lisamine";
            string body = $@"
                <html>
                <body>
                <div style=""font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px; border-radius: 8px;"">
                    <h2 style=""text-align: center; color: #333;"">Aitäh, et lisasite vee näidud!</h2>
                    <p style=""font-size: 16px; color: #555;"">Tere, {currentUser.FirstName} {currentUser.LastName}!</p>
                    <p style=""font-size: 16px; color: #555;"">Teie vee näidud on edukalt salvestatud järgmistelt aadressilt:</p>
                
                    <div style=""background-color: #fff; padding: 20px; border-radius: 8px; margin-bottom: 20px;"">
                        <p><strong>Aadress:</strong> {address}, Korter: {apartament}</p>
                        <p><strong>Külma vee näidud:</strong> {coldWater} m³</p>
                        <p><strong>Küppa vee näidud:</strong> {warmWater} m³</p>
                    </div>

                    <p style=""font-size: 16px; color: #555;"">Teie näidud on edukalt salvestatud meie süsteemis. Kui teil on küsimusi, võtke meiega ühendust.</p>
                
                    <div style=""text-align: center; margin-top: 30px;"">
                        <p style=""font-size: 14px; color: #777;"">Kui teil on küsimusi, võtke meiega ühendust.</p>
                        <p style=""font-size: 14px; color: #888;"">Lugupidamisega, Teie Teenuse Pakkuja</p>
                    </div>
                </div>
                </body>
                </html>";
            SendEmail(currentUser.Email, subject, body);
            return Ok(await CreateWaterMeter(currentUser.Email, address, apartament, DateTime.Now, coldWater, warmWater, false));
        }

        // GET: WaterMeter/getMonthlyBill/year/month
        [HttpGet("getMonthlyBill/{year}/{month}")]
        public async Task<IActionResult> GetMonthlyBill(int year, int month)
        {
            AccountModel? currentUser = TryGetCurrentUser();
            if (currentUser == null)
                return BadRequest(new { message = "Te ei ole sisse logitud" });
            WaterMeterModel? waterMeter = await DB.WaterMeters.FirstOrDefaultAsync(x => x.Date.Year == year && x.Date.Month == month && x.Email == currentUser.Email);
            if (waterMeter == null)
                return BadRequest(new { message = "Sellel inimesel puuduvad selle kuu veemõõtjate näidud" });

            string body = @$"
                <html>
                <body>
                <div class=""container"" style=""width: 80%; margin: 0 auto; font-family: Arial, sans-serif; background-color: #f9f9f9; padding: 20px; border-radius: 8px;"">
                    <div class=""header"" style=""text-align: center; margin-bottom: 20px;"">
                        <h2 style=""font-size: 28px; font-weight: bold; color: #333;"">Arve - {month} {year}</h2>
                        <p style=""font-size: 16px; color: #555;"">Kuupäev: <strong>{DateTime.Now}</strong></p>
                    </div>

                    <div class=""billing-info"" style=""font-size: 16px; line-height: 1.8; margin-bottom: 30px; background-color: #fff; padding: 20px; border-radius: 8px;"">
                        <h3 style=""font-size: 22px; color: #333;"">Kliendi teave:</h3>
                        <p><strong>Elektronposti aadress:</strong> {currentUser.Email}</p>
                        <p><strong>Aadress:</strong> {waterMeter.Address}, korter: {waterMeter.Apartment}</p>
                    </div>

                    <div class=""water-usage"" style=""font-size: 16px; line-height: 1.8; margin-bottom: 30px; background-color: #fff; padding: 20px; border-radius: 8px;"">
                        <h3 style=""font-size: 22px; color: #333;"">Vee tarbimine:</h3>
                        <p><strong>Külma vee näidud:</strong> {waterMeter.ColdWater} m³</p>
                        <p><strong>Küppa vee näidud:</strong> {waterMeter.WarmWater} m³</p>
                    </div>

                    <div class=""payment-status"" style=""font-size: 16px; line-height: 1.8; margin-bottom: 30px; background-color: #fff; padding: 20px; border-radius: 8px;"">
                        <h3 style=""font-size: 22px; color: #333;"">Makse staatus:</h3>
                        <p><strong>Status:</strong> {waterMeter.PaymentStatus}</p>
                    </div>

                    <div class=""footer"" style=""text-align: center; font-size: 14px; color: #777; margin-top: 20px;"">
                        <p>Täname, et maksisite! Kui teil on küsimusi, palun võtke meiega ühendust.</p>
                    </div>

                    <div class=""signature"" style=""text-align: center; margin-top: 40px;"">
                        <p style=""font-size: 14px; color: #888;"">See arve on genereeritud automaatselt, seetõttu ei ole allkiri vajalik.</p>
                    </div>
                </div>
                </body>
                </html>";
            SendEmail(currentUser.Email, "Arve", body);
            return Ok(waterMeter);
        }

        private static string SendEmail(string email, string subject, string body)
        {
            try
            {
                SmtpClient smtpClient = new("smtp.mailersend.net")
                {
                    Port = 587,
                    Credentials = new NetworkCredential("MS_J6xeQZ@trial-3z0vklo1yy7g7qrx.mlsender.net", "ICkuQVllloYW5EF1"),
                    EnableSsl = true
                };
                MailMessage mailMessage = new()
                {
                    From = new MailAddress("MS_J6xeQZ@trial-3z0vklo1yy7g7qrx.mlsender.net", "WaterMeter"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(email);
                smtpClient.Send(mailMessage);
                return "Email sent successfully!";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }
    }
}
