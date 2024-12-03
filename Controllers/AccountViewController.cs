using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WaterMeterAPI.Migrations;
using WaterMeterAPI.Models;

namespace WaterMeterAPI.Controllers
{
    public class AccountViewController(IHttpClientFactory httpClientFactory) : Controller
    {
        private readonly HttpClient client = httpClientFactory.CreateClient("ApiClient");

        private async Task<Tuple<bool, string?, T?>> ApiRequest<T>(string request = "") where T : class
        {
            HttpResponseMessage response = await client.GetAsync($"/Account/{request}");
            string data = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                return Tuple.Create<bool, string?, T?>(false, JsonConvert.DeserializeObject<string>(data), null);
            return Tuple.Create<bool, string?, T?>(true, "Ok", JsonConvert.DeserializeObject<T>(data));
        }
        public async Task<ActionResult> Index()
        {
            Tuple<bool, string?, AccountModel[]?> accounts = await ApiRequest<AccountModel[]>();
            if (!accounts.Item1)
                return RedirectToAction("Error", accounts.Item2);
            return View(accounts.Item3);
        }

        // GET: AccountViewController/Details/5
        public async Task<ActionResult> Details(int id)
        {
            Tuple<bool, string?, AccountModel?> account = await ApiRequest<AccountModel>($"{id}");
            if (!account.Item1)
                return RedirectToAction("Error", account.Item2);
            return View(account.Item3);
        }

        // GET: AccountViewController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: AccountViewController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: AccountViewController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: AccountViewController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: AccountViewController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: AccountViewController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
