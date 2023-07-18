using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;

namespace TcServer.Utility
{
	public static class DayTime
	{
		public static int ToInt32(string time)
		{
			bool less0 = false;
			if (time.ElementAt(0) == '-')
			{
				less0 = true;
				time = time.Substring(1);
			}   
			var dt = DateTime.ParseExact(time, "HH:mm", null);
			var tm = dt.Hour * 60 + dt.Minute;
			if (less0)
				tm = -tm;
			return tm;
		}
		
		public static string ToString(int time)
		{
			bool less0 = false;
			if (time < 0)
			{
				less0 = true;
				time = -time;
			}
			var fmt = new DateTime().AddMinutes(time).ToString("HH:mm");
			if (less0)
				fmt = "-" + fmt;
			return fmt;
		}
		
		public static string ToStringUnsigned(int time)
		{
			if (time < 0)
				time += 1440;
			return new DateTime().AddMinutes(time).ToString("HH:mm");
		}
		
		public static string ToString(int? time)
		{
			if (time is null)
				return "";
			return ToString(time.Value);
		}
	}
}