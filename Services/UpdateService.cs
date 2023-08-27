using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Diagnostics;
using System.Text.Json;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using TcServer;
using TcServer.Storage;
using TcServer.Storage.Core;
using TcServer.Services;
using TcServer.Attributes;
using TcServer.Core.Remote;
using TcServer.Utility;
using Microsoft.EntityFrameworkCore;

namespace TcServer.Services
{
	public interface IUpdateService
	{
		IReadOnlyDictionary<string, bool> DevStat { get; }
		
		Task UpdateDevicesStat(int? compid = null);
	}
	
	public class UpdateService: IUpdateService
	{
		protected readonly HttpClient devClient;
		protected readonly CoreContext dbCtx;
		
		// stores info about whether each device is online or not
		protected Dictionary<string, bool> devStat { get; set; } = new();
		
		public UpdateService(IHttpClientFactory clientFactory, IServiceProvider sp)
		{
			devClient = clientFactory.CreateClient("Remote");
			dbCtx = sp.GetRequiredService<CoreContext>();
		}
		
		public IReadOnlyDictionary<string, bool> DevStat
		{
			get { return new ReadOnlyDictionary<string, bool>(devStat); }
		}
		
		public async Task UpdateDevicesStat(int? compid)
		{
			List<Device> devices;
			using (var scope = Transactions.DbAsyncScopeRC())
			{
				if (compid is null)
					devices = await dbCtx.Devices.ToListAsync();
				else devices = await dbCtx.Devices
					.Where(d => d.CompanyId == compid)
					.ToListAsync();
				
				scope.Complete();
			}
			
			var tasks = new Task<DevExtResponseDTO?>[devices.Count];
			for (int i = 0; i != devices.Count; ++i)
				tasks[i] = Methods.GetDeviceKey(devClient, devices[i].Address);
			
			var tdone = await Task.WhenAll(tasks);
			for (int i = 0; i != tdone.Length; ++i)
				devStat[devices[i].SerialNumber] = tdone[i] is not null;
		}
	}
}