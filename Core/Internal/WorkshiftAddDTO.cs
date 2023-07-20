using System.ComponentModel.DataAnnotations;

namespace TcServer.Core.Internal
{
	public class WorkshiftAddDTO
	{
        [Required]
        public string JobTitle { get; set; } = null!;
        
        [StringLength(10)]
		public string? DateBegin { get; set; }

        [StringLength(10)]
		public string? DateEnd { get; set; }
        
		public int? SunArrive { get; set; }
		public int? SunLeave { get; set; }
		
		public int? MonArrive { get; set; }
		public int? MonLeave { get; set; }
		
		public int? TueArrive { get; set; }
		public int? TueLeave { get; set; }
		
		public int? WedArrive { get; set; }
		public int? WedLeave { get; set; }
		
		public int? ThuArrive { get; set; }
		public int? ThuLeave { get; set; }
		
		public int? FriArrive { get; set; }
		public int? FriLeave { get; set; }
		
		public int? SatArrive { get; set; }
		public int? SatLeave { get; set; }
	}
}
