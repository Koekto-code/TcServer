using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using TcServer.Storage.Core;
using TcServer.Utility;

namespace TcServer.Views.Devices.Index
{
	public class ViewData
	{
		public List<Device> Devices { get; set; } = new();
		
		public Dictionary<string, bool> DevStat { get; set; } = new();
		
		public string CompName { get; set; } = null!;
	}
}
