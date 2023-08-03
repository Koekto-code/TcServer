using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Diagnostics;
using System.Text.Json;

using TcServer;
using TcServer.Storage.Core;
using TcServer.Services;
using TcServer.Attributes;
using TcServer.Core;
using TcServer.Core.Remote;
using TcServer.Utility;

using Microsoft.EntityFrameworkCore;

// Just a wrapper for Remote/Methods.cs

namespace TcServer.Services
{
	public interface IRemoteService
	{
		Task<PersonResponseDTO?> AddEmployee(string uri, string pass, PersonSubmitDTO dto);
		
		Task<PersonResponseDTO?> UpdateEmployee(string uri, string pass, PersonSubmitDTO dto);
		
		Task<DevResponseDTO?> RemoveEmployees(string uri, string pass, List<string> ids);
		
		Task<PersonListResponseDTO?> QueryEmployees(string uri, string pass, string id);
		
		Task<PhotoAssignResponseDTO?> AssignPhoto(string uri, string pass, PhotoAssignDTO dto, bool initial = true);
		
		Task<DevResponseDTO?> DeletePhoto(string uri, string pass, string faceId);
		
		Task<RecordsResponseDTO?> QueryRecords(string uri, string pass, RecordsQueryDTO dto);
		
		Task<DevResponseDTO?> DeleteRecords(string uri, string pass, RecordsDeleteDTO dto);
		
		Task<DevResponseDTO?> SetIdentifyCallback(string uri, string pass, SetIdentifyCallbackDTO dto);
	}
	
	public class RemoteService: IRemoteService
	{
		protected HttpClient devClient = null!;
		
		public RemoteService(IHttpClientFactory clientFactory)
		{
			devClient = clientFactory.CreateClient("Remote");
		}
		
		public async Task<PersonResponseDTO?> AddEmployee(string uri, string pass, PersonSubmitDTO dto)
		{
			return await Methods.AddEmployee(devClient, uri, pass, dto);
		}
		
		public async Task<PersonResponseDTO?> UpdateEmployee(string uri, string pass, PersonSubmitDTO dto)
		{
			return await Methods.UpdateEmployee(devClient, uri, pass, dto);
		}
		
		public async Task<DevResponseDTO?> RemoveEmployees(string uri, string pass, List<string> ids)
		{
			return await Methods.RemoveEmployees(devClient, uri, pass, ids);
		}
		
		public async Task<PersonListResponseDTO?> QueryEmployees(string uri, string pass, string id)
		{
			return await Methods.QueryEmployees(devClient, uri, pass, id);
		}
		
		public async Task<PhotoAssignResponseDTO?> AssignPhoto(string uri, string pass, PhotoAssignDTO dto, bool initial)
		{
			return await Methods.AssignPhoto(devClient, uri, pass, dto, initial);
		}
		
		public async Task<DevResponseDTO?> DeletePhoto(string uri, string pass, string faceId)
		{
			return await Methods.DeletePhoto(devClient, uri, pass, faceId);
		}
		
		public async Task<RecordsResponseDTO?> QueryRecords(string uri, string pass, RecordsQueryDTO dto)
		{
			return await Methods.QueryRecords(devClient, uri, pass, dto);
		}
		
		public async Task<DevResponseDTO?> DeleteRecords(string uri, string pass, RecordsDeleteDTO dto)
		{
			return await Methods.DeleteRecords(devClient, uri, pass, dto);
		}
		
		public async Task<DevResponseDTO?> SetIdentifyCallback(string uri, string pass, SetIdentifyCallbackDTO dto)
		{
			return await Methods.SetIdentifyCallback(devClient, uri, pass, dto);
		}
	}
}