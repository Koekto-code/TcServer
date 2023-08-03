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
using TcServer.Core.Wappi;
using TcServer.Utility;
using TcServer.Views.Schedule.Index;

namespace TcServer.Controllers
{
	[ApiController]
	[Route("remote/")]
	public class RemoteController : Controller
	{
		protected readonly CoreContext dbCtx;
		protected readonly IConfigService cfgSvc;
		protected readonly HttpClient waClient;

		
		public RemoteController(CoreContext dbctx, IConfigService cfgsvc, IHttpClientFactory cf)
		{
			dbCtx = dbctx;
			cfgSvc = cfgsvc;
			waClient = cf.CreateClient("WhatsAppNotify");
		}
		
		protected enum RecordProp
		{
			None,
			ArriveTime,
			LeaveTime
		};
		
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
			string hostname = cfgSvc.Access()["Host:Address"]!;
			
			Employee? empl = null;
			Photo? photo = null;
			Company? comp;
			string date;
			Record viewrec;
			DateTime timepoint;
			RecordProp changed = RecordProp.None;
			List<string?> recipients;
			
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
				
				comp = device.Company;
				var conf = JsonSerializer.Deserialize<Company.Settings>(comp.JsonSettings);
				if (conf is null)
				{
					Console.WriteLine("error: Couldn't deserialize Company.JsonSettings. Resetting to default\n");
					comp.JsonSettings = "{}";
					await dbCtx.SaveChangesAsync();
					conf = new Company.Settings();
				}	
				
				/*====   calculate time and date   ====*/
				
				if (!long.TryParse(rec.time, out long rectime))
					return BadRequest();
				
				var unixOff = DateTimeOffset.FromUnixTimeMilliseconds(rectime);
				timepoint = unixOff.DateTime.AddHours(conf.GMTOffset);
				
				date = timepoint.ToString("yyyy-MM-dd");
				
				/*====   check if recognized   ====*/
				
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
				
				/*====   photo part   ====*/
				
				if (rec.base64 is null || rec.base64 == string.Empty)
				{
					Console.WriteLine($"warning: No base64 content in photo. Please reset callbacks on {device.SerialNumber}");
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
					if (empl is not null)
						photo.EmployeeId = empl.Id;
					
					List<Photo> ph = await dbCtx.Photos
						.Where(p => p.EmployeeId == photo.EmployeeId)
						.ToListAsync();
					
					var rand = new Random();
					while (ph.Count >= photoLimit)
					{
						int rem = rand.Next() % ph.Count;
						dbCtx.Photos.Remove(ph[rem]);
						ph.RemoveAt(rem);
					}
					await dbCtx.Photos.AddAsync(photo);
					await dbCtx.SaveChangesAsync();
				}
				
				if (empl is null)
				{
					Console.WriteLine($"warning: Unrecognized person {rec.personId}");
					
					// Mark request as accepted so that device can delete it from its local DB
					return Ok("{\"result\": 1, \"success\": true}");
				}
				
				/*====   write the record finally   ====*/
				
				var record = await dbCtx.AtdRecords
					.Where(r => r.EmployeeId == empl.Id)
					.FirstOrDefaultAsync(r => r.Date == date);

				int mins = timepoint.Hour * 60 + timepoint.Minute;

				if (record is null)
				{
					record = new()
					{
						EmployeeId = empl.Id,
						Date = date
					};
					await dbCtx.AtdRecords.AddAsync(record);
				}

				if (record.TimeArrive is null)
				{
					changed = RecordProp.ArriveTime;
					record.TimeArrive = mins;
				}
				else if (record.TimeLeave is null)
				{
					if (mins - record.TimeArrive >= recInterval)
					{
						changed = RecordProp.LeaveTime;
						record.TimeLeave = mins;
					}
				}
				else if (mins - record.TimeLeave >= recInterval)
				{
					// rewrite the record
					changed = RecordProp.ArriveTime;
					record.TimeArrive = mins;
					record.TimeLeave = null;
				}

				await dbCtx.SaveChangesAsync();
				
				// =========================
				
				var rules = await dbCtx.WorkShifts
					.Where(w => w.CompanyId == empl.CompanyId)
					.Where(w => w.JobTitle == empl.JobTitle)
					.ToListAsync();
				
				var rulesPublic = await dbCtx.WorkShifts
					.Where(w => w.CompanyId == empl.CompanyId)
					.Where(w => w.JobTitle == string.Empty)
					.ToListAsync();
				
				recipients = await dbCtx.Employees
					.Where(e => e.CompanyId == empl.CompanyId)
					.Where(e => e.Phone != null)
					.Where(e => e.Notify == Employee.NotifyMode.EnableWhatsApp)
					.Select(e => e.Phone)
					.ToListAsync();
				
				viewrec = new(record.TimeArrive, record.TimeLeave, date, rules, rulesPublic);
				scope.Complete();
			}
			
			/*====   send notification via WhatsApp   ====*/
			
			if (changed != RecordProp.None)
			{
				string wappiSession = cfgSvc.Access()["WAPPI:Profile"]!;
				string time = timepoint.ToString("HH:mm");
				string state = changed == RecordProp.ArriveTime ? "Вход" : "Выход";
				string datefmt = timepoint.ToString("dd.MM.yyyy");
				string comm = string.Empty;
				
				string msg = $"*{comp.Name}*\n{empl.Name}\n{state} в {time}, {datefmt}";
				
				foreach (string? phone in recipients)
				{
					dynamic? resp;
					if (photo?.Base64 is not null)
					{
						resp = await ChatMethods.SendImage
						(
							waClient,
							wappiSession,
							new()
							{
								recipient = phone!,
								caption = msg,
								b64_file = photo.Base64
							}
						);
					}
					else
					{
						resp = await ChatMethods.SendMessage
						(
							waClient,
							wappiSession,
							new()
							{
								recipient = phone!,
								body = msg
							}
						);
					}
				}
			}
			return Ok("{\"result\": 1, \"success\": true}");;
		}
	}
}
