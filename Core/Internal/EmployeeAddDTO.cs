using System.ComponentModel.DataAnnotations;

namespace TcServer.Core.Internal
{
	public class EmployeeAddDTO
	{
		[Required]
		public string JobTitle { get; set; } = null!;
		
		[Required]
		public string Name { get; set; } = null!;

		public string? HomeAddress { get; set; }
		
		public string? Phone { get; set; }
	}
}
