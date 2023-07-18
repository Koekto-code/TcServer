using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using TcServer.Storage.Core;
using TcServer.Utility;

namespace TcServer.Views.WorkShifts.Index
{
	public class ViewData
	{
		public List<WorkShift> WorkShifts { get; set; } = new();
	}
}
