using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading.Tasks;

using TcServer.Storage.Core;

namespace TcServer.Core
{
	public class RegisterDTO
	{
		[Required]
		public AccountType Type;

		[Required]
		public string Email { get; set; } = null!;

		[Required]
		public string Password { get; set; } = null!;

		[Required]
		public string Name { get; set; } = null!;

		// required if Type is Company
		public string? CompanyName;
	}
}
