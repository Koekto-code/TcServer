using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using TcServer.Storage.Core;
using TcServer.Utility;

namespace TcServer.Views.Employee.Index
{
	public class ViewData
	{
		public Storage.Core.Company Company { get; set; } = null!;
		
		public Storage.Core.Employee Employee { get; set; } = null!;
		
		public List<int> OwnPhotos { get; set; } = new();
		
		public List<int> StrangerPhotos { get; set; } = new();
	}
}
