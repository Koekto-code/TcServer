using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TcServer.Services;
using TcServer.Storage;
using TcServer.Storage.Core;

namespace TcServer.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class CookieAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly AccountType? requiredAccType;

	public CookieAuthorizeAttribute()
    {
        requiredAccType = null;
    }

	public CookieAuthorizeAttribute(AccountType reqType)
    {
        requiredAccType = reqType;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var svcprov = context.HttpContext.RequestServices;
        var tokenSvc = svcprov.GetRequiredService<ITokenService>();
        var dbCtx = svcprov.GetRequiredService<CoreContext>();
        
        string? email = tokenSvc.Validate(context.HttpContext.Request.Cookies["accessToken"]);
        if (email is null)
            goto authfail;
        
        var entity = await dbCtx.AccByEmailAsync(email);
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