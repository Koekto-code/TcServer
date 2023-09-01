using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Transactions;
using System.Diagnostics;
using System.Text;
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
		[CookieAuthorize(AccountType.Admin)]
		public async Task<IActionResult> AddDevice(string compname, [FromBody] DeviceAddDTO dto)
		{
			var client = (Account)HttpContext.Items["authEntity"]!;
			Company? comp = null;
			
			using (var scope = Transactions.DbAsyncScopeDefault())
			{
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
				
				DevExtResponseDTO? resp = await Methods.GetDeviceKey(devClient, dto.addr);
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
			
			var updSvc = HttpContext.RequestServices.GetRequiredService<IUpdateService>();
			await updSvc.UpdateDevicesStat(comp.Id);
			
			await dbCtx.SaveRemoteChangesAsync();
			return Ok();
		}
		
		[HttpPost("devices/delete")]
		[CookieAuthorize(AccountType.Admin)]
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
					
					await dbCtx.Entry(idDevMap[id]).Collection(d => d.Photos).LoadAsync();
					dbCtx.Devices.Remove(idDevMap[id]);
				}
				
				await dbCtx.SaveChangesAsync();
				scope.Complete();
			}
			
			await dbCtx.SaveRemoteChangesAsync();
			return Ok();
		}
		
		[HttpPost("devices/resetcallback")]
		[CookieAuthorize(AccountType.Admin)]
		public async Task<IActionResult> ResetDevicesCallback(string compname, [FromBody] int[] innerIds)
		{
			string hostAddr = cfgSvc.Access()["Host:Address"]!;
			
			var remSvc = HttpContext.RequestServices.GetRequiredService<IRemoteService>();
			var tasks = new List<Task<DevExtResponseDTO?>>();
			var devices = new List<Device>();
			Company? comp;
			
			using (var scope = Transactions.DbAsyncScopeRC())
			{
				comp = await dbCtx.Companies.FirstOrDefaultAsync(c => c.Name == compname);
				if (comp is null)
					return BadRequest();
				
				scope.Complete();
			}
			
			var updSvc = Request.HttpContext.RequestServices.GetRequiredService<IUpdateService>();
			await updSvc.UpdateDevicesStat(comp.Id);
			
			using (var scope = Transactions.DbAsyncScopeRC())
			{
				foreach (int i in innerIds)
				{
					Device? dev = await dbCtx.Devices
						.Where(d => d.CompanyId == comp.Id)
						.FirstOrDefaultAsync(d => d.InnerId == i);
					
					if (dev?.Password is null)
						continue;
					
					if (!updSvc.DevStat[dev.SerialNumber])
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
					
					await dbCtx.Entry(emplMap[id]).Collection(e => e.AtdRecords).LoadAsync();
					await dbCtx.Entry(emplMap[id]).Collection(e => e.Photos).LoadAsync();
					dbCtx.Employees.Remove(emplMap[id]);
				}
				
				await dbCtx.SaveChangesAsync();
				scope.Complete();
			}
			
			await dbCtx.SaveRemoteChangesAsync();
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
			
			await dbCtx.SaveRemoteChangesAsync();
			return Ok();
		}
		
		[HttpPost("employees/add")]
		[CookieAuthorize]
		public async Task<IActionResult> AddEmployee(int unitid, [FromBody] EmployeeAddDTO dto)
		{
			var client = (Account)HttpContext.Items["authEntity"]!;
			
			Unit? unit;
			int idSel = 1;
			List<Device> devices;
			
			try
			{
				// if the exception is thrown, chances that
				// an emloyee with same InnerCompId is already added
				
				using (var scope = Transactions.DbAsyncScopeRC())
				{
					unit = await dbCtx.Units.FindAsync(unitid);
					if (unit is null)
						return BadRequest();
					
					if (!await Utility.IsAuthorizedData(this, client, unit))
						return Unauthorized();
					
					// get free innerId for employee within Company
					List<int> innerIds = await dbCtx.Employees
						.Where(e => e.CompanyId == unit.CompanyId)
						.Select(e => e.InnerCompId)
						.ToListAsync();
					innerIds.Sort();

					for (int i = 0; i < innerIds.Count; ++i) {
						if (innerIds[i] != idSel) break;
						++idSel;
					}
					
					devices = await dbCtx.Devices
						.Where(d => d.CompanyId == unit.CompanyId)
						.Where(d => d.Password != null)
						.ToListAsync();
					
					Employee empl = new()
					{
						UnitId = unit.Id,
						CompanyId = unit.CompanyId,
						InnerCompId = idSel,
						JobTitle = dto.JobTitle,
						Name = dto.Name,
						Phone = dto.Phone?.Length > 0 ? dto.Phone : null,
						HomeAddress = dto.HomeAddress?.Length > 0 ? dto.HomeAddress : null,
						Notify = Employee.NotifyMode.None,
						EntryDate = DateTime.Today.ToString("yyyy-MM-dd"),
						
						// guessing sync 'll be successful. change later if not
						RemoteSynchronized = true
					};
					
					await dbCtx.Employees.AddAsync(empl);
					await dbCtx.SaveChangesAsync();
					
					scope.Complete();
				}
			}
			catch (Exception) { return BadRequest(); }
			
			List<Task<PersonResponseDTO?>> tasks = new();
			foreach (var dev in devices)
			{
				await Methods.RemoveEmployees(devClient, dev.Address, dev.Password!, new() { idSel.ToString() });
				
				tasks.Add (
					Methods.AddEmployee (
						devClient, dev.Address, dev.Password!,
						new() {
							id = idSel.ToString(),
							name = dto.Name
						}
					)
				);
			}
			PersonResponseDTO?[] complete = await Task.WhenAll(tasks);
			
			foreach (var res in complete)
			{
				if (!(res?.success == true))
				{
					using (var scope = Transactions.DbAsyncScopeDefault())
					{
						var empl = await dbCtx.Employees
							.Where(e => e.CompanyId == unit.CompanyId)
							.FirstOrDefaultAsync(e => e.InnerCompId == idSel);
						
						if (empl is null)
							return BadRequest();
						
						empl.RemoteSynchronized = false;
						
						await dbCtx.SaveChangesAsync();
						scope.Complete();
					}
					break;
				}
			}
			
			await dbCtx.SaveRemoteChangesAsync();
			return Ok();
		}
		
		[HttpPost("employees/addphoto")]
		[CookieAuthorize]
		public async Task<IActionResult> AssignPhoto(int emplid, int photoid)
		{
			var client = (Account)HttpContext.Items["authEntity"]!;
			Employee? empl;
			Photo? photo;
			Device dev;
			
			using (var scope = Transactions.DbAsyncScopeRC())
			{
				empl = await dbCtx.Employees.FindAsync(emplid);
				if (empl is null)
					return BadRequest();
				
				if (!await Utility.IsAuthorizedData(this, client, empl))
					return Unauthorized();
				
				photo = await dbCtx.Photos.FindAsync(photoid);
				if (photo is null || photo.EmployeeId is not null)
					return BadRequest();
				
				await dbCtx.Entry(photo).Reference(p => p.Device).LoadAsync();
				if (photo.Device.Password is null)
					return BadRequest();
				
				if (!await Utility.IsAuthorizedData(this, client, photo.Device))
					return Unauthorized();
				
				dev = photo.Device;
				scope.Complete();
			}
			
			var remSvc = HttpContext.RequestServices.GetRequiredService<IRemoteService>();
			
			await remSvc.DeletePhoto(dev.Address, dev.Password, empl.InnerCompId.ToString());
			
			// @fixme: deserializes to null on success
			await remSvc.AssignPhoto(dev.Address, dev.Password, new()
			{
				personId = empl.InnerCompId.ToString(),
				faceId = empl.InnerCompId.ToString(),
				base64 = photo.Base64
			});
			
			int photoLimit = Convert.ToInt32(cfgSvc.Access()["Misc:EmployeePhotoLimit"]!);
			
			using (var scope = Transactions.DbAsyncScopeDefault())
			{
				photo = await dbCtx.Photos.FindAsync(photoid);
				if (photo is null || photo.EmployeeId is not null)
					return BadRequest();
				
				List<Photo> ph = await dbCtx.Photos
					.Where(p => p.EmployeeId == photo.EmployeeId)
					.ToListAsync();
				
				var rand = new Random();
				while (ph.Count > photoLimit)
				{
					int rem = rand.Next() % ph.Count;
					dbCtx.Photos.Remove(ph[rem]);
					ph.RemoveAt(rem);
				}
				photo.EmployeeId = empl.Id;
				
				await dbCtx.SaveChangesAsync();
				scope.Complete();
			}
			
			await dbCtx.SaveRemoteChangesAsync();
			return Ok();
		}
		
		[HttpPost("employees/update")]
		[CookieAuthorize]
		public async Task<IActionResult> UpdateEmployee(int emplid, [FromBody] EmployeeUpdateDTO dto)
		{
			var client = (Account)HttpContext.Items["authEntity"]!;
			
			Employee? empl;
			List<Device> devices;
			
			bool devSyncNeeded = false;
			
			using (var scope = Transactions.DbAsyncScopeDefault())
			{
				empl = await dbCtx.Employees.FindAsync(emplid);
				if (empl is null)
					return BadRequest();
				
				devices = await dbCtx.Devices
					.Where(d => d.Password != null)
					.ToListAsync();
				
				if (dto.JobTitle is not null)
					empl.JobTitle = dto.JobTitle;
				
				if (dto.Name is not null && dto.Name != empl.Name)
				{
					devSyncNeeded = true;
					empl.Name = dto.Name;
				}
				
				if (dto.IdCard is not null && dto.IdCard != empl.IdCard)
				{
					devSyncNeeded = true;
					empl.IdCard = dto.IdCard == string.Empty ? null : dto.IdCard;
				}
				
				empl.HomeAddress = dto.HomeAddress;
				if (empl.HomeAddress?.Length == 0)
					empl.HomeAddress = null;
				
				empl.Phone = dto.Phone;
				if (empl.Phone?.Length == 0)
					empl.Phone = null;
				
				await dbCtx.SaveChangesAsync();
				scope.Complete();
			}
			
			// sync with device
			bool sync = true;
			if (devSyncNeeded)
			{
				var remSvc = HttpContext.RequestServices.GetRequiredService<IRemoteService>();
				foreach (var dev in devices)
				{
					var updStatus = await remSvc.UpdateEmployee (
						dev.Address, dev.Password!,
						new() {
							id = empl.InnerCompId.ToString(),
							name = empl.Name,
							idcardNum = empl.IdCard
						}
					);
					sync &= updStatus?.success == true;
				}
			}
			
			// keep track of whether the employee should be resynchronized later or not
			// note: if sync is true but empl.RemSync is not, then the employee data should be fully
			// synchronized later so leave this as false
			if (empl.RemoteSynchronized && !sync)
			{
				using (var scope = Transactions.DbAsyncScopeDefault())
				{
					empl = await dbCtx.Employees.FindAsync(emplid);
					if (empl is null)
						return BadRequest();
					
					empl.RemoteSynchronized = sync;
					
					await dbCtx.SaveChangesAsync();
					scope.Complete();
				}
			}
			
			await dbCtx.SaveRemoteChangesAsync();
			return Ok();
		}
		
		[HttpPost("employees/notify")]
		[CookieAuthorize]
		public async Task<IActionResult> SetEmplNotify(string compname, int emplid, uint state)
		{
			var client = (Account)HttpContext.Items["authEntity"]!;
			
			using (var scope = Transactions.DbAsyncScopeDefault())
			{
				var parse = await Utility.ParseViewpath(this, compname, null, client, false);
				if (parse?.Company is null)
					return BadRequest();
				
				var empl = await dbCtx.Employees
					.Where(e => e.CompanyId == parse.Company.Id)
					.FirstOrDefaultAsync(e => e.InnerCompId == emplid);
				
				if (empl is null)
					return BadRequest();
				
				empl.Notify = (Employee.NotifyMode)state;
				
				await dbCtx.SaveChangesAsync();
				scope.Complete();
			}
			
			await dbCtx.SaveRemoteChangesAsync();
			return Ok();
		}
		
		[HttpPost("unit/add")]
		[CookieAuthorize]
		public async Task<IActionResult> AddUnit(string compname, string name)
		{
			if (name?.Contains('.') ?? true)
				return BadRequest();

			var client = (Account)HttpContext.Items["authEntity"]!;
			
			using (var scope = Transactions.DbAsyncScopeDefault())
			{
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
				scope.Complete();
			}
			
			await dbCtx.SaveRemoteChangesAsync();
			return Ok();
		}

		[HttpPost("unit/delete")]
		[CookieAuthorize]
		public async Task<IActionResult> DeleteUnit(string compname, string unitname)
		{
			var client = (Account)HttpContext.Items["authEntity"]!;
			
			using (var scope = Transactions.DbAsyncScopeDefault())
			{
				var parse = await Utility.ParseViewpath(this, compname, unitname, client, false);
				if (parse?.Active is null)
					return BadRequest();
				
				await dbCtx.Entry(parse.Active).Collection(u => u.Employees).LoadAsync();
				dbCtx.Units.Remove(parse.Active);
				
				await dbCtx.SaveChangesAsync();
				scope.Complete();
			}
			
			await dbCtx.SaveRemoteChangesAsync();
			return Ok();
		}

		[HttpPost("unit/rename")]
		[CookieAuthorize]
		public async Task<IActionResult> RenameUnit(string compname, string unitname, string newname)
		{
			var client = (Account)HttpContext.Items["authEntity"]!;
			
			using (var scope = Transactions.DbAsyncScopeDefault())
			{
				var parse = await Utility.ParseViewpath(this, compname, unitname, client, false);
				if (parse?.Active is null)
					return BadRequest();

				parse.Active.Name = newname;
				
				await dbCtx.SaveChangesAsync();
				scope.Complete();
			}
			
			await dbCtx.SaveRemoteChangesAsync();
			return Ok();
		}
		
		[HttpPost("comp/delete")]
		[CookieAuthorize]
		public async Task<IActionResult> DeleteCompany(string compname)
		{
			var client = (Account)HttpContext.Items["authEntity"]!;
			
			using (var scope = Transactions.DbAsyncScopeDefault())
			{
				var parse = await Utility.ParseViewpath(this, compname, null, client, false);
				if (parse?.Company is null)
					return BadRequest();
				
				await dbCtx.Entry(parse.Company).Reference(c => c.Account).LoadAsync();
				dbCtx.Accounts.Remove(parse.Company.Account);
				
				await dbCtx.SaveChangesAsync();
				scope.Complete();
			}
			
			await dbCtx.SaveRemoteChangesAsync();
			return Ok();
		}
		
		[HttpPost("comp/settimezone")]
		[CookieAuthorize]
		public async Task<IActionResult> SetCompTimeZone(string compname, double offset)
		{
			var client = (Account)HttpContext.Items["authEntity"]!;
			
			using (var scope = Transactions.DbAsyncScopeDefault())
			{
				var parse = await Utility.ParseViewpath(this, compname, null, client, false);
				if (parse?.Company is null)
					return BadRequest();
				
				var conf = JsonSerializer.Deserialize<Company.Settings>(parse.Company.JsonSettings);
				if (conf is null)
					conf = new(); // ignore error
				
				conf.GMTOffset = offset;
				parse.Company.JsonSettings = JsonSerializer.Serialize(conf, JsonSerializerOptions.Default);
				
				await dbCtx.SaveChangesAsync();
				scope.Complete();
			}
			
			await dbCtx.SaveRemoteChangesAsync();
			return Ok();
		}
		
		[HttpPost("comp/openalldoors")]
		[CookieAuthorize]
		public async Task<IActionResult> OpenAllDoors(string compname)
		{
			var client = (Account)HttpContext.Items["authEntity"]!;
			List<Device> devices;
			
			using (var scope = Transactions.DbAsyncScopeDefault())
			{
				var parse = await Utility.ParseViewpath(this, compname, null, client, false);
				if (parse?.Company is null)
					return BadRequest();
				
				devices = await dbCtx.Devices
					.Where(d => d.CompanyId == parse.Company.Id)
					.Where(d => d.Password != null)
					.ToListAsync();
				
				scope.Complete();
			}
			
			var remSvc = HttpContext.RequestServices.GetRequiredService<IRemoteService>();
			
			List<Task<DevResponseDTO?>> tasks = new();
			foreach (var dev in devices)
			{
				tasks.Add(remSvc.SendControl(dev.Address, dev.Password!, new() {
					type = 1
				}));
			}
			
			DevResponseDTO?[] stat = await Task.WhenAll(tasks);
			bool successAll = true;
			foreach (var st in stat)
				successAll &= !st?.success ?? false;
			
			if (!successAll)
				return BadRequest();
			
			await dbCtx.SaveRemoteChangesAsync();
			return Ok();
		}
		
		[HttpPost("workshifts/add")]
		[CookieAuthorize]
		public async Task<IActionResult> AddWorkshift(string compname, [FromBody] WorkshiftAddDTO? dto)
		{
			if (dto is null)
				return BadRequest();
			
			var client = (Account)HttpContext.Items["authEntity"]!;
			
			using (var scope = Transactions.DbAsyncScopeDefault())
			{
				var parse = await Utility.ParseViewpath(this, compname, null, client, false);
				if (parse?.Company is null)
					return BadRequest();
				
				var comp = parse.Company;
				
				await dbCtx.WorkShifts.AddAsync(new()
				{
					InnerId = comp.NextShiftId++,
					CompanyId = comp.Id,
					JobTitle = dto.JobTitle,
					
					DateBegin = dto.DateBegin,
					DateEnd = dto.DateEnd,
					
					SunArrive = dto.SunArrive,
					SunLeave = dto.SunLeave,
					
					MonArrive = dto.MonArrive,
					MonLeave = dto.MonLeave,
					
					TueArrive = dto.TueArrive,
					TueLeave = dto.TueLeave,
					
					WedArrive = dto.WedArrive,
					WedLeave = dto.WedLeave,
					
					ThuArrive = dto.ThuArrive,
					ThuLeave = dto.ThuLeave,
					
					FriArrive = dto.FriArrive,
					FriLeave = dto.FriLeave,
					
					SatArrive = dto.SatArrive,
					SatLeave = dto.SatLeave
				});
				
				await dbCtx.SaveChangesAsync();
				scope.Complete();
			}
			return Ok();
		}
		
		[HttpPost("workshifts/delete")]
		[CookieAuthorize]
		public async Task<IActionResult> DeleteWorkshifts(string compname, [FromBody] int[] innerIds)
		{
			var client = (Account)HttpContext.Items["authEntity"]!;
			
			using (var scope = Transactions.DbAsyncScopeDefault())
			{
				var pdata = await Utility.ParseViewpath(this, compname, null, client, false);
				if (pdata?.Company is null)
					return BadRequest();
				
				var comp = pdata.Company;
				
				var wshifts = await dbCtx.WorkShifts
					.Where(w => w.CompanyId == comp.Id)
					.ToListAsync();
				
				var innerIdsSet = innerIds.ToHashSet();
				
				foreach (WorkShift wsh in wshifts)
					if (innerIdsSet.Contains(wsh.InnerId))
						dbCtx.WorkShifts.Remove(wsh);
				
				await dbCtx.SaveChangesAsync();
				scope.Complete();
			}
			return Ok();
		}
	}
}