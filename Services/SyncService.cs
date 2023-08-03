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
	protected readonly SyncContext dbCtx;
	
	public SyncService(IServiceProvider sp, SyncContext ctx)
	{
		updSvc = sp.GetRequiredService<IUpdateService>();
		remSvc = sp.GetRequiredService<IRemoteService>();
		cfgSvc = sp.GetRequiredService<IConfigService>();
		dbCtx = ctx;
	}
	
	protected override async Task ExecuteAsync(CancellationToken token)
	{
		var delayTime = Convert.ToDouble(cfgSvc.Access()["SyncService:DelaySeconds"]!);
		
		while (!token.IsCancellationRequested)
		{
			await updSvc.UpdateDevicesStat();
			
			Dictionary<int, List<Device>> mapping = new();
			
			// map CompanyId to all devices of this company
			using (var scope = Transactions.DbAsyncScopeRC())
			{
				foreach (var dev in dbCtx.Devices.Where(d => d.CompanyId != null))
				{
					int k = dev.CompanyId!.Value;
					if (!mapping.ContainsKey(k))
						mapping[k] = new() { dev };
					else mapping[k].Add(dev);
				}
				scope.Complete();
			}
			
			// perform employees' sync for each company
			foreach (var el in mapping)
			{
				List<Employee> innerEmpls;
				using (var scope = Transactions.DbAsyncScopeRC())
				{
					innerEmpls = await dbCtx.Employees
						.Where(e => e.CompanyId == el.Key)
						.ToListAsync();
					scope.Complete();
				}
				
				// to store IDs of employees to be marked as RemoteSynchronized
				HashSet<int> synced = new();
				
				// ensure innnerEmpls is equal to list of employees stored in device
				foreach (var dev in el.Value)
				{
					if (dev.Password is null || !updSvc.DevStat[dev.SerialNumber])
						continue;
					
					var remoteEmpls = (
						await remSvc.QueryEmployees(dev.Address, dev.Password, "-1")
					)?.data.ToDictionary(e => e.id);
					
					if (remoteEmpls is null)
						continue;
					
					var tasks = new List<Task<PersonResponseDTO?>>();
					
					foreach (var empl in innerEmpls)
					{
						string eid = empl.InnerCompId.ToString();
						
						if (remoteEmpls.ContainsKey(eid))
						{
							if (
								remoteEmpls[eid].name != empl.Name ||
								remoteEmpls[eid].phone != (empl.Phone ?? string.Empty)
							) {
								tasks.Add(remSvc.UpdateEmployee(dev.Address, dev.Password, new()
								{
									id = eid,
									name = empl.Name,
									phone = empl.Phone ?? string.Empty
								}));
							}
							if (!empl.RemoteSynchronized)
								synced.Add(empl.Id);
							
							remoteEmpls.Remove(eid);
						}
						else
						{
							tasks.Add(remSvc.AddEmployee(dev.Address, dev.Password, new()
							{
								id = eid,
								name = empl.Name,
								phone = empl.Phone
							}));
						}
					}
					await Task.WhenAll(tasks);
					
					List<string> delIds = remoteEmpls.Select(p => p.Key).ToList();
					if (delIds.Count > 0)
						await remSvc.RemoveEmployees(dev.Address, dev.Password, delIds);
				}
				
				if (synced.Count != 0)
				{
					using (var scope = Transactions.DbAsyncScopeDefault())
					{
						var notsynced = await dbCtx.Employees
							.Where(e => !e.RemoteSynchronized)
							.ToListAsync();
						
						foreach (var empl in notsynced)
						{
							if (synced.Contains(empl.Id))
								empl.RemoteSynchronized = true;
						}
						await dbCtx.SaveChangesAsync();
						scope.Complete();
					}
				}
			}
			await Task.Delay(TimeSpan.FromSeconds(delayTime));
		}
	}
}
