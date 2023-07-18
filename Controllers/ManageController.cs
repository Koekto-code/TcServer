using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Transactions;
using System.Diagnostics;
using System.Text.Json;

using TcServer;
using TcServer.Storage;
using TcServer.Storage.Core;
using TcServer.Services;
using TcServer.Attributes;
using TcServer.Core;
using TcServer.Core.Remote;
using TcServer.Core.Internal;
using TcServer.Utility;

namespace TcServer.Controllers
{
	[Route("/manage")]
	public class ManageController : Controller
	{
		protected readonly HttpClient devClient;
		protected readonly CoreContext dbCtx;
		protected readonly IConfigService cfgSvc;
		
		public ManageController(IHttpClientFactory factory, CoreContext dbctx, IConfigService cfgsvc)
		{
			devClient = factory.CreateClient("Remote");
			dbCtx = dbctx;
			cfgSvc = cfgsvc;
		}
		
		[HttpPost("devices/add")]
		[CookieAuthorize]
		public async Task<IActionResult> AddDevice(string compname)
		{
			var client = (Account)HttpContext.Items["authEntity"]!;
			
			DeviceAddDTO? dto = null;
			using (StreamReader rd = new(Request.Body))
			{
				string body = await rd.ReadToEndAsync();
				dto = JsonSerializer.Deserialize<DeviceAddDTO>(body);
				if (dto is null)
					return BadRequest();
			}
			
			using (var scope = Transactions.DbAsyncScopeDefault())
			{
				Company? comp = null;
				if (client.Type == AccountType.Company)
				{
					await dbCtx.Entry(client).Reference(c => c.Company).LoadAsync();
					comp = client.Company;
					if (comp!.Name != compname)
						return Unauthorized();
				}
				else if (client.Type == AccountType.Admin)
				{
					comp = await dbCtx.Companies.FirstOrDefaultAsync(c => c.Name == compname);
					if (comp is null)
						return BadRequest();
				}
				else return Unauthorized();
				
				DevResponseDTO? resp = await Methods.GetDeviceKey(devClient, dto.addr);
				if (resp is null || !resp.success)
					return BadRequest();
				
				Device? exist = await dbCtx.Devices.FirstOrDefaultAsync(d => d.SerialNumber == resp.data);
				if (exist is not null)
				{
					if (exist.CompanyId != comp.Id)
						return BadRequest();
					
					// this may be useful when the device is inaccessible via its previous URI
					exist.Address = dto.addr;
					exist.Password = dto.pass;
				}
				else
				{
					await dbCtx.Devices.AddAsync(new()
					{
						SerialNumber = resp.data!,
						Address = dto.addr,
						Password = dto.pass,
						CompanyId = comp.Id,
						InnerId = comp.NextDeviceId++
					});
				}
				
				await dbCtx.SaveChangesAsync();
				scope.Complete();
			}
			
			var updSvc = Request.HttpContext.RequestServices.GetRequiredService<IUpdateService>();
			await updSvc.UpdateDevicesStat();
			
			return Ok();
		}
		
		[HttpPost("devices/delete")]
		[CookieAuthorize]
		public async Task<IActionResult> DeleteDevices(string compname, [FromBody] int[] innerIds)
		{
			var client = (Account)HttpContext.Items["authEntity"]!;
			
			using (var scope = Transactions.DbAsyncScopeDefault())
			{
				Company? comp = null;
				if (client.Type == AccountType.Company)
				{
					await dbCtx.Entry(client).Reference(c => c.Company).LoadAsync();
					comp = client.Company;
					if (comp!.Name != compname)
						return Unauthorized();
				}
				else if (client.Type == AccountType.Admin)
				{
					comp = await dbCtx.Companies.FirstOrDefaultAsync(c => c.Name == compname);
					if (comp is null)
						return BadRequest();
				}
				else return Unauthorized();
				
				await dbCtx.Entry(comp).Collection(c => c.Devices).LoadAsync();
				Dictionary<int, Device> idDevMap = comp.Devices.ToDictionary(d => d.InnerId);
				foreach (int id in innerIds)
				{
					if (!idDevMap.ContainsKey(id))
						continue;
					dbCtx.Devices.Remove(idDevMap[id]);
				}
				
				await dbCtx.SaveChangesAsync();
				scope.Complete();
			}
			return Ok();
		}
		
