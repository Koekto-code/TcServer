using System.Text.Json;

namespace TcServer.Core
{
	public static class Json
	{
		public static JsonSerializerOptions SerializerDefaults()
		{
			return new JsonSerializerOptions {
				DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
			};
		}
	}
}