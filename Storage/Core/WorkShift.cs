using System.ComponentModel.DataAnnotations;

namespace TcServer.Storage.Core
{
	public class WorkDayDesc
	{
		// desired arrive time in minutes
		public int? Ar { get; set; } = null;
		
		// desired leave time
		public int? Lv { get; set; } = null;
	}
	
	public class WorkShift
	{
		[Key]
		public int Id { get; set; }
		
		// Unique ID within Company
		public int InnerId { get; set; }
		
		[Required]
		public string JobTitle { get; set; } = null!;
		
		// many : 1, required for WorkShift
		public int CompanyId { get; set; }
		public Company Company { get; set; } = null!;
		
		// Relevance markers.
		// If both null, the workshift is relevant forever.
		// If no begin marker, the rule is relevant all the time until DateEnd.
		// Likewise without end marker, the rule is relevant from DateBeg forever.
		// If intervals cross over, the one with later DateBegin has precedence.
		// format: "yyyy-MM-dd"

		[StringLength(10)]
		public string? DateBegin { get; set; }

		[StringLength(10)]
		public string? DateEnd { get; set; }

		// Free days are written as null values

		public int? SunArrive { get; set; } = null;
		public int? SunLeave { get; set; } = null;
		
		public int? MonArrive { get; set; } = null;
		public int? MonLeave { get; set; } = null;
		
		public int? TueArrive { get; set; } = null;
		public int? TueLeave { get; set; } = null;
		
		public int? WedArrive { get; set; } = null;
		public int? WedLeave { get; set; } = null;
		
		public int? ThuArrive { get; set; } = null;
		public int? ThuLeave { get; set; } = null;
		
		public int? FriArrive { get; set; } = null;
		public int? FriLeave { get; set; } = null;
		
		public int? SatArrive { get; set; } = null;
		public int? SatLeave { get; set; } = null;

		public WorkDayDesc? this[DayOfWeek ind]
		{
			get
			{
				return
					ind == DayOfWeek.Sunday ? new() { Ar = SunArrive, Lv = SunLeave } :
					ind == DayOfWeek.Monday ? new() { Ar = MonArrive, Lv = MonLeave } :
					ind == DayOfWeek.Tuesday ? new() { Ar = TueArrive, Lv = TueLeave } :
					ind == DayOfWeek.Wednesday ? new() { Ar = WedArrive, Lv = WedLeave } :
					ind == DayOfWeek.Thursday ? new() { Ar = ThuArrive, Lv = ThuLeave } :
					ind == DayOfWeek.Friday ? new() { Ar = FriArrive, Lv = FriLeave } :
					ind == DayOfWeek.Saturday ? new() { Ar = SatArrive, Lv = SatLeave } :
				null;
			}
			set
			{
				switch (ind)
				{
					case DayOfWeek.Sunday:
						SunArrive = value!.Ar;
						SunLeave = value!.Lv;
						break;
					case DayOfWeek.Monday:
						MonArrive = value!.Ar;
						MonLeave = value!.Lv;
						break;
					case DayOfWeek.Tuesday:
						TueArrive = value!.Ar;
						TueLeave = value!.Lv;
						break;
					case DayOfWeek.Wednesday:
						WedArrive = value!.Ar;
						WedLeave = value!.Lv;
						break;
					case DayOfWeek.Thursday:
						ThuArrive = value!.Ar;
						ThuLeave = value!.Lv;
						break;
					case DayOfWeek.Friday:
						FriArrive = value!.Ar;
						FriLeave = value!.Lv;
						break;
					case DayOfWeek.Saturday:
						SatArrive = value!.Ar;
						SatLeave = value!.Lv;
						break;
				}
			}
		}
	}
}
