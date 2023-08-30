using System.IO;
using System.Diagnostics;
using System.Text.Json;
using System.Globalization;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

using TcServer;
using TcServer.Storage;
using TcServer.Storage.Core;
using TcServer.Services;
using TcServer.Attributes;
using TcServer.Core;
using TcServer.Utility;
using TcServer.Views.Schedule.Index;

using ClosedXML.Excel;

namespace TcServer.Controllers
{
	[Route("/schedule")]
	public class ScheduleController : Controller
	{
		protected readonly CoreContext dbCtx;
		
		public ScheduleController(CoreContext dbctx)
		{
			dbCtx = dbctx;
		}
		
		[NonAction]
		public async Task<Dictionary<Employee, Record?>> CollectRecords(object scope, string? date, DateTime current)
		{
			List<Employee> empls;
			int compId;
			
			if (scope is Unit unit)
			{
				compId = unit.CompanyId;
				empls = await
					dbCtx.Employees
					.Where(e => e.UnitId == unit.Id)
					.ToListAsync();				
			}
			else if (scope is Company comp)
			{
				compId = comp.Id;
				empls = await
					dbCtx.Employees
					.Where(e => e.CompanyId == comp.Id)
					.ToListAsync();
			}
			else throw new Exception();
			
			//================================
			
			Dictionary<Employee, Record?> records = new();
			
			if (date is null || date.Length == 0)
			{
				foreach (var empl in empls)
					records[empl] = null;
				
				return records;
			}
			
			// @todo dont display employees if they joined company after viewdate
			var shiftsRaw = dbCtx.WorkShifts
				.Where(s => s.CompanyId == compId);

			var shifts = new Dictionary<string, List<WorkShift>>();
			
			// prepare shared rules
			shifts[string.Empty] = await shiftsRaw
				.Where(s => s.JobTitle == string.Empty)
				.ToListAsync();
			
			foreach (var emp in empls)
			{
				List<WorkShift> ruleset;
				if (shifts.ContainsKey(emp.JobTitle))
					ruleset = shifts[emp.JobTitle];
				else
				{
					ruleset = await shiftsRaw.Where(s => s.JobTitle == emp.JobTitle).ToListAsync();
					shifts[emp.JobTitle] = ruleset;
				}
				
				var record = await dbCtx.AtdRecords
					.FirstOrDefaultAsync(r => r.EmployeeId == emp.Id && r.Date == date);
				
				records[emp] = new
				(
					record?.TimeArrive,
					record?.TimeLeave,
					date, current, ruleset,
					shifts[string.Empty]
				);
			}
			return records;
		}
		
