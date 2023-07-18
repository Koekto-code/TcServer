using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TcServer.Storage.Core
{
	public class Unit
	{
		[Key]
		public int Id { get; set; }

		// many : 1, required for Unit
		public int CompanyId { get; set; }
		public Company Company { get; set; } = null!;

		// 1 : many, not required
		public ICollection<Employee> Employees { get; set; } = new List<Employee>();

		[Required]
		public string Name { get; set; } = null!;
	}
}
