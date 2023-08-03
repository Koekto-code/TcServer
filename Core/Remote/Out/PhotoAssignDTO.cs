using System.ComponentModel.DataAnnotations;

namespace TcServer.Core.Remote
{
	public class PhotoAssignDTO
	{
		public class BoundingBox
		{
			public int top { get; set; }
			public int bottom { get; set; }
			public int left { get; set; }
			public int right { get; set; }
			
			public bool empty { get; set; }
		}
		
		[Required]
		public string personId { get; set; } = null!;
		
		[Required]
		public string faceId { get; set; } = null!;
		
		// one of these should be set
		public string? url { get; set; }
		public string? base64 { get; set; }
		
		public bool isEasyWay { get; set; } = false;
		
		public BoundingBox? bbox { get; set; }
	}
}
