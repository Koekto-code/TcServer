using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using TcServer.Storage.Core;
using TcServer.Utility;

namespace TcServer.Views.Schedule.Index
{
	public class Record
	{
		public enum State
		{
			Normal,
			Early,
			Late,
			Skip
		}

		public int? TimeArrive { get; set; }

		public State ArriveState = State.Normal;

		public int? TimeLeave { get; set; }

		public State LeaveState = State.Normal;

		public Record(int? timeArrive, int? timeLeave, string date, List<WorkShift> rules)
		{
			TimeArrive = timeArrive;
			TimeLeave = timeLeave;

			DateTime target = DateTime.ParseExact(date, "yyyy-MM-dd", null);
			WorkShift? ruleSelected = Shift.SelectRule(target, rules);

			// compare the data with rule
			if (ruleSelected is not null)
			{
				WorkDayDesc? wd = ruleSelected[target.DayOfWeek];
				if (wd is not null)
				{
					if (wd.Ar is not null && timeArrive is null)
						ArriveState = State.Skip;
					else if (timeArrive > wd.Ar)
						ArriveState = State.Late;

					if (wd.Lv is not null && timeLeave is null)
						LeaveState = State.Skip;
					else if (timeLeave < wd.Lv)
						LeaveState = State.Early;
				}
			}
		}
	}

	public class ViewData
	{
		public AccountType AccType;

		public Storage.Core.Company? Company;

		public List<string> Companies { get; set; } = new();
		
		public List<string> Units { get; set; } = new();

		public Unit? Active { get; set; } = null;

		public string? SelectedDate { get; set; } = null;
		
		// all employees of the active unit should be here
		public Dictionary<Storage.Core.Employee, Record?> Records { get; set; } = new();

		public List<Device> AvailableDevices { get; set; } = new();
	}
}
