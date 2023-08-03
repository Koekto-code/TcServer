using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;

using TcServer.Storage;
using TcServer.Storage.Core;
using TcServer.Core;
using TcServer.Utility;


namespace TcServer.Services
{
	public interface IAccountService
	{
		Task<int> RegisterAsync(RegisterDTO data);
		
		Task<AuthDTO?> ChangePwdAsync(ChangePwdDTO data, bool force = false);
		
		Task<AuthDTO> LoginAsync(LoginDTO data);
		
		Task<AuthDTO> RefreshAsync(RefreshDTO data);
	}

	public class AccountService : IAccountService
	{
		protected readonly JwtSecurityTokenHandler tokenHandler;
		protected readonly CoreContext dbCtx;
		protected readonly ITokenService tokenSvc;
		protected readonly IConfigService configSvc;

		public AccountService(CoreContext dbctx, ITokenService ts, IConfigService conf)
		{
			tokenHandler = new();
			dbCtx = dbctx;
			tokenSvc = ts;
			configSvc = conf;
		}

		public async Task<int> RegisterAsync(RegisterDTO data)
		{
			using (var scope = Transactions.DbAsyncScopeDefault())
			{
				if (await dbCtx.AccByEmailAsync(data.Email!) is not null)
					return 1;

				if (data.Type == AccountType.Company && data.CompanyName is null)
					return 2;

				var user = new Account
				{
					Email = data.Email,
					Name = data.Name,
					Type = data.Type,
					Password = Password.Hash(data.Password)
				};

				await dbCtx.Accounts.AddAsync(user);
				await dbCtx.SaveChangesAsync(); // for getting actual next ID

				if (user.Type == AccountType.Company)
				{
					var comp = new Company
					{
						AccountId = user.Id,
						Name = data.CompanyName!
					};
					await dbCtx.Companies.AddAsync(comp);
					await dbCtx.SaveChangesAsync();
				}
				scope.Complete();
			}
			return 0;
		}
		
		public async Task<AuthDTO> LoginAsync(LoginDTO data)
		{
			Account? user;
			using (var scope = Transactions.DbAsyncScopeRC())
			{
				user = await dbCtx.AccByEmailAsync(data.Email!);
				if (user is null)
					return new AuthDTO();

				scope.Complete();
			}

			bool chk = Password.Compare(data.Password!, user.Password!);
			if (!chk)
				return new AuthDTO();

			string accessToken = tokenSvc.NewAccessToken(user);

			return new AuthDTO()
			{
				accessToken = accessToken,
				expiration = DateTime.UtcNow.AddDays(configSvc.JwtExpiryDays()).ToFileTime()
			};
		}
		
		public async Task<AuthDTO?> ChangePwdAsync(ChangePwdDTO data, bool force)
		{
			AuthDTO? result = null;
			Account? user;
			
			using (var scope = Transactions.DbAsyncScopeDefault())
			{
				user = await dbCtx.AccByEmailAsync(data.Email!);
				if (user is null)
					return result;
			
				if (!force && !Password.Compare(data.PrevPassword!, user.Password!))
					return result;
				
				user.Password = Password.Hash(data.NewPassword);
				result = new AuthDTO()
				{
					accessToken = tokenSvc.NewAccessToken(user),
					expiration = DateTime.UtcNow.AddDays(configSvc.JwtExpiryDays()).ToFileTime()
				};
				
				await dbCtx.SaveChangesAsync();
				scope.Complete();
			}
			return result;
		}

		public async Task<AuthDTO> RefreshAsync(RefreshDTO data)
		{
			var user = await dbCtx.AccByEmailAsync(data.Email!);
			if (user is null)
				return new AuthDTO();

			string accessToken = tokenSvc.NewAccessToken(user);

			return new AuthDTO()
			{
				accessToken = accessToken,
				expiration = DateTime.UtcNow.AddDays(configSvc.JwtExpiryDays()).ToFileTime()
			};
		}
	}
}
