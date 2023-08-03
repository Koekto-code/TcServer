using System.ComponentModel.DataAnnotations;

namespace TcServer.Core.Remote
{
	public class PhotoAssignResponseDTO
	{
		public class Data
		{
			public string personId { get; set; } = null!;
			
			public string faceId { get; set; } = null!;
			
			// base64-encoded
			public string feature { get; set; } = null!;
			
			public string SDKVersion { get; set; } = null!;
			
			public string id { get; set; } = null!;
			
			// the image can be accessed from device's LAN
			public string imgPath { get; set; } = null!;
		}
		
		public string code { get; set; } = null!;
		
		public Data data { get; set; } = null!;
		
		public string msg { get; set; } = null!;
		
		public int result { get; set; }
		
		public bool success { get; set; }
	}
}
