using System.ComponentModel.DataAnnotations;

namespace TcServer.Core.Remote
{
	public class RecordsDeleteDTO
	{
		[Required]
		public string personId { get; set; } = null!;
		
		[Required]
		public string startTime { get; set; } = "0";
		
		[Required]
		public string endTime { get; set; } = "0";
		
		public int? model;
	}
}
