using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Transactions;
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

// Shared code for controllers

namespace TcServer.Controllers
{
	public static class Utility
	{
		public class ViewpathParseData
		{
			public List<string>? Companies { get; set; } = null;
			
			public List<string>? Units { get; set; } = null;
			
			public Company? Company { get; set; } = null;
			
			public Unit? Active { get; set; } = null;
		}
		
		public static async Task<bool> IsAuthorizedData(Controller self, Account acc, int compId)
		{
			if (acc.Type == AccountType.Admin)
				return true;
			
			if (acc.Type == AccountType.Company)
			{
				var dbCtx = self.Request.HttpContext.RequestServices
					.GetRequiredService<CoreContext>();
				
				await dbCtx.Entry(acc).Reference(a => a.Company).LoadAsync();
				if (acc.Company!.Id != compId)
					return false;
				
				return true;
			}
			return false;
		}
		
		public static async Task<bool> IsAuthorizedData(Controller self, Account acc, Employee empl)
		{
			return await IsAuthorizedData(self, acc, empl.CompanyId);
		}
		public static async Task<bool> IsAuthorizedData(Controller self, Account acc, Unit unit)
		{
			return await IsAuthorizedData(self, acc, unit.CompanyId);
		}
		public static async Task<bool> IsAuthorizedData(Controller self, Account acc, Device dev)
		{
			if (dev.CompanyId is null)
				return acc.Type == AccountType.Admin;
			
			return await IsAuthorizedData(self, acc, dev.CompanyId.Value);
		}
		
		public static async Task<ViewpathParseData?> ParseViewpath
		(
			Controller self,
			string? compname,
			string? unitname,
			Account acc,
			bool loadUnitList = true
		) {
			var dbCtx = self.Request.HttpContext.RequestServices
				.GetRequiredService<CoreContext>();

			var data = new ViewpathParseData();

			if (acc.Type == AccountType.Company)
			{
				await dbCtx.Entry(acc).Reference(u => u.Company).LoadAsync();
				if (acc.Company is null)
					return null;
				if (compname is not null && acc.Company.Name != compname)
					return null;
				
				data.Company = acc.Company;
			}
			else if (acc.Type == AccountType.Admin)
			{
				data.Companies = await dbCtx.Companies
					.Select(c => c.Name)
					.ToListAsync();
				
				if (compname is not null)
				{
					data.Company = await dbCtx.Companies.FirstOrDefaultAsync(c => c.Name == compname);
					if (data.Company is null)
						return null;
				}
				else return data;
			}
			else return null;
			
			if (loadUnitList)
			{
				data.Units = await dbCtx.Units
					.Where(u => u.CompanyId == data.Company.Id)
					.Select(u => u.Name)
					.ToListAsync();
			}
			if (unitname is not null)
			{
				data.Active = await dbCtx.Units
					.Where(u => u.CompanyId == data.Company.Id)
					.FirstOrDefaultAsync(u => u.Name == unitname);
			}
			return data;
		}
	}
}