		[HttpPost("devices/resetcallback")]
		[CookieAuthorize]
		public async Task<IActionResult> ResetDevicesCallback(string compname, [FromBody] int[] innerIds)
		{
			string hostAddr = cfgSvc.Access()["Host:Address"]!;
			
			var remSvc = HttpContext.RequestServices.GetRequiredService<IRemoteService>();
			var tasks = new List<Task<DevResponseDTO?>>();
			var devices = new List<Device>();
			
			using (var scope = Transactions.DbAsyncScopeDefault())
			{
				Company? comp = await dbCtx.Companies.FirstOrDefaultAsync(c => c.Name == compname);
				if (comp is null)
					return BadRequest();
				
				foreach (int i in innerIds)
				{
					Device? dev = await dbCtx.Devices
						.Where(d => d.CompanyId == comp.Id)
						.FirstOrDefaultAsync(d => d.InnerId == i);
					
					if (dev?.Password is null)
						continue;
					
					devices.Add(dev);
					tasks.Add(remSvc.SetIdentifyCallback
					(
						dev.Address, dev.Password,
						new()
						{
							// clear all callbacks
							callbackUrl = string.Empty
						}
					));
				}
				scope.Complete();
			}
			
			await Task.WhenAll(tasks);
			tasks.Clear();
			
			foreach (var dev in devices)
			{
				tasks.Add(remSvc.SetIdentifyCallback
				(
					dev.Address, dev.Password!,
					new()
					{
						callbackUrl = hostAddr + "/remote/callback",
						
						// need base64 to gather employees photos right from callbacks
						base64Enable = 2
					}
				));
			}
			await Task.WhenAll(tasks);
			return Ok();
		}
		
		[HttpPost("employees/delete")]
		[CookieAuthorize]
		public async Task<IActionResult> DeleteEmployees(string compname, [FromBody] int[] innerIds)
		{
			var client = (Account)HttpContext.Items["authEntity"]!;
			
			using (var scope = Transactions.DbAsyncScopeDefault())
			{
				var parse = await Utility.ParseViewpath(this, compname, null, client, false);
				if (parse?.Company is null)
					return BadRequest();
				
				var comp = parse.Company;
				await dbCtx.Entry(comp).Collection(c => c.Employees).LoadAsync();
				var emplMap = comp.Employees.ToDictionary(e => e.InnerCompId);
				
				foreach (int id in innerIds)
				{
					if (!emplMap.ContainsKey(id))
						continue;

					dbCtx.Employees.Remove(emplMap[id]);
				}
				
				await dbCtx.SaveChangesAsync();
				scope.Complete();
			}
			return Ok();
		}

		[HttpPost("employees/transfer")]
		[CookieAuthorize]
		public async Task<IActionResult> TransferEmployees(string compname, string destination, [FromBody] int[] innerIds)
		{
			var client = (Account)HttpContext.Items["authEntity"]!;
			
			using (var scope = Transactions.DbAsyncScopeDefault())
			{
				var parse = await Utility.ParseViewpath(this, compname, null, client, false);
				if (parse?.Company is null)
					return BadRequest();
				
				var comp = parse.Company;
				await dbCtx.Entry(comp).Collection(c => c.Employees).LoadAsync();
				var emplMap = comp.Employees.ToDictionary(e => e.InnerCompId);
				
				var destUnit = await dbCtx.Units
					.Where(u => u.CompanyId == parse.Company.Id)
					.FirstOrDefaultAsync(u => u.Name == destination);
				if (destUnit is null)
					return BadRequest();
				
				foreach (int id in innerIds)
				{
					if (!emplMap.ContainsKey(id))
						continue;
					var empl = emplMap[id];
					empl.UnitId = destUnit.Id;
				}
				
				await dbCtx.SaveChangesAsync();
				scope.Complete();
			}
			return Ok();
		}
		
