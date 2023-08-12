using System.ComponentModel.DataAnnotations;

namespace TcServer.Core.Internal
{
	public class EmployeeUpdateDTO
	{
		public string? JobTitle { get; set; }

		public string? Name { get; set; }

		public string? HomeAddress { get; set; }
		
		public string? Phone { get; set; }
		
		public string? IdCard { get; set; }
	}
}
