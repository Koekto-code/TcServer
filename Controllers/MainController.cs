using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Diagnostics;
using System.Text.Json;

using TcServer;
using TcServer.Storage;
using TcServer.Storage.Core;
using TcServer.Services;
using TcServer.Attributes;
using TcServer.Core;
using TcServer.Utility;
using Microsoft.EntityFrameworkCore;

namespace TcServer.Controllers
{
	[Route("/")]
	public class MainController : Controller
	{
		[CookieAuthorizeOptional]
		public IActionResult Index()
		{
			if (HttpContext.Items["authEntity"] is null)
				return RedirectToAction("Login");
			return RedirectToAction("Index", "Schedule");
		}
		
		[HttpGet("register")]
		[CookieAuthorize(AccountType.Admin)]
		public IActionResult Register()
		{
			return View();
		}

		[HttpGet("login")]
		[CookieAuthorizeOptional]
		public IActionResult Login()
		{
			if (HttpContext.Items["authEntity"] is not null)
				return RedirectToAction("Index", "Schedule");
			return View();
		}

		[HttpGet("logout")]
		public IActionResult Logout()
		{
			foreach (var cook in Request.Cookies.Keys)
				Response.Cookies.Delete(cook);
			
			return RedirectToAction("Index");
		}

		[HttpPost("register")]
		[CookieAuthorize(AccountType.Admin)]
		public async Task<IActionResult> RegisterPost()
		{
			var accSvc = Request.HttpContext.RequestServices.GetRequiredService<IAccountService>();

			var regdto = new RegisterDTO()
			{
				Email = Request.Form["email"]!,
				Password = Request.Form["password"]!,
				Name = Request.Form["name"]!,
				CompanyName = Request.Form["company_name"],
			};
			regdto.Type = regdto.CompanyName is null ? AccountType.Default : AccountType.Company;

			int res = await accSvc.RegisterAsync(regdto);
			if (res != 0)
				return BadRequest();

			return Ok();
		}

		[HttpPost("login")]
		public async Task<IActionResult> LoginPost()
		{
			var accountSvc = Request.HttpContext.RequestServices.GetRequiredService<IAccountService>();

			string email = Request.Form["email"]!;
			string password = Request.Form["password"]!;

			var dto = new LoginDTO()
			{
				Email = email,
				Password = password
			};

			var concolor = Console.ForegroundColor;

			var result = await accountSvc.LoginAsync(dto);
			if (result.expiration == -1)
				return BadRequest();

			var cookieOpts = new CookieOptions()
			{
				HttpOnly = true,
				Expires = DateTime.FromFileTime(result.expiration)
			};

			Response.Cookies.Append("accessToken", result.accessToken, cookieOpts);
			return RedirectToAction("Index");
		}
		
		[HttpPost("changepwd")]
		public async Task<IActionResult> ChangePwdPost()
		{
			// @todo
			var accountSvc = Request.HttpContext.RequestServices.GetRequiredService<IAccountService>();

			string email = Request.Form["email"]!;
			string password = Request.Form["password"]!;

			var dto = new LoginDTO()
			{
				Email = email,
				Password = password
			};

			var concolor = Console.ForegroundColor;

			var result = await accountSvc.LoginAsync(dto);
			if (result.expiration == -1)
				return BadRequest();

			var cookieOpts = new CookieOptions()
			{
				HttpOnly = true,
				Expires = DateTime.FromFileTime(result.expiration)
			};

			Response.Cookies.Append("accessToken", result.accessToken, cookieOpts);
			return RedirectToAction("Index");
		}

		[HttpPost("logout")]
		public IActionResult LogoutPost()
		{
			Response.Cookies.Delete("accessToken");
			return RedirectToAction("Index");
		}

		// @todo limit refreshing frequency
		[CookieAuthorize]
		[HttpPost("refresh")]
		public async Task<IActionResult> RefreshTokenPost()
		{
			var accountSvc = Request.HttpContext.RequestServices.GetRequiredService<IAccountService>();

			string email = (string)HttpContext.Items["authEmail"]!;
			var result = await accountSvc.RefreshAsync(new RefreshDTO() { Email = email });

			var cookieOpts = new CookieOptions()
			{
				HttpOnly = true,
				Expires = DateTime.FromFileTime(result.expiration)
			};
			Response.Cookies.Append("accessToken", result.accessToken, cookieOpts);
			return Ok();
		}
	}
}
