using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading.Tasks;

using TcServer.Storage.Core;

namespace TcServer.Core
{
	public class RegisterInitialDTO
	{
		[Required]
		public string Email { get; set; } = null!;

		[Required]
		public string Password { get; set; } = null!;

		[Required]
		public string RegKey { get; set; } = null!;
		
		// if omitted, the type is Admin
		public string? Type { get; set; }

		[Required]
		public string Name { get; set; } = null!;
		
		// required if Type is Company
		public string? CompanyName { get; set; }
	}
}