		[HttpPost("employees/add")]
		[CookieAuthorize]
		public async Task<IActionResult> AddEmployee
		(
			string compname, string unitname,
			string name, string title, string? addr, int dev
		) {
			var client = (Account)HttpContext.Items["authEntity"]!;
			
			using (var scope = Transactions.DbAsyncScopeDefault())
			{
				var parse = await Utility.ParseViewpath(this, compname, unitname, client, false);
				if (parse?.Active is null)
					return BadRequest();
				
				Unit unit = parse.Active;
				
				// get free innerId for employee within Company
				List<int> innerIds = await dbCtx.Employees
					.Where(e => e.CompanyId == unit.CompanyId)
					.Select(e => e.InnerCompId)
					.ToListAsync();
				innerIds.Sort();

				int idSel = 1;
				for (int i = 0; i < innerIds.Count; ++i) {
					if (innerIds[i] != idSel) break;
					++idSel;
				}
				
				Device? device = await dbCtx.Devices
					.Where(d => d.CompanyId == unit.CompanyId)
					.FirstOrDefaultAsync(d => d.InnerId == dev);
				
				if (device?.Password is null)
					return BadRequest();
				
				PersonResponseDTO? resp = await Methods.AddEmployee (
					devClient, device.Address, device.Password,
					new() {
						id = idSel.ToString(),
						name = name
					}
				);

				Employee empl = new()
				{
					UnitId = unit.Id,
					CompanyId = unit.CompanyId,
					InnerCompId = idSel,
					DeviceId = device.Id,
					JobTitle = title,
					Name = name,
					HomeAddress = (addr is null || addr == "") ? null : addr,
					EntryDate = DateTime.Today.ToString("yyyy-MM-dd"),
					
					// the employee can be still added without sync with remote device
					// (considering it's just disabled if so)
					RemoteSynchronized = resp?.success ?? false
				};

				await dbCtx.Employees.AddAsync(empl);
				await dbCtx.SaveChangesAsync();
				
				scope.Complete();
			}
			
			return Ok();
		}
		
		[HttpPost("unit/add")]
		[CookieAuthorize]
		public async Task<IActionResult> AddUnit(string compname, string name)
		{
			if (name?.Contains('.') ?? true)
				return BadRequest();

			var client = (Account)HttpContext.Items["authEntity"]!;
			
			var parse = await Utility.ParseViewpath(this, compname, null, client, false);
			if (parse?.Company is null)
				return BadRequest();

			var unit = new Unit()
			{
				CompanyId = parse.Company.Id,
				Name = name
			};
			await dbCtx.Units.AddAsync(unit);
			await dbCtx.SaveChangesAsync();
			
			return Ok();
		}

		[HttpPost("unit/delete")]
		[CookieAuthorize]
		public async Task<IActionResult> DeleteUnit(string compname, string unitname)
		{
			var client = (Account)HttpContext.Items["authEntity"]!;

			var parse = await Utility.ParseViewpath(this, compname, unitname, client, false);
			if (parse?.Active is null)
				return BadRequest();
			
			dbCtx.Units.Remove(parse.Active);
			await dbCtx.SaveChangesAsync();
			
			return Ok();
		}

		[HttpPost("unit/rename")]
		[CookieAuthorize]
		public async Task<IActionResult> RenameUnit(string compname, string unitname, string newname)
		{
			var client = (Account)HttpContext.Items["authEntity"]!;

			var parse = await Utility.ParseViewpath(this, compname, unitname, client, false);
			if (parse?.Active is null)
				return BadRequest();

			parse.Active.Name = newname;
			await dbCtx.SaveChangesAsync();
			
			return Ok();
		}
		
		[HttpPost("comp/delete")]
		[CookieAuthorize]
		public async Task<IActionResult> DeleteCompany(string compname)
		{
			var client = (Account)HttpContext.Items["authEntity"]!;

			var parse = await Utility.ParseViewpath(this, compname, null, client, false);
			if (parse?.Company is null)
				return BadRequest();
			
			await dbCtx.Entry(parse.Company).Reference(c => c.Account).LoadAsync();
			dbCtx.Accounts.Remove(parse.Company.Account);
			await dbCtx.SaveChangesAsync();
			
			return Ok();
		}
		
		[HttpPost("comp/settimezone")]
		[CookieAuthorize]
		public async Task<IActionResult> SetCompTimeZone(string compname, double offset)
		{
			var client = (Account)HttpContext.Items["authEntity"]!;

			var parse = await Utility.ParseViewpath(this, compname, null, client, false);
			if (parse?.Company is null)
				return BadRequest();
			
			parse.Company.GMTOffset = offset;
			await dbCtx.SaveChangesAsync();
			
			return Ok();
		}
	}
}