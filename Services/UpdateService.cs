using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Diagnostics;
using System.Text.Json;

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
		Task UpdateDevicesStat();
	}
	
	public class UpdateService: IUpdateService
	{
		protected readonly HttpClient devClient;
		protected readonly CoreContext dbCtx;
		
		public UpdateService(IHttpClientFactory clientFactory, CoreContext dbctx)
		{
			devClient = clientFactory.CreateClient("Remote");
			dbCtx = dbctx;
		}
		
		public async Task UpdateDevicesStat()
		{
			var devices = await dbCtx.Devices.ToListAsync();
			
			var tasks = new Task<DevResponseDTO?>[devices.Count];
			for (int i = 0; i != devices.Count; ++i)
			{
				tasks[i] = Methods.GetDeviceKey(devClient, devices[i].Address);
			}
			await Task.WhenAll(tasks);
			for (int i = 0; i != devices.Count; ++i)
			{
				dbCtx.DevicesStat[devices[i].SerialNumber] = tasks[i] is not null;
			}
		}
	}
}