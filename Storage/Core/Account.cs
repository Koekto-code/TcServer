using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TcServer.Storage.Core
{
	public enum AccountType
	{
		Default,
		Company,
		Admin
	}

	public class Account
	{
		[Key]
		public int Id { get; set; }

		[Required]
		public AccountType Type { get; set; }

		// 1 : 1, required for Company
		public Company? Company { get; set; }
		
		[Required]
		public string Email { get; set; } = null!;

		// Hashed (using PBKDF2 with HMACSHA256)
		[Required]
		public byte[] Password { get; set; } = null!;
		
		[Required]
		public string Name { get; set; } = null!;
	}
}
