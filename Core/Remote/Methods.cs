using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Diagnostics;
using System.Text.Json;

using TcServer;
using TcServer.Storage.Core;
using TcServer.Services;
using TcServer.Attributes;
using TcServer.Core;
using TcServer.Utility;
using Microsoft.EntityFrameworkCore;

// The raw methods for interaction with Uface devices

namespace TcServer.Core.Remote
{
	public static class Methods
	{
		public static async Task<T?> TryJson<T> (HttpResponseMessage response) where T: class
		{
			if (response.IsSuccessStatusCode)
			{
				try
				{
					string content = await response.Content.ReadAsStringAsync();
					return JsonSerializer.Deserialize<T>(content);
				}
				catch (JsonException)
				{
					// @todo log
					return null;
				}
			}
			return null;
		}
		
		public static async Task<DevResponseDTO?> GetDeviceKey(HttpClient client, string uri)
		{
			HttpResponseMessage response = null!;
			try
			{
				// no password needed
				response = await client.GetAsync(uri + "/getDeviceKey");
			}
			catch (Exception) { return null; }
			return await TryJson<DevResponseDTO>(response);
		}
		
		public static async Task<PersonResponseDTO?> AddEmployee
		(
			HttpClient client, string uri, string pass,
			PersonSubmitDTO dto
		) {
			HttpResponseMessage response = null!;
			string path = uri + "/person/create";

			Dictionary<string, string> formData = new Dictionary<string, string>
			{
				{ "pass", pass },
				{ "person", JsonSerializer.Serialize(dto, Json.SerializerDefaults()) }
			};
			HttpContent formcontent = new FormUrlEncodedContent(formData);
			try
			{
				response = await client.PostAsync(path, formcontent);
			}
			catch (Exception) { return null; }
			return await TryJson<PersonResponseDTO>(response);
		}
		
		public static async Task<PersonResponseDTO?> UpdateEmployee
		(
			HttpClient client, string uri, string pass,
			PersonSubmitDTO dto
		) {
			HttpResponseMessage response = null!;
			string path = uri + "/person/update";

			Dictionary<string, string> formData = new Dictionary<string, string>
			{
				{ "pass", pass },
				{ "person", JsonSerializer.Serialize(dto, Json.SerializerDefaults()) }
			};
			HttpContent formcontent = new FormUrlEncodedContent(formData);
			try
			{
				response = await client.PostAsync(path, formcontent);
			}
			catch (Exception) { return null; }
			return await TryJson<PersonResponseDTO>(response);
		}
		
		public static async Task<DevResponseDTO?> RemoveEmployees
		(
			HttpClient client, string uri, string pass,
			List<string> ids // -1 to remove all
		) {
			HttpResponseMessage response = null!;
			string path = uri + "/person/delete";

			Dictionary<string, string> formData = new Dictionary<string, string>
			{
				{ "pass", pass },
				{ "id", string.Join(',', ids) }
			};
			HttpContent formcontent = new FormUrlEncodedContent(formData);
			try
			{
				response = await client.PostAsync(path, formcontent);
			}
			catch (Exception) { return null; }
			
			// @todo more advanced response
			return await TryJson<DevResponseDTO>(response);
		}
		
		public static async Task<PersonListResponseDTO?> QueryEmployees
		(
			HttpClient client, string uri, string pass,
			string id
		) {
			HttpResponseMessage response = null!;
			string path = uri + "/person/find?pass=" + pass + "&id=" + id;
			try
			{
				response = await client.GetAsync(path);
			}
			catch (Exception) { return null; }
			return await TryJson<PersonListResponseDTO>(response);
		}
		
		public static async Task<RecordsResponseDTO?> QueryRecords
		(
			HttpClient client, string uri, string pass,
			RecordsQueryDTO dto
		) {
			HttpResponseMessage response = null!;
			
			string path = uri + "/newFindRecords" +
				"?pass=" + pass +
				"&personId=" + dto.personId + 
				"&startTime=" + dto.startTime +
				"&endTime=" + dto.endTime;
			
			if (dto.length is not null) path += "&length=" + dto.length;
			if (dto.model is not null) path += "&model=" + dto.model;
			if (dto.order is not null) path += "&order=" + dto.order;
			if (dto.index is not null) path += "&index=" + dto.index;
			try
			{
				response = await client.GetAsync(path);
			}
			catch (Exception) { return null; }
			return await TryJson<RecordsResponseDTO>(response);
		}
		
		public static async Task<DevResponseDTO?> DeleteRecords
		(
			HttpClient client, string uri, string pass,
			RecordsDeleteDTO dto
		) {
			HttpResponseMessage response = null!;
			
			string path = uri + "/newDeleteRecords" +
				"?pass=" + pass +
				"&personId=" + dto.personId + 
				"&startTime=" + dto.startTime +
				"&endTime=" + dto.endTime;
			
			if (dto.model is not null) path += "&model=" + dto.model;
			try
			{
				response = await client.PostAsync(path, new StringContent(string.Empty));
			}
			catch (Exception) { return null; }
			return await TryJson<DevResponseDTO>(response);
		}
		
		public static async Task<DevResponseDTO?> SetIdentifyCallback (
			HttpClient client, string uri, string pass,
			SetIdentifyCallbackDTO dto
		) {
			HttpResponseMessage response = null!;
			string path = uri + "/setIdentifyCallBack";

			Dictionary<string, string> formData = new Dictionary<string, string>
			{
				{ "pass", pass },
				{ "callbackurl", dto.callbackUrl }
			};
			if (dto.base64Enable is not null)
				formData["base64Enable"] = dto.base64Enable.Value.ToString();
			
			HttpContent formcontent = new FormUrlEncodedContent(formData);
			try
			{
				response = await client.PostAsync(path, formcontent);
			}
			catch (Exception) { return null; }
			
			return await TryJson<DevResponseDTO>(response);
		}
	}
}