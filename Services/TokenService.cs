using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;

using TcServer;
using TcServer.Storage.Core;

namespace TcServer.Services
{
	public interface ITokenService
	{
		string NewAccessToken(Account acc);
		string? Validate(string? token);
	}

	public class TokenService : ITokenService
	{
		protected JwtSecurityTokenHandler tokenHandler;
		protected readonly IConfigService _configSvc;

		public TokenService(IConfigService conf)
		{
			tokenHandler = new();
			_configSvc = conf;
		}

		public string NewAccessToken(Account acc)
		{
			var tokenDescriptor = new SecurityTokenDescriptor
			{
				Subject = new ClaimsIdentity(new[] { new Claim("email", acc.Email!) }),
				Expires = DateTime.UtcNow.AddDays(_configSvc.JwtExpiryDays()),
				SigningCredentials = new SigningCredentials(
					_configSvc.JwtSymSecurityKey(),
					SecurityAlgorithms.HmacSha256Signature
				),
				Issuer = _configSvc.Access()["JWT:ValidIssuer"]
			};
			var token = tokenHandler.CreateToken(tokenDescriptor);
			return tokenHandler.WriteToken(token);
		}

		// returns email of validated account
		public string? Validate(string? token)
		{
			try
			{
				var tokenParams = _configSvc.JwtValidationParams();
				
				// extra minutes may be added on token generation. Check here as-is
				tokenParams.ClockSkew = TimeSpan.Zero;

				tokenHandler.ValidateToken(token, tokenParams, out SecurityToken validatedToken);
				var jwtToken = validatedToken as JwtSecurityToken;
				var cl = jwtToken!.Claims.First(x => x.Type == "email");
				
				return cl?.Value;
			}
			catch { return null; }
		}
	}
}
