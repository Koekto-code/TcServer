using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

using TcServer.Services;
using TcServer.Storage;
using TcServer.Storage.Core;

namespace TcServer.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class BearerAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
{
    protected readonly AccountType? requiredAccType;

	public BearerAuthorizeAttribute()
	{
		requiredAccType = null;
	}

	public BearerAuthorizeAttribute(AccountType reqType)
    {
        requiredAccType = reqType;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
		var svcprov = context.HttpContext.RequestServices;
        var tokenSvc = svcprov.GetRequiredService<ITokenService>();
        var dbCtx = svcprov.GetRequiredService<CoreContext>();
		
        var authHdr = context.HttpContext.Request.Headers["Authorization"];
		if (authHdr.Count != 1)
			goto authfail;
		
		string[] hdrData = authHdr[0]!.Split(' ');
		if (hdrData.Length != 2 || hdrData[0] != "Bearer")
			goto authfail;

		string? email = tokenSvc.Validate(hdrData[1]);
		if (email is null)
			goto authfail;

		Account? entity = await dbCtx.AccByEmailAsync(email);

		if (entity is null)
			goto authfail;
		
		if (requiredAccType is not null && entity.Type != requiredAccType)
			goto authfail;

		context.HttpContext.Items["authEmail"] = email;
		context.HttpContext.Items["authEntity"] = entity;
		return;

	authfail:
		context.Result = new UnauthorizedResult();
	}
}