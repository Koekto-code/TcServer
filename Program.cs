using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using System;
using System.Globalization;
using System.Text;

using TcServer.Storage;
using TcServer.Services;
using TcServer.Attributes;

// force Convert to expect doubles in 12.5 format, not 12,5
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("en-US");

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<CoreContext> (
	options => {
		options.UseSqlServer (
			builder.Configuration.GetConnectionString("DefaultConnection")!
		);
		options.EnableSensitiveDataLogging(false);
	}
);

builder.Services.AddSingleton<IConfigService, ConfigService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IUpdateService, UpdateService>();
builder.Services.AddScoped<IRemoteService, RemoteService>();

builder.Services.AddHostedService<SyncService>();
builder.Services.AddHttpClient("Remote", c => c.Timeout = TimeSpan.FromSeconds(10.0));

var app = builder.Build();

app.UseStaticFiles();
app.UseDeveloperExceptionPage();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(ep => {
	ep.MapControllers();
});

app.MapControllerRoute
(
	name: "default",
	pattern: "{controller=Home}/{action=Index}"
);

await app.RunAsync();
