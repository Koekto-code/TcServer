using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;

using TcServer;
using TcServer.Storage.Core;
using TcServer.Services;
using TcServer.Attributes;
using TcServer.Core.Remote;
using TcServer.Utility;

namespace TcServer.Core.Wappi
{
	public static class ChatMethods
	{
		public static async Task<dynamic?> SendMessage
		(
			HttpClient client, string profile,
			MessageSendDTO dto
		) {
			var resp = await client.PostAsync (
				$"https://wappi.pro/api/async/message/send?profile_id={profile}",
				new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json")
			);
			return await Methods.TryJson<dynamic>(resp);
		}
		
		public static async Task<dynamic?> SendImage
		(
			HttpClient client, string profile,
			ImageSendDTO dto
		) {
			var resp = await client.PostAsync (
				$"https://wappi.pro/api/async/message/img/send?profile_id={profile}",
				new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json")
			);
			return await Methods.TryJson<dynamic>(resp);
		}
	}
}