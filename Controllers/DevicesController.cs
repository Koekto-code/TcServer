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

// Display devices and allow to add/remove them on this page

namespace TcServer.Controllers
{
	[Route("/devices")]
	public class DevicesController: Controller
	{
		[CookieAuthorize]
		public async Task<IActionResult> Index(string? compname)
		{
			if (compname is null) return BadRequest();
			
			var svcprov = HttpContext.RequestServices;
			var updSvc = svcprov.GetRequiredService<IUpdateService>();
			var dbCtx = svcprov.GetRequiredService<CoreContext>();
			
			var client = (Account)HttpContext.Items["authEntity"]!;
			Company? comp = null;
			
			if (client.Type == AccountType.Company)
			{
				await dbCtx.Entry(client).Reference(c => c.Company).LoadAsync();
				comp = client.Company!;
				if (comp.Name != compname)
					return Unauthorized();
			}
			else if (client.Type == AccountType.Admin)
			{
				comp = await dbCtx.Companies.FirstOrDefaultAsync(c => c.Name == compname);
				if (comp is null)
					return BadRequest();
			}
			else return BadRequest();
			
			await updSvc.UpdateDevicesStat();
			await dbCtx.Entry(comp).Collection(c => c.Devices).LoadAsync();

			Views.Devices.Index.ViewData viewdata = new()
			{
				Devices = comp.Devices.ToList(),
				DevStat = dbCtx.DevicesStat,
				CompName = compname
			};
			return View(viewdata);
		}
	}
}