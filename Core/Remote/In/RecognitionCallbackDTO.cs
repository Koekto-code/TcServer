// Representation of recognition callback model from Uface API

namespace TcServer.Core.Remote
{
	namespace RecognitionCallback
	{
		public class AttendanceDTO
		{
			public string attendanceStatus { get; set; } = null!;

			public int attendanceId { get; set; }
		}
	}

	public class RecognitionCallbackDTO
	{
		public string deviceKey { get; set; } = null!;

		public string time { get; set; } = null!; // unix time (msec)

		public string ip { get; set; } = null!;

		public string personId { get; set; } = null!;

		public string? path { get; set; }

		public string idcardNum { get; set; } = null!;

		public string model { get; set; } = null!;

		public string aliveType { get; set; } = null!;

		public string identifyType { get; set; } = null!;

		public string passTimeType { get; set; } = null!;

		public string permissionTimeType { get; set; } = null!;

		public string recModeType { get; set; } = null!;

		public RecognitionCallback.AttendanceDTO attendance { get; set; } = null!;

		public string type { get; set; } = null!;

		public string? base64 { get; set; }

		public int recType { get; set; }

		public string temperature { get; set; } = null!;

		public string standard { get; set; } = null!;

		public string temperatureState { get; set; } = null!;

		public string tempUnit { get; set; } = null!;

		public string MaskState { get; set; } = null!;

		public string direction { get; set; } = null!;

		public string antiPassbackResult { get; set; } = null!;

		public string identifyComponent { get; set; } = null!;
	}
}
