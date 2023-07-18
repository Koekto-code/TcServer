using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Diagnostics;
using System.Text.Json;

using TcServer;
using TcServer.Utility;
using TcServer.Storage;
using TcServer.Storage.Core;
using TcServer.Services;
using TcServer.Attributes;
using TcServer.Core.Remote;
using Microsoft.EntityFrameworkCore;

namespace TcServer.Services;

public class SyncService: BackgroundService
{
	protected readonly IUpdateService updSvc;
	protected readonly IRemoteService remSvc;
	protected readonly IConfigService cfgSvc;
	protected readonly CoreContext dbCtx;
	
	public SyncService
	(
		IUpdateService updsvc,
		IRemoteService remsvc,
		IConfigService cfgsvc,
		CoreContext dbctx
	) {
		updSvc = updsvc;
		remSvc = remsvc;
		cfgSvc = cfgsvc;
		dbCtx = dbctx;
	}
	
	protected override async Task ExecuteAsync(CancellationToken token)
	{
		var delayTime = Convert.ToDouble(cfgSvc.Access()["SyncService:DelaySeconds"]!);
		
		while (!token.IsCancellationRequested)
		{
			await updSvc.UpdateDevicesStat();
			List<Device> devices = await dbCtx.Devices.ToListAsync();
			
			foreach (var dev in devices)
			{
				if (!dbCtx.DevicesStat[dev.SerialNumber] || dev.Password is null)
					continue;
				
				var remoteEmpls = (
					await remSvc.QueryEmployees(dev.Address, dev.Password, "-1")
				)?.data.ToDictionary(e => e.id);
				
				if (remoteEmpls is null)
					continue;
				
				List<Employee> innerEmpls = null!;
				
				innerEmpls = await dbCtx.Employees.Where(e => e.CompanyId == dev.CompanyId).ToListAsync();
				var tasks = new List<Task<PersonResponseDTO?>>();
				
				foreach (var empl in innerEmpls)
				{
					string eid = empl.InnerCompId.ToString();
					
					if (remoteEmpls.ContainsKey(eid))
					{
						if (remoteEmpls[eid].name != empl.Name)
						{
							tasks.Add(remSvc.UpdateEmployee(dev.Address, dev.Password, new()
							{
								id = eid,
								name = empl.Name
							}));
						}
						remoteEmpls.Remove(eid);
					}
					else
					{
						tasks.Add(remSvc.AddEmployee(dev.Address, dev.Password, new()
						{
							id = eid,
							name = empl.Name
						}));
					}
				}
				await Task.WhenAll(tasks);
				
				List<string> delIds = remoteEmpls.Select(p => p.Key).ToList();
				if (delIds.Count > 0)
					await remSvc.RemoveEmployees(dev.Address, dev.Password, delIds);
			}
			await Task.Delay(TimeSpan.FromSeconds(delayTime));
		}
	}
}
