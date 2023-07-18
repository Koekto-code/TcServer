
namespace TcServer.Core.Remote
{
	public class DevResponseDTO
	{
		public string code { get; set; } = null!;
		
		public string? data { get; set; }
		
		public string msg { get; set; } = null!;
		
		public string? SDKVersion { get; set; } = null!;
		
		public int result { get; set; }
		
		public bool success { get; set; }
	}
}
