
namespace TcServer.Core.Remote
{
	public class RecordsResponseDTO
	{
		public class Data
		{
			public class PageInfo
			{
				public int index { get; set; }
				
				public int length { get; set; }
				
				public int size { get; set; }
				
				public int total { get; set; }
			}
			
			public class Record
			{
				public int aliveType { get; set; }
				
				public string id { get; set; } = null!;
				
				public string idcardNum { get; set; } = null!;
				
				public int identifyType { get; set; }
				
				public int isImgDeleted { get; set; }
				
				public bool isPass { get; set; }
				
				public int maskState { get; set; }
				
				public int model { get; set; }
				
				public bool antiPassback { get; set; }
				
				public string name { get; set; } = null!;
				
				// the path to on-site photo
				public string path { get; set; } = null!;
				
				public string personId { get; set; } = null!;
				
				public int recType { get; set; }
				
				// 0: failed, 1: success
				public int state { get; set; }
				
				// unix millisecond timestamp
				public long time { get; set; }
				
				public int type { get; set; }
			}
			
			public PageInfo pageInfo { get; set; } = null!;
			
			public List<Record> records { get; set; } = null!;
		}
		
		public string code { get; set; } = null!;
		
		public Data data { get; set; } = null!;
		
		public string msg { get; set; } = null!;
		
		public int result { get; set; }
		
		public bool success { get; set; }
	}
}
