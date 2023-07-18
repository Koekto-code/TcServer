using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TcServer.Services;
using TcServer.Storage;

namespace TcServer.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class CookieAuthorizeOptionalAttribute : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var svcprov = context.HttpContext.RequestServices;
        var tokenSvc = svcprov.GetRequiredService<ITokenService>();
        var dbCtx = svcprov.GetRequiredService<CoreContext>();
        
        string? email = tokenSvc.Validate(context.HttpContext.Request.Cookies["accessToken"]);
        if (email is not null)
        {
            var entity = await dbCtx.AccByEmailAsync(email);
            
            context.HttpContext.Items["authEmail"] = email;
            context.HttpContext.Items["authEntity"] = entity;
        }
    }
}