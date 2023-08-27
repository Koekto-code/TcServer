using System.ComponentModel.DataAnnotations;

namespace TcServer.Core.Remote
{
	public class DoorControlDTO
	{
		public int? type { get; set; }
		
		public string? content { get; set; }
	}
}
