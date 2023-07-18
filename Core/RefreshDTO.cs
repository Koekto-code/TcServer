using System.ComponentModel.DataAnnotations;

namespace TcServer.Core
{
	public class RefreshDTO
	{
		[Required]
		public string Email { get; set; } = null!;
	}
}