		// The main site page by functionality
		[CookieAuthorizeOptional]
		public async Task<IActionResult> Index(string? compname, string? unitname, string? viewdate)
		{
			var client = HttpContext.Items["authEntity"] as Account;
			if (client is null)
				return RedirectToAction("Login", "Main");
			
			bool cookieRedirect = false;
			string?[] reqcook = new string?[2]
			{
				Request.Cookies["scheduleCompName"],
				Request.Cookies["scheduleUnitName"]
			};
			if (compname is null)
			{
				compname = reqcook[0];
				cookieRedirect = compname is not null;
			}
			else if (reqcook[0] is null)
				Response.Cookies.Append("scheduleCompName", compname);
			
			if (unitname is null)
			{
				unitname = reqcook[1];
				cookieRedirect |= unitname is not null;
			}
			else if (reqcook[1] is null)
				Response.Cookies.Append("scheduleUnitName", unitname);
			
			if (cookieRedirect)
			{
				return RedirectToAction("Index", "Schedule", new
				{
					compname = compname,
					unitname = unitname,
					viewdate = viewdate
				});
			}
			
			Views.Schedule.Index.ViewData viewdata;
			
			using (var scope = Transactions.DbAsyncScopeRC())
			{
				var pdata = await Utility.ParseViewpath(this, compname, unitname == "__all__" ? null : unitname, client);
				if (pdata is null)
					return BadRequest();
				
				var now = DateTime.Now.AddHours (
					pdata.Company is not null
					? JsonSerializer.Deserialize<Company.Settings>
						(pdata.Company.JsonSettings)?.GMTOffset ?? 0.0
					: 0.0
				);
				
				string selDate = viewdate ?? now.ToString("yyyy-MM-dd");
				int emplsPt = 0, emplsT = 0, emplsP = 0;
				
				if (pdata.Company is not null)
				{
					emplsT = await dbCtx.Companies
						.Where(c => c.Id == pdata.Company.Id)
						.Select(c => c.Employees.Count())
						.FirstOrDefaultAsync();
					emplsPt = await dbCtx.Companies
						.Where(c => c.Id == pdata.Company.Id)
						.Select(c => c.Employees.Count(e => e.Phone != null))
						.FirstOrDefaultAsync();
				}
				if (pdata.Active is not null)
				{
					emplsP = await dbCtx.Units
						.Where(u => u.Id == pdata.Active.Id)
						.Select(u => u.Employees.Count(e => e.Phone != null))
						.FirstOrDefaultAsync();
				}
					
				viewdata = new()
				{
					AccType = client.Type,
					Company = pdata.Company,
					Companies = pdata.Companies ?? new(),
					Units = pdata.Units ?? new(),
					Active = pdata.Active,
					SelectedDate = selDate,
					EmplsTotal = emplsT,
					EmplsWithPhones = emplsP,
					EmplsWithPhonesTotal = emplsPt
				};
				
				var recScope = viewdata.Active as object ?? viewdata.Company as object;
				if (recScope is not null)
					viewdata.Records = await CollectRecords(recScope, viewdate, now);
				
				scope.Complete();
			}
			return View(viewdata);
		}
		
