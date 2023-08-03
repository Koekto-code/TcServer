using System.Text;
using System.Text.Json;
using System.Transactions;

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
			Photo? photo;
			
			using (var scope = Transactions.DbAsyncScopeRC())
			{
				var client = (Account)HttpContext.Items["authEntity"]!;
				bool authorized = false;
				
				if (client.Type == AccountType.Admin)
					authorized = true;
				else if (client.Type == AccountType.Company)
				{
					var device = await dbCtx.Photos
						.Where(p => p.Id == id)
						.Select(p => p.Device)
						.FirstOrDefaultAsync();
					
					if (device is null)
						return BadRequest();
					
					await dbCtx.Entry(client).Reference(c => c.Company).LoadAsync();
					if (client.Company is not null && device.CompanyId == client.Company.Id)
						authorized = true;
				}
				
				if (!authorized)
					return Unauthorized();
				
				photo = await dbCtx.Photos.FindAsync(id);
				if (photo?.Base64 is null)
					return BadRequest();
				
				scope.Complete();
			}
			
			byte[] img = Convert.FromBase64String(photo.Base64);
			string contentType = photo.Format == Photo.ImageFormat.PNG ? "image/png" : "image/jpeg";
			
			return File(img, contentType);
		}
	}
}
