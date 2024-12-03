﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WaterMeterAPI.Models;
using WaterMeterAPI.Models.DB;

namespace WaterMeterAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class AccountController(DBContext DB, IMemoryCache memoryCache) : ControllerBase
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

        // GET: Account
        [HttpGet]
        public IActionResult GetAccounts() 
        {
            if (!TryGetCurrentUser()?.Role.Equals("Admin") ?? true)
                return BadRequest("See toiming on lubatud ainult administraatorile");
            return Ok(DB.Accounts);
        }

        // GET: Account/id
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAccountModel(int id)
        {
            if (!TryGetCurrentUser()?.Role.Equals("Admin") ?? true)
                return BadRequest("See toiming on lubatud ainult administraatorile");
            if (await DB.Accounts.ElementAtNoTrack(id) == null)
                return BadRequest("Kasutajat ei leitud");
            return Ok(await DB.Accounts.ElementAtNoTrack(id));
        }

        // DELETE: Account/delete/id
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!TryGetCurrentUser()?.Role.Equals("Admin") ?? true)
                return BadRequest("See toiming on lubatud ainult administraatorile");
            AccountModel? AccountModel = await DB.Accounts.ElementAtWithTrack(id);
            if (AccountModel == null)
                return BadRequest("Kasutajat ei leitud");
            DB.Accounts.Remove(AccountModel);
            await DB.SaveChangesAsync();
            return Ok(DB.Accounts);
        }

        // POST: Account/create/firstname/lastname/gender/email/password
        [HttpPost("create/{firstname}/{lastname}/{gender}/{email}/{password}")]
        public async Task<IActionResult> Create(string firstname, string lastname, string gender, string email, string password)
        {
            if (!TryGetCurrentUser()?.Role.Equals("Admin") ?? true)
                return BadRequest("See toiming on lubatud ainult administraatorile");
            if (!await DB.Accounts.Where(x => x.Email == email).AnyAsync())
            {
                await DB.Accounts.AddAsync(new AccountModel(0, firstname, lastname, gender, email, password, "User"));
                await DB.SaveChangesAsync();
                return Ok(DB.Accounts);
            }
            return BadRequest("Dubleeritud Kasutaja");
        }

        // GET: Account/login/username/password
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
                return BadRequest("Vale parool või nimi");
            }
        }

        // POST: Account/register/firstname/lastname/gender/email/password
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
                return BadRequest("Dubleeritud kasutajat");
            }
        }


        // GET: Account/fullName
        [HttpGet("fullName")]
        public IActionResult GetFullName()
        {
            AccountModel? currentUser = TryGetCurrentUser();
            return currentUser == null ? BadRequest("Kasutajat ei leitud") : Ok($"{currentUser.FirstName} {currentUser.LastName}");
        }

        // GET: Account/isAdmin
        [HttpGet("isAdmin")]
        public bool IsAdmin() =>
            (TryGetCurrentUser()?.Role ?? "") == "Admin";

        // GET: Account/currentUser
        [HttpGet("currentUser")]
        public IActionResult GetCurrentUser()
        {
            _memoryCache.TryGetValue("currentUser", out AccountModel? account);
            return account == null ? BadRequest("Kasutajat ei leitud") : Ok(account);
        }

        // GET: Account/isAuthorized
        [HttpGet("isAuthorized")]
        public bool IsAuthorized() =>
            TryGetCurrentUser() != null;

        // GET: Account/logout
        [HttpGet("logout")]
        public IActionResult Logout()
        {
            if (IsAuthorized())
            {
                SetCurrentUser(null);
                return Ok(new { message = "Ole välja logitud" });
            }
            else
                return BadRequest("Sa ei ole sisse logitud");
        }
    }
}
