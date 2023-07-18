using System.ComponentModel.DataAnnotations;

namespace TcServer.Core.Remote
{
	public class SetIdentifyCallbackDTO
	{
		[Required]
		public string callbackUrl { get; set; } = null!;
		
		public int? base64Enable { get; set; } // 1: off, 2: on
	}
}
