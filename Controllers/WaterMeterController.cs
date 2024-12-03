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
        public IActionResult GetWaterMeters() 
        {
            if (!TryGetCurrentUser()?.Role.Equals("Admin") ?? true)
                return BadRequest("See toiming on lubatud ainult administraatorile");
            return Ok(DB.WaterMeters); 
        }

        // GET: WaterMeter/id
        [HttpGet("{id}")]
        public async Task<IActionResult> GetWaterMeter(int id)
        {
            if (!TryGetCurrentUser()?.Role.Equals("Admin") ?? true)
                return BadRequest("See toiming on lubatud ainult administraatorile");
            if (await DB.WaterMeters.ElementAtNoTrack(id) == null)
                return BadRequest("Veenäitu ei leitud");
            return Ok(await DB.WaterMeters.ElementAtNoTrack(id));
        }

        // DELETE: WaterMeter/delete/id
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!TryGetCurrentUser()?.Role.Equals("Admin") ?? true)
                return BadRequest("See toiming on lubatud ainult administraatorile");
            WaterMeterModel? waterMeter = await DB.WaterMeters.ElementAtWithTrack(id);
            if (waterMeter == null)
                return BadRequest("Veenäitu ei leitud");
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
            if (!TryGetCurrentUser()?.Role.Equals("Admin") ?? true)
                return BadRequest("See toiming on lubatud ainult administraatorile");
            if (await DB.Accounts.Where(x => x.Email == email).AnyAsync())
            {
                return await CreateWaterMeter(email, address, apartament, date, coldWater, warmWater, paymentStatus);
            }
            return BadRequest("Kasutaja ei leitud");
        }

        // POST: WaterMeter/createGivenNonExistentEmail/email/address/apartament/date/coldWater/warmWater/paymentStatus
        [HttpPost("createGivenNonExistentEmail/{email}/{address}/{apartament}/{date}/{coldWater}/{warmWater}/{paymentStatus}")]
        public async Task<IActionResult> CreateGivenNonExistentEmail(string email, string address, int apartament, DateTime date, int coldWater, int warmWater, bool paymentStatus)
        {
            if (!TryGetCurrentUser()?.Role.Equals("Admin") ?? true)
                return BadRequest("See toiming on lubatud ainult administraatorile");
            return await CreateWaterMeter(email, address, apartament, date, coldWater, warmWater, paymentStatus);
        }

        // POST: WaterMeter/addWaterMeter/address/apartament/coldWater/warmWater
        [HttpPost("addWaterMeter/{address}/{apartament}/{coldWater}/{warmWater}")]
        public async Task<IActionResult> AddWaterMeter(string address, int apartament, int coldWater, int warmWater)
        {
            AccountModel? currentUser = TryGetCurrentUser();
            if (currentUser == null)
                return BadRequest("Te ei ole sisse logitud");
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
                return BadRequest("Te ei ole sisse logitud");
            WaterMeterModel? waterMeter = await DB.WaterMeters.FirstOrDefaultAsync(x => x.Date.Year == year && x.Date.Month == month && x.Email == currentUser.Email);
            if (waterMeter == null)
                return BadRequest("Sellel inimesel puuduvad selle kuu veemõõtjate näidud");

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

        // GET: WaterMeter/sendReminder/email
        [HttpGet("sendReminder/{email}")]
        public async Task<IActionResult> SendReminder(string email)
        {
            if (!TryGetCurrentUser()?.Role.Equals("Admin") ?? true)
                return BadRequest("See toiming on lubatud ainult administraatorile");
            WaterMeterModel[] waterMeters = await DB.WaterMeters.Where(x => x.Email == email && !x.PaymentStatus).ToArrayAsync();
            if (!waterMeters.Any())
                return BadRequest("Kõik arved on makstud");
            string subject = "Mäleta: Teie vee näitude maksmine";
            string body = $@"
                <html>
                <body>
                <div style=""font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px; border-radius: 8px;"">
                <h2 style=""text-align: center; color: #333;"">Tere, {email}!</h2>
                <p style=""font-size: 16px; color: #555;"">Teie järgmised vee näidud on endiselt tasumata:</p>
                
                <table style=""width: 100%; border-collapse: collapse; margin-top: 20px;"">
                    <thead>
                        <tr style=""background-color: #f1f1f1;"">
                            <th style=""padding: 10px; border: 1px solid #ddd; text-align: left;"">Aadress</th>
                            <th style=""padding: 10px; border: 1px solid #ddd; text-align: left;"">Korter</th>
                            <th style=""padding: 10px; border: 1px solid #ddd; text-align: left;"">Külm vesi (m³)</th>
                            <th style=""padding: 10px; border: 1px solid #ddd; text-align: left;"">Küppa vesi (m³)</th>
                            <th style=""padding: 10px; border: 1px solid #ddd; text-align: left;"">Maksmise staatus</th>
                        </tr>
                    </thead>
                        <tbody>";
            foreach (var meter in waterMeters)
            {
                body += $@"
                    <tr>
                        <td style=""padding: 10px; border: 1px solid #ddd;"">{meter.Address}</td>
                        <td style=""padding: 10px; border: 1px solid #ddd;"">{meter.Apartment}</td>
                        <td style=""padding: 10px; border: 1px solid #ddd;"">{meter.ColdWater} m³</td>
                        <td style=""padding: 10px; border: 1px solid #ddd;"">{meter.WarmWater} m³</td>
                        <td style=""padding: 10px; border: 1px solid #ddd;"">Makse puudub</td>
                    </tr>";
            }
            body += $@"
                </tbody>
                </table>

                <p style=""font-size: 16px; color: #555; margin-top: 20px;"">
                    Palun tasuge need arved niipea kui võimalik. Kui teil on küsimusi, võtke meiega ühendust.
                </p>

                <div style=""text-align: center; margin-top: 30px;"">
                    <p style=""font-size: 14px; color: #777;"">Kui teil on küsimusi, võtke meiega ühendust.</p>
                    <p style=""font-size: 14px; color: #888;"">Lugupidamisega, Teie Teenuse Pakkuja</p>
                </div>
            </div>
            </body>
            </html>";
            SendEmail(email, subject, body);
            return Ok(waterMeters);
        }

        // GET: WaterMeter/getWaterMeterForYear/year
        [HttpGet("getWaterMeterForYear/{year}")]
        public async Task<IActionResult> GetWaterMeterForYear(int year)
        {
            if (!TryGetCurrentUser()?.Role.Equals("Admin") ?? true)
                return BadRequest("See toiming on lubatud ainult administraatorile");
            WaterMeterModel[] waterMeters = await DB.WaterMeters.Where(x => x.Date.Year == year).ToArrayAsync();
            if (!waterMeters.Any())
                return BadRequest("Selle aasta veenäitu ei leitud");
            return Ok(waterMeters);
        }

        // GET: WaterMeter/getReminder/id
        [HttpGet("getReminder/{id}")]
        public async Task<IActionResult> GetReminder(int id)
        {
            WaterMeterModel? waterMeter = await DB.WaterMeters.ElementAtNoTrack(id);
            if (waterMeter == null)
                return BadRequest("ID-andmetega veenäitu ei leitud");
            if (waterMeter.PaymentStatus)
                return BadRequest("Need veenäidud on tasutud");
            string subject = "Mäleta: Teie vee näitude maksmine";
            string body = $@"
                <html>
                <body>
                <div style=""font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px; border-radius: 8px;"">
                    <h2 style=""text-align: center; color: #333;"">Tere, {waterMeter.Email}!</h2>
                    <p style=""font-size: 16px; color: #555;"">Teie vee näidud on endiselt tasumata. Palun vaadake järgmisi andmeid:</p>

                    <p style=""font-size: 16px; color: #555;"">
                        <strong>Aadress:</strong> {waterMeter.Address}<br />
                        <strong>Korter:</strong> {waterMeter.Apartment}<br />
                        <strong>Külm vesi:</strong> {waterMeter.ColdWater} m³<br />
                        <strong>Küppa vesi:</strong> {waterMeter.WarmWater} m³<br />
                        <strong>Maksmise staatus:</strong> Makse puudub
                    </p>

                    <p style=""font-size: 16px; color: #555; margin-top: 20px;"">
                        Palun tasuge need arved niipea kui võimalik. Kui teil on küsimusi, võtke meiega ühendust.
                    </p>

                    <div style=""text-align: center; margin-top: 30px;"">
                        <p style=""font-size: 14px; color: #777;"">Kui teil on küsimusi, võtke meiega ühendust.</p>
                        <p style=""font-size: 14px; color: #888;"">Lugupidamisega, Teie Teenuse Pakkuja</p>
                    </div>
                </div>
                </body>
                </html>";
            SendEmail(waterMeter.Email, subject, body);
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
