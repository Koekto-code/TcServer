using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TcServer.Storage.Core
{
	public class Company
	{
		public class Settings
		{
			public double GMTOffset { get; set; } = 0.0;
		}
		
		[Key]
		public int Id { get; set; }

		// 1 : 1, required for Company
		public int AccountId { get; set; }
		public Account Account { get; set; } = null!;

		// 1 : many, required for WorkShift
		public ICollection<WorkShift> WorkShifts { get; set; } = new List<WorkShift>();

		public int NextShiftId { get; set; } = 1;

		// 1 : many, required for Unit
		public ICollection<Unit> Units { get; set; } = new List<Unit>();

		// 1 : many, required for Employee
		public ICollection<Employee> Employees { get; set; } = new List<Employee>();

		// 1 : many, optional, cascade deletion
		public ICollection<Device> Devices { get; set; } = new List<Device>();

		public int NextDeviceId { get; set; } = 1;
		
		public string JsonSettings { get; set; } = "{}";

		// unique
		[Required]
		public string Name { get; set; } = null!;
	}
}
