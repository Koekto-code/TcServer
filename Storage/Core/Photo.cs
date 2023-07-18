using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

// represents photos that are taken on recognition attempt by device

namespace TcServer.Storage.Core
{
	public class Photo
	{
		[Key]
		public int Id { get; set; }

		// many : 1, required for Photo
		public int DeviceId { get; set; }
		public Device Device { get; set; } = null!;
		
		// many : 1, not required, cascade deletion
		public int? EmployeeId { get; set; }
		public Employee? Employee { get; set; }
		
		public enum ImageFormat
		{
			PNG,
			JPG
		}

		public ImageFormat? Format { get; set; }
		
		public string? Base64 { get; set; }
		
		// reserved
		public string? Url { get; set; }
	}
}
