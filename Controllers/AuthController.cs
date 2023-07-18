using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

using TcServer.Storage.Core;
using TcServer.Services;
using TcServer.Attributes;
using TcServer.Core;

// Handles authentication for apps using API

namespace TcServer.Controllers
{
	[ApiController]
	[Route("auth/")]
	public class AuthController : Controller
	{
		[HttpPost("register")]
		[BearerAuthorize(AccountType.Admin)]
		public async Task<IActionResult> Register([FromBody] RegisterDTO dto)
		{
			var accountSvc = Request.HttpContext.RequestServices.GetRequiredService<IAccountService>();

			int res = await accountSvc.RegisterAsync(dto);
			if (res != 0)
				return new BadRequestResult();

			return Ok();
		}

		[HttpPost("regbykey")]
		public async Task<IActionResult> RegisterInitial([FromBody] RegisterInitialDTO dto)
		{
			var accountSvc = Request.HttpContext.RequestServices.GetRequiredService<IAccountService>();
			var configSvc = Request.HttpContext.RequestServices.GetRequiredService<IConfigService>();

			if (!Convert.ToBoolean(configSvc.Access()["Misc:InitialAPIRegAllow"]))
				return BadRequest();

			if (dto.RegKey != configSvc.Access()["Misc:InitialAPIRegKey"])
				return Unauthorized();
			
			AccountType accType = AccountType.Admin;
			if (dto.Type is not null && !Enum.TryParse(dto.Type, out accType))
				return BadRequest();

			int res = await accountSvc.RegisterAsync(new RegisterDTO()
			{
				Type = accType,
				Email = dto.Email,
				Password = dto.Password,
				Name = dto.Name,
				CompanyName = dto.CompanyName
			});
			if (res == 2)
				return BadRequest("CompanyName is missing");
			if (res != 0)
				return BadRequest();

			return Ok();
		}

		[HttpPost("login")]
		public async Task<IActionResult> Login([FromBody] LoginDTO dto)
		{
			var accountSvc = Request.HttpContext.RequestServices.GetRequiredService<IAccountService>();

			var result = await accountSvc.LoginAsync(dto);
			if (result.expiration == -1)
			{
				return new BadRequestResult();
			}
			return Ok(result);
		}

		[HttpPost("authcheck")]
		[BearerAuthorize]
		public IActionResult AuthCheck()
		{
			var entity = (Account)HttpContext.Items["authEntity"]!;
			AuthCheckDTO dto = new()
			{
				Type = entity.Type.ToString(),
				Email = entity.Email,
				Name = entity.Name
			};
			return Ok(dto);
		}

		// @todo refresh token
	}
}