		// if unitname is defined, then export only for this unit. otherwise, for all units
		// if dateend is defined, then create excel pages for all dates in this range and fill likewise
		[HttpGet("xlsxreport")]
		[CookieAuthorize]
		public async Task<IActionResult> GetExcelReport
		(
			string compname, string viewdate,
			string? unitname = null, string? dateend = null
		) {
			var client = (Account)HttpContext.Items["authEntity"]!;
			List<string> datesRange;
			
			//================================
			
			if (dateend is not null)
			{
				datesRange = new();
				
				if (!DateTime.TryParseExact(viewdate, "yyyy-MM-dd", null, DateTimeStyles.None, out var dt))
					return BadRequest();
				if (!DateTime.TryParseExact(dateend, "yyyy-MM-dd", null, DateTimeStyles.None, out var dateEnd))
					return BadRequest();
				
				for (; dt <= dateEnd; dt = dt.AddDays(1))
				{
					datesRange.Add(dt.ToString("yyyy-MM-dd"));
				}
			}
			else datesRange = new() { viewdate };
			
			//================================
			
			var pdata = await Utility.ParseViewpath(this, compname, unitname, client, false);
			if (pdata is null || pdata.Company is null)
				return BadRequest();
			
			var now = DateTime.Now.AddHours (
				JsonSerializer.Deserialize<Company.Settings>
					(pdata.Company.JsonSettings)?.GMTOffset ?? 0.0
			);
			
			Dictionary<string/* date */,
				Dictionary<string /* unit name */,
					Dictionary<Employee, Record?>
				>
			> worksheets = new();
			
			foreach (string date in datesRange)
			{
				if (pdata.Active is not null)
				{
					worksheets[date] = new();
					worksheets[date][pdata.Active.Name] = await CollectRecords(pdata.Active, date, now);
					continue;
				}
				
				Dictionary<string /* unit name */, Dictionary<Employee, Record?>> pagedata = new();
				Dictionary<Employee, Record?> allUnitsRec = await CollectRecords(pdata.Company, date, now);
				
				using (var scope = Transactions.DbAsyncScopeRC())
				{
					foreach (var pair in allUnitsRec)
					{
						string name = await dbCtx.Units
							.Where(u => u.Id == pair.Key.UnitId)
							.Select(u => u.Name)
							.FirstAsync();
							
						if (!pagedata.ContainsKey(name))
							pagedata[name] = new();
						pagedata[name][pair.Key] = pair.Value;
					}
					scope.Complete();
				}
				worksheets[date] = pagedata;
			}
			
			//================================
			
			using (var workbook = new XLWorkbook())
			{
				foreach (var pair in worksheets)
				{
					var worksheet = workbook.AddWorksheet(pair.Key);
					worksheet.Column(1).Width = 35;
					worksheet.Column(2).Width = 15;
					worksheet.Column(3).Width = 15;
					worksheet.Column(4).Width = 40;
					worksheet.Column(5).Width = 15;
					worksheet.Column(6).Width = 15;
					worksheet.Column(7).Width = 30;
					worksheet.Column(8).Width = 25;
					
					int row = 1;
					
					var tophdr = worksheet.Range(row, 1, row, 8);
					tophdr.Merge();
					tophdr.Style.Font.Bold = true;
					tophdr.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
					
					string tophdrVal = "Отчёт за " + pair.Key;
					if (pair.Value.Count > 1)
						tophdrVal += ", все отделы";
					
					tophdr.Value = tophdrVal;
					
					// sort by unit name
					var unitdataSorted = pair.Value.OrderBy(p => p.Key);
					
					foreach (var unitdata in unitdataSorted)
					{
						++row;
						
						int rowbeg = ++row;
						var hdr = worksheet.Range(row, 1, row, 8);
						hdr.Merge();
						hdr.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
						hdr.Style.Fill.BackgroundColor = XLColor.Mint;
						hdr.Value = unitdata.Key;
						
						++row;
						var hdrcols = worksheet.Range(row, 1, row, 8);
						hdrcols.Style.Font.Bold = true;
						hdrcols.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
						
						worksheet.Cell(row, 1).Value = "Должность";
						worksheet.Cell(row, 2).Value = "ID";
						worksheet.Cell(row, 3).Value = "ID карты";
						worksheet.Cell(row, 4).Value = "Имя сотрудника";
						worksheet.Cell(row, 5).Value = "Время прихода";
						worksheet.Cell(row, 6).Value = "Время ухода";
						worksheet.Cell(row, 7).Value = "Комментарий";
						worksheet.Cell(row, 8).Value = "Телефон";
						
						var empldataSorted = unitdata.Value.OrderBy(p => p.Key.Name);
						foreach (var recdata in empldataSorted)
						{
							++row;
							worksheet.Cell(row, 1).Value = recdata.Key.JobTitle;
							worksheet.Cell(row, 2).Value = recdata.Key.InnerCompId.ToString();
							worksheet.Cell(row, 3).Value = recdata.Key.IdCard ?? string.Empty;
							worksheet.Cell(row, 4).Value = recdata.Key.Name;
							worksheet.Cell(row, 8).Value = recdata.Key.Phone ?? string.Empty;
							if (recdata.Value is not null)
							{
								worksheet.Cell(row, 5).Value = DayTime.ToString(recdata.Value.TimeArrive);
								worksheet.Cell(row, 6).Value = DayTime.ToString(recdata.Value.TimeLeave);
								
								var comments = new List<string>();
								
								if (recdata.Value.ArriveState == Record.State.Skip)
									comments.Add("Пропуск");
								else if (recdata.Value.ArriveState == Record.State.Late)
									comments.Add("Опоздание");

								if (recdata.Value.LeaveState == Record.State.Early)
									comments.Add("Ранний уход");
								
								worksheet.Cell(row, 7).Value = string.Join(", ", comments);
							}
						}
						
						var timecols = worksheet.Range(rowbeg, 5, row, 6);
						timecols.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
						
						var result = worksheet.Range(rowbeg, 1, row, 8);
						result.Style.Border.TopBorder = XLBorderStyleValues.Thin;
						result.Style.Border.RightBorder = XLBorderStyleValues.Thin;
						result.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
						result.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
					}
				}
				
				using (var output = new MemoryStream())
				{
					workbook.SaveAs(output);
					return File
					(
						output.ToArray(),
						"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
						compname + (unitname is null ? "" : $"-{unitname}") + $"-{viewdate}.xlsx"
					);
				}
			}
		}
	}
}
