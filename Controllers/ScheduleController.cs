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
		public async Task<Dictionary<Employee, Record?>> CollectRecords(object scope, string? date)
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
			
			foreach (var emp in empls)
			{
				List<WorkShift> ruleset;
				if (shifts.ContainsKey(emp.JobTitle))
					ruleset = shifts[emp.JobTitle];
				else
				{
					// absence of WorkShift rules for given job title is similar to one with default constructor
					ruleset = await shiftsRaw.Where(s => s.JobTitle == emp.JobTitle).ToListAsync();
					shifts[emp.JobTitle] = ruleset;
				}
				
				var record = await dbCtx.AtdRecords
					.FirstOrDefaultAsync(r => r.EmployeeId == emp.Id && r.Date == date);
				
				records[emp] = new
				(
					record?.TimeArrive,
					record?.TimeLeave,
					date, ruleset
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
			
			var pdata = await Utility.ParseViewpath(this, compname, unitname, client);
			if (pdata is null)
				return BadRequest();
			
			Views.Schedule.Index.ViewData viewdata = new()
			{
				AccType = client.Type,
				Company = pdata.Company,
				Companies = pdata.Companies ?? new(),
				Units = pdata.Units ?? new(),
				Active = pdata.Active,
				SelectedDate = viewdate ?? DateTime.Today.ToString("yyyy-MM-dd")
			};
			
			if (viewdata.Active is null)
				return View(viewdata);
			
			viewdata.Records = await CollectRecords(viewdata.Active, viewdate);
			
			// for assigning devices to new employees
			viewdata.AvailableDevices = await dbCtx.Devices
				.Where(d => d.CompanyId == pdata.Company!.Id)
				.ToListAsync();
			
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
			
			Dictionary<string/* date */,
				Dictionary<int /* unit id */,
					Dictionary<Employee, Record?>
				>
			> worksheets = new();
			
			foreach (string date in datesRange)
			{
				if (pdata.Active is not null)
				{
					worksheets[date] = new();
					worksheets[date][pdata.Active.Id] = await CollectRecords(pdata.Active, date);
					continue;
				}
				
				Dictionary<int /* unit id */, Dictionary<Employee, Record?>> pagedata = new();
				Dictionary<Employee, Record?> allUnitsRec = await CollectRecords(pdata.Company, date);
				
				foreach (var pair in allUnitsRec)
				{
					int k = pair.Key.UnitId!.Value;
					if (!pagedata.ContainsKey(k))
						pagedata[k] = new();
					pagedata[k][pair.Key] = pair.Value;
				}
				worksheets[date] = pagedata;
			}
			
			//================================
			
			using (var workbook = new XLWorkbook())
			{
				foreach (var pair in worksheets)
				{
					var worksheet = workbook.AddWorksheet(pair.Key);
					worksheet.Column(1).Width = 20;
					worksheet.Column(2).Width = 40;
					worksheet.Column(3).Width = 15;
					worksheet.Column(4).Width = 15;
					
					int row = 1;
					
					var tophdr = worksheet.Range(row, 1, row, 4);
					tophdr.Merge();
					tophdr.Style.Font.Bold = true;
					tophdr.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
					
					string tophdrVal = "Отчёт за " + pair.Key;
					if (pair.Value.Count > 1)
						tophdrVal += ", все отделы";
					
					tophdr.Value = tophdrVal;
					
					foreach (var unitdata in pair.Value)
					{
						++row;
						Unit? unit = await dbCtx.Units.FindAsync(unitdata.Key);
						
						int rowbeg = ++row;
						var hdr = worksheet.Range(row, 1, row, 5);
						hdr.Merge();
						hdr.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
						hdr.Style.Fill.BackgroundColor = XLColor.Mint;
						hdr.Value = unit!.Name;
						
						++row;
						var hdrcols = worksheet.Range(row, 1, row, 5);
						hdrcols.Style.Font.Bold = true;
						
						worksheet.Cell(row, 1).Value = "Должность";
						worksheet.Cell(row, 2).Value = "Имя сотрудника";
						worksheet.Cell(row, 3).Value = "Время прихода";
						worksheet.Cell(row, 4).Value = "Время ухода";
						worksheet.Cell(row, 5).Value = "Комментарий";
						
						foreach (var recdata in unitdata.Value)
						{
							Console.WriteLine($"Recdata: {recdata.Key.Name}");
							++row;
							worksheet.Cell(row, 1).Value = recdata.Key.JobTitle;
							worksheet.Cell(row, 2).Value = recdata.Key.Name;
							if (recdata.Value is not null)
							{
								worksheet.Cell(row, 3).Value = DayTime.ToString(recdata.Value.TimeArrive);
								worksheet.Cell(row, 4).Value = DayTime.ToString(recdata.Value.TimeLeave);
							}
						}
						
						var result = worksheet.Range(rowbeg, 1, row, 4);
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
