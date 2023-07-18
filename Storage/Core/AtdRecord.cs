using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TcServer.Storage.Core
{
	public class AtdRecord
	{
		[Key]
		public int Id { get; set; }

		// many : 1, required for AtdRecord
		public int EmployeeId { get; set; }
		public Employee Employee { get; set; } = null!;

		[Required]
		[StringLength(10)]
		public string Date { get; set; } = null!;

		public int? TimeArrive { get; set; } = null;

		public int? TimeLeave { get; set; } = null;
	}
}
