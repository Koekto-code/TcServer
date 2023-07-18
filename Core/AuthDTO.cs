using System.ComponentModel.DataAnnotations;

namespace TcServer.Core
{
	public class AuthDTO
	{
		[Required]
		public string accessToken { get; set; } = null!;
		
		public long expiration { get; set; } = -1;
	}
}
