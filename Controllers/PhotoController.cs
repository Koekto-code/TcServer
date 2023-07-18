using System.Text;
using System.Text.Json;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

using TcServer.Storage;
using TcServer.Storage.Core;
using TcServer.Services;
using TcServer.Attributes;
using TcServer.Core;
using TcServer.Utility;

namespace TcServer.Controllers
{
	[Route("img/")]
	public class PhotoController : Controller
	{
		protected readonly CoreContext dbCtx;
		
		public PhotoController(CoreContext dbctx)
		{
			dbCtx = dbctx;
		}
		
		[HttpGet("id")]
		[CookieAuthorize]
		public async Task<IActionResult> ById(int id)
		{
			string email = (string)HttpContext.Items["authEmail"]!;
			var client = (await dbCtx.AccByEmailAsync(email))!;
			
			// @todo optimize checking before authorization
			var photo = await dbCtx.Photos.FindAsync(id);
			if (photo is null || photo.Base64 is null)
				return BadRequest();
			
			bool authorized = false;
			
			if (client.Type == AccountType.Admin)
				authorized = true;
			else if (client.Type == AccountType.Company)
			{
				await dbCtx.Entry(client).Reference(c => c.Company).LoadAsync();
				var dev = await dbCtx.Devices.FindAsync(photo.DeviceId);
				if (dev is null)
					return BadRequest();
				if (client.Company is not null && dev.CompanyId == client.Company.Id)
					authorized = true;
			}
			
			if (!authorized)
				return Unauthorized();
			
			byte[] img = Convert.FromBase64String(photo.Base64);
			
			string contentType = photo.Format == Photo.ImageFormat.PNG ? "image/png" : "image/jpeg";
			return File(img, contentType);
		}
	}
}
