
namespace TcServer.Core.Remote
{
	namespace PersonResponse
	{
		public class Data
		{
			public long createTime { get; set; }
			
			public int faceAndCardPermission { get; set; }
			public int faceAndPasswordPermission { get; set; }
			public int facePermission { get; set; }
			
			public string iDNumber { get; set; } = null!;
			public string id { get; set; } = null!;
			
			public int idCardPermission { get; set; }
			public string idcardNum { get; set; } = null!;
			
			public string name { get; set; } = null!;
			
			public string password { get; set; } = null!;
			public int passwordPermission { get; set; }
			
			public string phone { get; set; } = null!;
			public string qrCode { get; set; } = null!;
			public int role { get; set; }
			public int qrCodePermit { get; set; }
			
			public string tag { get; set; } = null!;
		}
	}
	
	public class PersonResponseDTO
	{
		public string code { get; set; } = null!;
		
		public PersonResponse.Data data { get; set; } = null!;
		
		public string msg { get; set; } = null!;
		
		public int result { get; set; }
		
		public bool success { get; set; }
		
		public long? timestamp { get; set; }
	}
	
	public class PersonListResponseDTO
	{
		public string code { get; set; } = null!;
		
		public List<PersonResponse.Data> data { get; set; } = new();
		
		public string msg { get; set; } = null!;
		
		public int result { get; set; }
		
		public bool success { get; set; }
		
		public long? timestamp { get; set; }
	}
}
