using System;
using TcServer.Storage.Core;

namespace TcServer.Utility
{
	public static class Shift
	{
		public static WorkShift? SelectRule(DateTime date, List<WorkShift> rules)
		{
			WorkShift? ruleSelected = null;
			DateTime? latest = null;

			// choose rule with highest precedence (the latest actual)
			foreach (var r in rules)
			{
				DateTime? datebeg = r.DateBegin is null ? null : DateTime.ParseExact(r.DateBegin, "yyyy-MM-dd", null);

				if ((datebeg is null || datebeg <= date) &&
					(r.DateEnd is null || DateTime.ParseExact(r.DateEnd, "yyyy-MM-dd", null) > date))
				{
					ruleSelected = r;
					if (datebeg > latest)
						latest = datebeg;
				}
			}
			return ruleSelected;
		}

		public static WorkShift? SelectRule(string date, List<WorkShift> rules)
		{
			DateTime target = DateTime.ParseExact(date, "yyyy-MM-dd", null);
			return SelectRule(target, rules);
		}
	}
}
