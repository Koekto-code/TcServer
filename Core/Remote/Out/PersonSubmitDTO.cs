using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace TcServer.Core.Remote
{
	public class PersonSubmitDTO
	{
		public string id { get; set; } = null!;

		[Required]
		public string name { get; set; } = null!;
		
		[JsonIgnore(Condition = JsonIgnoreCondition.Never)]
		public string? idcardNum { get; set; }

		public string? iDNumber { get; set; }

		public int? facePermission { get; set; }

		public int? idCardPermission { get; set; }

		public int? faceAndCardPermission { get; set; }

		public int? iDPermission { get; set; }

		public string? tag { get; set; }

		public string? phone { get; set; }

		public string? password { get; set; }

		public int? passwordPermission { get; set; }

		public int? role { get; set; }

		public int? qrCodePermit { get; set; }

		public string? qrCode { get; set; }

		public int? cardAndPasswordPermission { get; set; }

		public int? type { get; set; }

		public string? scheduleId { get; set; }
	}
}
