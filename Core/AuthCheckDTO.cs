using System.ComponentModel.DataAnnotations;

namespace TcServer.Core
{
	public class AuthCheckDTO
	{
		public string Type { get; set; } = null!;
		
		public string Email { get; set; } = null!;
		
		public string Name { get; set; } = null!;
	}
}
