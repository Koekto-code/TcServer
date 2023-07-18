using System.ComponentModel.DataAnnotations;

namespace TcServer.Core
{
	public class ChangePwdDTO
	{
		[Required]
		public string Email { get; set; } = null!;

		[Required]
		public string PrevPassword { get; set; } = null!;
		
		[Required]
		public string NewPassword { get; set; } = null!;
	}
}
