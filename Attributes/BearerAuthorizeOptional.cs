using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TcServer.Services;
using TcServer.Storage;

namespace TcServer.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class BearerAuthorizeOptionalAttribute : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
		var svcprov = context.HttpContext.RequestServices;
        var tokenSvc = svcprov.GetRequiredService<ITokenService>();
        var dbCtx = svcprov.GetRequiredService<CoreContext>();
		
		var authHdr = context.HttpContext.Request.Headers["Authorization"];
		if (authHdr.Count != 1) return;

		string[] hdrData = authHdr[0]!.Split(' ');
		if (hdrData.Length != 2 || hdrData[0] != "Bearer") return;

		string? email = tokenSvc.Validate(hdrData[1]);
		if (email is not null)
		{
			var entity = await dbCtx.AccByEmailAsync(email);
			
			context.HttpContext.Items["authEmail"] = email;
			context.HttpContext.Items["authEntity"] = entity;
		}
	}
}