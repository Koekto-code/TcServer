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
				// Considering there're 2 scopes of changes, called 'inner' and 'remote':
				//   First ones are made on this site
				//   Second ones are made manually while interacting with devices, respectively
				// Only one of them may have value at the moment
				// The decision whether accept 1st or 2nd scope changes
				//   is made for each employee, depending on the state of local data
				
				List<Employee> innerEmpls;
				using (var scope = Transactions.DbAsyncScopeRC())
				{
					innerEmpls = await dbCtx.Employees
						.Where(e => e.CompanyId == el.Key)
						.ToListAsync();
					scope.Complete();
				}
				
				Dictionary<int, Employee> innerChangedEmpls = new();
				
				// ensure innerEmpls is equal to list of employees stored in device
				foreach (var dev in el.Value)
				{
					if (dev.Password is null || !updSvc.DevStat[dev.SerialNumber])
						continue;
					
					var remoteEmpls = (
						await remSvc.QueryEmployees(dev.Address, dev.Password, "-1")
					)?.data.ToDictionary(e => e.id);
					
					if (remoteEmpls is null)
						continue;
					
					List<Task<PersonResponseDTO?>> tasks = new();
					List<int> tasksEmplNums = new(); // to know the employee related to each remote task
					
					for (int i = 0; i != innerEmpls.Count; ++i)
					{
						var empl = innerEmpls[i];
						string eid = empl.InnerCompId.ToString();
						
						if (remoteEmpls.ContainsKey(eid))
						{
							var re = remoteEmpls[eid];
							if (
								re.name != empl.Name ||
								re.idcardNum != (empl.IdCard ?? string.Empty)
							) {
								// if not marked as synchronized, prioritize inner changes
								// (means failed to apply inner changes to remote before)
								// otherwise let the remote changes apply
								if (empl.RemoteSynchronized)
								{
									empl.Name = re.name;
									empl.IdCard = re.idcardNum;
								}
								else
								{
									tasks.Add(remSvc.UpdateEmployee(dev.Address, dev.Password, new()
									{
										id = eid,
										name = empl.Name,
										idcardNum = empl.IdCard ?? string.Empty
									}));
									tasksEmplNums.Add(i);
									empl.RemoteSynchronized = true;
								}
								innerChangedEmpls[empl.Id] = empl;
							}
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
							tasksEmplNums.Add(i);
						}
					}
					var updTasks = await Task.WhenAll(tasks);
					
					for (int i = 0; i != tasks.Count; ++i)
					{
						PersonResponseDTO? ut = updTasks[i];
						if (ut is null /* || !ut.success */)
						{
							var ie = innerEmpls[tasksEmplNums[i]];
							if (ie.RemoteSynchronized)
							{
								ie.RemoteSynchronized = false;
								innerChangedEmpls[ie.Id] = ie;
							}
						}
					}
					
					List<string> delIds = remoteEmpls.Select(p => p.Key).ToList();
					if (delIds.Count > 0)
						await remSvc.RemoveEmployees(dev.Address, dev.Password, delIds);
				}
				
				if (innerChangedEmpls.Count != 0)
				{
					using (var scope = Transactions.DbAsyncScopeDefault())
					{
						innerEmpls = await dbCtx.Employees
							.Where(e => e.CompanyId == el.Key)
							.ToListAsync();
						
						foreach (var empl in innerEmpls)
						{
							if (innerChangedEmpls.ContainsKey(empl.Id))
							{
								var ch = innerChangedEmpls[empl.Id];
								empl.RemoteSynchronized = ch.RemoteSynchronized;
								empl.Name = ch.Name;
								empl.HomeAddress = ch.HomeAddress;
								empl.Notify = ch.Notify;
								empl.IdCard = ch.IdCard;
								empl.Phone = ch.Phone;
							}
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
