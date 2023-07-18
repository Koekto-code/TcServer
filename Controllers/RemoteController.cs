using System.Text;
using System.Text.Json;
using System.Transactions;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

using TcServer.Storage;
using TcServer.Storage.Core;
using TcServer.Services;
using TcServer.Attributes;
using TcServer.Core;
using TcServer.Core.Remote;
using TcServer.Utility;

namespace TcServer.Controllers
{
	[ApiController]
	[Route("remote/")]
	public class RemoteController : Controller
	{
		protected readonly IConfigService cfgSvc;
		protected readonly CoreContext dbCtx;
		
		public RemoteController(CoreContext dbctx, IConfigService cfgsvc)
		{
			dbCtx = dbctx;
			cfgSvc = cfgsvc;
		}
		
		[HttpPost("callback")]
		public async Task<IActionResult> RecognitionCallback()
		{
			RecognitionCallbackDTO? rec;
			using (StreamReader reader = new(Request.Body))
			{
				rec = JsonSerializer.Deserialize<RecognitionCallbackDTO>(await reader.ReadToEndAsync());
				if (rec is null)
					return BadRequest();
			}
		
			int retryCount = Convert.ToInt32(cfgSvc.Access()["Misc:SqlQueryRetryCount"]!);
			double retryDelay = Convert.ToDouble(cfgSvc.Access()["Misc:SqlQueryRetryDelay"]!);
			int recInterval = Convert.ToInt32(cfgSvc.Access()["Misc:RecordInterval"]!);
			int photoLimit = Convert.ToInt32(cfgSvc.Access()["Misc:EmployeePhotoLimit"]!);
			
			using (var scope = new TransactionScope
			(
				TransactionScopeOption.Required,
				new TransactionOptions {
					IsolationLevel = IsolationLevel.ReadCommitted
				},
				TransactionScopeAsyncFlowOption.Enabled
			))
			{
				Device? device = await dbCtx.Devices.FirstOrDefaultAsync(d => d.SerialNumber == rec.deviceKey);
				if (device is null)
					return BadRequest();
				
				await dbCtx.Entry(device).Reference(d => d.Company).LoadAsync();
				if (device.Company is null)
					return BadRequest();
				
				var comp = device.Company;
				
				/*===-- calculate time and date --===*/
				
				if (!long.TryParse(rec.time, out long rectime))
					return BadRequest();
				
				var unixOff = DateTimeOffset.FromUnixTimeMilliseconds(rectime);
				var timepoint = unixOff.DateTime.AddHours(comp.GMTOffset);
				
				string date = timepoint.ToString("yyyy-MM-dd");
				Console.WriteLine($"INFO: Record date: {date}");
				
				/*===-- check if recognized --===*/
				
				Employee? empl = null;
				int emplInnerId = 0;
				
				if (
					rec.personId.Length > 0 &&
					rec.personId != "STRANGERBABY"
				) {
					emplInnerId = Convert.ToInt32(rec.personId);
					if (emplInnerId != 0)
						empl = await dbCtx.Employees
							.Where(e => e.CompanyId == comp.Id)
							.FirstOrDefaultAsync(e => e.InnerCompId == emplInnerId);
				}
				
				/*===-- photo part ---===*/
				
				Photo? photo = null;
				if (rec.base64 is null || rec.base64 == string.Empty)
				{
					Console.WriteLine($"ERROR: No base64 content in photo");
				}
				else
				{
					photo = new()
					{
						Base64 = rec.base64,
						Url = rec.path,
						DeviceId = device.Id
					};
					if (
						photo.Url is not null &&
						photo.Url.Length > 3 &&
						photo.Url[^4..] == ".png"
					) {
						photo.Format = Photo.ImageFormat.PNG;
					}
					else photo.Format = Photo.ImageFormat.JPG;
				}
				
				if (photo is not null)
				{
					List<int> ph = null!;
					if (empl is not null)
					{
						Console.WriteLine($"INFO: Photo belongs to {empl.Id}");
						photo.EmployeeId = empl.Id;
						
						ph = await dbCtx.Photos
							.Where(p => p.EmployeeId == empl.Id)
							.Select(p => p.Id)
							.ToListAsync();
					}
					else
					{
						Console.WriteLine($"INFO: Photo owner not found ({rec.personId}), saving as-is");
						ph = await dbCtx.Photos
							.Where(p => p.EmployeeId == null)
							.Select(p => p.Id)
							.ToListAsync();
					}
					var rand = new Random();
					while (ph.Count >= photoLimit)
					{
						int rem = rand.Next() % ph.Count;
						var entity = await dbCtx.Photos.FindAsync(ph[rem]);
						dbCtx.Photos.Remove(entity!);
						ph.RemoveAt(rem);
					}
					await dbCtx.Photos.AddAsync(photo);
				}
				
				if (empl is null)
				{
					Console.WriteLine($"WARN: Unrecognized person {rec.personId}");
					
					// Mark request as accepted so that device can delete it from its local DB
					return Ok("{\"result\": 1, \"success\": true}");
				}
				
				/*===-- write the record finally --===*/
				
				var record = await dbCtx.AtdRecords
					.Where(r => r.EmployeeId == empl.Id)
					.FirstOrDefaultAsync(r => r.Date == date);

				int mins = timepoint.Hour * 60 + timepoint.Minute;
				Console.WriteLine($"INFO: Callback time: {mins} minutes");

				if (record is null)
				{
					record = new()
					{
						EmployeeId = empl.Id,
						Date = date
					};
					record.TimeArrive = mins;
					await dbCtx.AtdRecords.AddAsync(record);
				}
				else
				{
					// sorting
					if (record.TimeArrive is null)
						record.TimeArrive = mins;
					else if (record.TimeArrive > mins)
						record.TimeArrive = mins;
					else if (record.TimeLeave is null)
						record.TimeLeave = mins;
					else if (record.TimeLeave < mins)
						record.TimeLeave = mins;
					
					if (record.TimeLeave - record.TimeArrive < recInterval)
						record.TimeLeave = null;
				}
				
				await dbCtx.SaveChangesAsync();
				scope.Complete();
				return Ok("{\"result\": 1, \"success\": true}");;
			}
		}
	}
}
