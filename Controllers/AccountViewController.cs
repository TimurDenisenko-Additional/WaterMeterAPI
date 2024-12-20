﻿using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;
using WaterMeterAPI.Migrations;
using WaterMeterAPI.Models;

namespace WaterMeterAPI.Controllers
{
    public class AccountViewController(IHttpClientFactory httpClientFactory) : Controller
    {
        private readonly HttpClient client = httpClientFactory.CreateClient("ApiClient");

        public async Task<Tuple<bool, string?, T?>> ApiRequest<T>(string request = "", string action = "Get", AccountModel? model = null) where T : class
        {
            try
            {
                HttpResponseMessage response = new();
                switch (action)
                {
                    case "Get":
                        response = await client.GetAsync($"/Account/{request}");
                        break;
                    case "Delete":
                        response = await client.DeleteAsync($"/Account/{request}");
                        break;
                    case "Post":
                        response = await client.PostAsync($"/Account/{request}", new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json"));
                        break;
                    default:
                        break;
                }
                string data = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                    return Tuple.Create<bool, string?, T?>(false, data, null);
                return Tuple.Create<bool, string?, T?>(true, "Ok", JsonConvert.DeserializeObject<T>(data));
            }
            catch (Exception ex)
            {
                return Tuple.Create<bool, string?, T?>(false, ex.Message, null);
            }
        }
        public ActionResult Error(string error = "Siin on üks tõsine probleem. Proovige hiljem uuesti.")
        {
            return View(error);
        }

        // GET: AccountView/Index
        public async Task<ActionResult> Index()
        {
            Tuple<bool, string?, AccountModel[]?> accounts = await ApiRequest<AccountModel[]>();
            if (!accounts.Item1)
                return View("Error", accounts.Item2);
            return View(accounts.Item3);
        }

        // GET: AccountView/Details/5
        public async Task<ActionResult> Details(int id)
        {
            Tuple<bool, string?, AccountModel?> account = await ApiRequest<AccountModel>($"{id}");
            if (!account.Item1)
                return View("Error", account.Item2);
            return View(account.Item3);
        }

        // GET: AccountView/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: AccountView/Create
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<ActionResult> Create(AccountModel accountModel)
        {
            try
            {
                Tuple<bool, string?, AccountModel[]?> account = await ApiRequest<AccountModel[]>($"create", "Post", accountModel);
                if (!account.Item1)
                    return View("Error", account.Item2);
                return View(nameof(Index), account.Item3);
            }
            catch
            {
                return View();
            }
        }

        // GET: AccountView/Delete/5
        public async Task<ActionResult> Delete(int id)
        {
            Tuple<bool, string?, AccountModel?> account = await ApiRequest<AccountModel>($"{id}");
            if (!account.Item1)
                return View("Error", account.Item2);
            return View(account.Item3);
        }

        // POST: AccountView/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(int id, IFormCollection collection)
        {
            Tuple<bool, string?, AccountModel[]?> account = await ApiRequest<AccountModel[]>($"delete/{id}", "Delete");
            if (!account.Item1)
                return View("Error", account.Item2);
            return View(nameof(Index), account.Item3);
        }

        // GET: AccountView/Register
        public ActionResult Register()
        {
            return View();
        }

        // POST: AccountView/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(AccountModel acc)
        {
            Tuple<bool, string?, string?> checkPassword = await ApiRequest<string>($"register", "POST", acc);
            if (!checkPassword.Item1)
                return View("Error", checkPassword.Item2);
            ViewData.Model = (await ApiRequest<AccountModel>($"currentUser")).Item3;
            return RedirectToAction("Index", "WaterMeterView");
        }

        // GET: AccountView/Login
        public ActionResult Login()
        {
            return View();
        }

        // POST: AccountView/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(AccountModel acc)
        {
            Tuple<bool, string?, string?> checkPassword = await ApiRequest<string>($"login/{acc.Email}/{acc.Password}");
            if (!checkPassword.Item1)
                return View("Error", checkPassword.Item2);
            AccountModel? currentUser = (await ApiRequest<AccountModel>($"currentUser")).Item3;
            ViewData.Model = currentUser;
            if (currentUser.Role.Equals("Admin"))
                return RedirectToAction(nameof(Index));
            return RedirectToAction("Index", "WaterMeterView");
        }

        public async Task<ActionResult> Logout()
        {
            Tuple<bool, string?, string?> logout = await ApiRequest<string>($"logout");
            if (!logout.Item1)
                return View("Error", logout.Item2);
            return RedirectToAction(nameof(Register));

        }
    }
}
