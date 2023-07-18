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

// View and edit name, title, photos etc. of given employee

namespace TcServer.Controllers
{
	[Route("/employee")]
	public class EmployeeController: Controller
	{
		protected readonly CoreContext dbCtx;
		
		public EmployeeController(CoreContext dbctx)
		{
			dbCtx = dbctx;
		}
		
		[CookieAuthorize]
		public async Task<IActionResult> Index(string? compname, int? id)
		{
			if (id is null || compname is null)
				return BadRequest();			

			var client = (Account)HttpContext.Items["authEntity"]!;

			var parse = await Utility.ParseViewpath(this, compname, null, client, false);
			if (parse?.Company is null)
				return BadRequest();

			var employee = await dbCtx.Employees
				.Where(e => e.CompanyId == parse.Company.Id)
				.FirstOrDefaultAsync(e => e.InnerCompId == id);
			if (employee is null)
				return BadRequest();
			
			await dbCtx.Entry(employee).Collection(e => e.Photos).LoadAsync();
			
			var stPhotos = await dbCtx.Photos
				.Where(p => p.DeviceId == employee.DeviceId)
				.Where(p => p.EmployeeId == null)
				.ToListAsync();

			var viewdata = new Views.Employee.Index.ViewData
			{
				Company = parse.Company,
				Employee = employee,
				OwnPhotos = employee.Photos.ToList(),
				StrangerPhotos = stPhotos
			};

			return View(viewdata);
		}
	}
}