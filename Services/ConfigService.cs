using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

using System.Text;

namespace TcServer.Services
{
	public interface IConfigService
	{
		IConfiguration Access();
		double JwtExpiryDays();
		SymmetricSecurityKey JwtSymSecurityKey();
		TokenValidationParameters JwtValidationParams();
	}
	
	public class ConfigService: IConfigService
	{
		protected readonly IConfiguration _config;

		// cache
		protected readonly double jwtExpiryDays;
		protected readonly SymmetricSecurityKey jwtSymSecurityKey;
		protected readonly TokenValidationParameters jwtValidationParams;

		public ConfigService(IConfiguration config)
		{
			_config = config;

			jwtExpiryDays = Convert.ToDouble(_config["JWT:ExpiryDays"]);
			jwtSymSecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JWT:KeyDetail"]!));
			jwtValidationParams = new TokenValidationParameters
			{
				IssuerSigningKey = jwtSymSecurityKey,
				ValidIssuer = _config["JWT:ValidIssuer"],

				ValidateIssuerSigningKey = true,
				ValidateIssuer = true,
				ValidateAudience = false,
				ValidateLifetime = true
			};
		}

		public IConfiguration Access()
		{
			return _config;
		}

		public double JwtExpiryDays()
		{
			return jwtExpiryDays;
		}

		public SymmetricSecurityKey JwtSymSecurityKey()
		{
			return jwtSymSecurityKey;
		}

		public TokenValidationParameters JwtValidationParams()
		{
			return jwtValidationParams;
		}
	}
}
