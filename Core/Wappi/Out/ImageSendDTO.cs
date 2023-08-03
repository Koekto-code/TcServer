using System.ComponentModel.DataAnnotations;

namespace TcServer.Core.Wappi
{
	public class ImageSendDTO
	{
		[Required]
		public string recipient { get; set; } = null!;
		
		[Required]
		public string caption { get; set; } = null!;
		
		[Required]
		public string b64_file { get; set; } = null!;
	}
}