using System.ComponentModel.DataAnnotations;

namespace TcServer.Core.Remote
{
	public class RecordsQueryDTO
	{
		[Required]
		public string personId { get; set; } = null!;
		
		[Required]
		public string startTime { get; set; } = "0";
		
		[Required]
		public string endTime { get; set; } = "0";
		
		public int? length;
		
		public int? model;
		
		public string? order;
		
		public int? index;
	}
}
