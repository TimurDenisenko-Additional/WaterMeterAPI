using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WaterMeterAPI.Models;
using WaterMeterAPI.Models.DB;

namespace WaterMeterAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class AccountModelController(DBContext DB, IMemoryCache memoryCache) : ControllerBase
    {
        private IMemoryCache _memoryCache { get; } = memoryCache;

        private AccountModel? TryGetCurrentUser() =>
            ((GetCurrentUser() as OkObjectResult)?.Value as AccountModel ?? null);
        private void SetCurrentUser(AccountModel? user)
        {
            if (user != null)
                _memoryCache.Set("currentUser", user);
            else
                _memoryCache.Remove("currentUser");
        }

        // GET: AccountModel
        [HttpGet]
        public List<AccountModel> GetAccounts() => [.. DB.Accounts];

        // GET: AccountModel/id
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAccountModel(int id) => await DB.Accounts.ElementAtNoTrack(id) == null ? 
            BadRequest(new { message = "Kasutajat ei leitud" }) : Ok(await DB.Accounts.ElementAtNoTrack(id));

        // DELETE: AccountModel/delete/id
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            AccountModel? AccountModel = await DB.Accounts.ElementAtWithTrack(id);
            if (AccountModel == null)
                return BadRequest(new { message = "Kasutajat ei leitud" });
            DB.Accounts.Remove(AccountModel);
            await DB.SaveChangesAsync();
            return Ok(DB.Accounts);
        }

        // POST: AccountModel/create/firstname/lastname/gender/email/password
        [HttpPost("create/{firstname}/{lastname}/{gender}/{email}/{password}")]
        public async Task<IActionResult> Create(string firstname, string lastname, string gender, string email, string password)
        {
            if (!await DB.Accounts.Where(x => x.Email == email).AnyAsync())
            {
                await DB.Accounts.AddAsync(new AccountModel(0, firstname, lastname, gender, email, password, "User"));
                await DB.SaveChangesAsync();
                return Ok(DB.Accounts);
            }
            return BadRequest(new { message = "Dubleeritud Kasutaja" });
        }

        // GET: AccountModel/login/username/password
        [HttpGet("login/{email}/{password}")]
        public async Task<IActionResult> Login(string email, string password)
        {
            AccountModel? checkingAccountModel = await DB.Accounts.FirstOrDefaultAsync(x => x.Email == email);
            if (checkingAccountModel != null && checkingAccountModel.Password == password)
            {
                SetCurrentUser(checkingAccountModel);
                return Ok(true);
            }
            else
            {
                return BadRequest("Vale parool või AccountModelnimi");
            }
        }

        // POST: AccountModel/register/firstname/lastname/gender/email/password
        [HttpPost("register/{firstname}/{lastname}/{gender}/{email}/{password}")]
        public async Task<IActionResult> Register(string firstname, string lastname, string gender, string email, string password)
        {
            if (!await DB.Accounts.AnyAsync(x => x.Email == email))
            {
                await Create(firstname, lastname, gender, email, password);
                SetCurrentUser(await DB.Accounts.FirstAsync(x => x.Email == email));
                return Ok(true);
            }
            else
            {
                return BadRequest(new { message = "Dubleeritud AccountModel" });
            }
        }


        // GET: AccountModel/fullName
        [HttpGet("fullName")]
        public IActionResult GetFullName()
        {
            AccountModel? currentUser = TryGetCurrentUser();
            return currentUser == null ? BadRequest(new { message = "Kasutajat ei leitud" }) : Ok($"{currentUser.FirstName} {currentUser.LastName}");
        }

        // GET: AccountModel/isAdmin
        [HttpGet("isAdmin")]
        public bool IsAdmin() =>
            (TryGetCurrentUser()?.Role ?? "") == "Admin";

        // GET: AccountModel/currentUser
        [HttpGet("currentUser")]
        public IActionResult GetCurrentUser()
        {
            _memoryCache.TryGetValue("currentUser", out AccountModel? account);
            return account == null ? BadRequest(new { message = "Kasutajat ei leitud" }) : Ok(account);
        }

        // GET: AccountModel/isAuthorized
        [HttpGet("isAuthorized")]
        public bool IsAuthorized() =>
            TryGetCurrentUser() != null;

        // GET: AccountModel/logout
        [HttpGet("logout")]
        public IActionResult Logout()
        {
            if (IsAuthorized())
            {
                SetCurrentUser(null);
                return Ok(new { message = "Ole välja logitud" });
            }
            else
                return BadRequest(new { message = "Sa ei ole sisse logitud" });
        }
    }
}
