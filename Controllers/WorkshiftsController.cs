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
	[Route("/workshifts")]
	public class WorkshiftsController : Controller
	{
		protected readonly CoreContext dbCtx;
		
		public WorkshiftsController(CoreContext dbctx)
		{
			dbCtx = dbctx;
		}
		
		[CookieAuthorize]
		public async Task<IActionResult> Index(string? compname = null)
		{
			var client = (Account)HttpContext.Items["authEntity"]!;
			
			var pdata = await Utility.ParseViewpath(this, compname, null, client, false);
			if (pdata?.Company is null)
				return BadRequest();
			
			var comp = pdata.Company;
			await dbCtx.Entry(comp).Collection(c => c.WorkShifts).LoadAsync();
			
			var viewdata = new Views.WorkShifts.Index.ViewData()
			{
				WorkShifts = comp.WorkShifts.ToList()
			};
			return View(viewdata);
		}
	}
}