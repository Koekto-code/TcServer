using System.ComponentModel.DataAnnotations;

namespace TcServer.Core.Internal
{
	public class DeviceAddDTO
	{
		public string addr { get; set; } = null!;
		
		public string? pass { get; set; }
	}
}
