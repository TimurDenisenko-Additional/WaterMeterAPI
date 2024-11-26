using Microsoft.AspNetCore.Mvc;
using WaterMeterAPI.Models;
using WaterMeterAPI.Models.DB;

namespace WaterMeterAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class AccountModelController : ControllerBase
    {
        private readonly DBContext DB;
        public AccountModelController(DBContext DB)
        {
            this.DB = DB;
        }

        // GET: AccountModel
        [HttpGet]
        public List<AccountModel> GetAccounts() => DB.Accounts.ToList();

        // GET: AccountModel/id
        [HttpGet("{id}")]
        public IActionResult GetAccountModel(int id) => DB.Accounts.ElementOrDefault(id) == null ? BadRequest(new { message = "Kasutajat ei leitud" }) : Ok(DB.Accounts.ElementOrDefault(id));

        // DELETE: AccountModel/delete/id
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            AccountModel? AccountModel = await DB.Accounts.ElementOrDefault(id);
            if (AccountModel == null)
                return BadRequest(new { message = "Kasutajat ei leitud" });
            DB.Accounts.ToList().RemoveAt(id);
            DB.SaveChanges();
            return Ok(DB.Accounts);
        }

        // POST: AccountModel/create/firstname/lastname/gender/email/password
        [HttpPost("create/{firstname}/{lastname}/{gender}/{email}/{password}")]
        public IActionResult Create(string firstname, string lastname, string gender, string email, string password)
        {
            if (!DB.Accounts.Where(x => x.Email == email).Any())
            {
                DB.Accounts.Add(new AccountModel(0, firstname, lastname, gender, email, password, "User"));
                DB.SaveChanges();
                return Ok(DB.Accounts);
            }
            return BadRequest(new { message = "Dubleeritud Kasutaja" });
        }

        // GET: AccountModel/login/username/password
        [HttpGet("login/{email}/{password}")]
        public IActionResult Login(string email, string password)
        {
            AccountModel? checkingAccountModel = DB.Accounts.Where(x => x.Email == email).ElementAtOrDefault(0);
            if (checkingAccountModel != null && checkingAccountModel.Password == password)
            {
                //isLogged = true;
                //currentAccountModelId = checkingAccountModel.Id;
                return Ok(true);
            }
            else
            {
                return BadRequest("Vale parool või AccountModelnimi");
            }
        }

        // POST: AccountModel/register/firstname/lastname/gender/email/password
        [HttpPost("register/{firstname}/{lastname}/{gender}/{email}/{password}")]
        public IActionResult Register(string firstname, string lastname, string gender, string email, string password)
        {
            if (!DB.Accounts.Where(x => x.Email == email).Any())
            {
                Create(firstname, lastname, gender, email, password);
                //isLogged = true;
                //currentAccountModelId = DB.Accounts.Count();
                return Ok(true);
            }
            else
            {
                //isLogged = false;
                //currentAccountModelId = -1;
                return BadRequest(new { message = "Dubleeritud AccountModel" });
            }
        }

        //// GET: AccountModel/logout
        //[HttpGet("logout")]
        //public string Logout()
        //{
        //    if (isLogged)
        //    {
        //        isLogged = false;
        //        currentAccountModelId = -1;
        //        return "Ole välja logitud";
        //    }
        //    else
        //        return "Sa ei ole sisse logitud";
        //}

        //// GET: AccountModel/get-current
        //[HttpGet("get-current")]
        //public IActionResult GetCurrent() => DB.Accounts.ElementOrDefault(currentAccountModelId) == null ? NotFound(new { message = "AccountModelt ei leitud" }) : Ok(DB.Accounts.ElementOrDefault(currentAccountModelId));

        //// GET: AccountModel/is-auth
        //[HttpGet("is-auth")]
        //public bool IsLogged() => isLogged;

        //// GET: AccountModel/is-admin
        //[HttpGet("is-admin")]
        //public async Task<bool> IsAdmin() => (await DB.Accounts.ElementOrDefault(currentAccountModelId) ?? new()).IsAdmin;
    }
}
