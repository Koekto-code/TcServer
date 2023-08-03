using System.ComponentModel.DataAnnotations;

namespace TcServer.Core.Wappi
{
	public class MessageSendDTO
	{
		[Required]
		public string recipient { get; set; } = null!;
		
		[Required]
		public string body { get; set; } = null!;
	}
}