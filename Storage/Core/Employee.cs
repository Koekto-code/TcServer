using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TcServer.Storage.Core
{
	public class Employee
	{
		[Key]
		public int Id { get; set; }
		
		// many : 1, required for Employee
		public int DeviceId { get; set; }
		public Device Device { get; set; } = null!;


		// many : 1, not required
		public int? UnitId { get; set; }
		public Unit? Unit { get; set; }


		// many : 1, required for Employee
		public int CompanyId { get; set; }
		public Company Company { get; set; } = null!;

		// within Company (reusable)
		public int InnerCompId { get; set;}


		// 1 : many, required for AtdRecord
		public ICollection<AtdRecord> AtdRecords = new List<AtdRecord>();

		[Required]
		public string JobTitle { get; set; } = null!;

		[Required]
		public string Name { get; set; } = null!;

		public string? HomeAddress { get; set; }

		public bool RemoteSynchronized { get; set; } = false;

		[Required]
		[StringLength(10)]
		public string EntryDate { get; set; } = null!;

		// 1 : many, not required, cascade deletion
		public ICollection<Photo> Photos { get; set; } = new List<Photo>();
	}
}
