using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TcServer.Storage.Core
{
	public class Device
	{
		[Key]
		public int Id { get; set; }
		
		// absolutely unique
		[Required]
		public string SerialNumber { get; set; } = null!;
		
		// URI
		[Required]
		public string Address { get; set; } = null!;
		
		public string? Password { get; set; } = null;

		// is set on assigning to Company (unique within)
		public int InnerId { get; set; }

		// many : 1, optional, cascade deletion
		public int? CompanyId { get; set; }
		public Company? Company { get; set; }
		
		// 1 : many, not required
		public ICollection<Employee> Employees { get; set; } = new List<Employee>();

		// 1 : many, required for Photo
		public ICollection<Photo> Photos { get; set; } = new List<Photo>();
	}
}
